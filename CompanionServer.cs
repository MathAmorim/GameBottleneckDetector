using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace GameBottleneckDetector;

public sealed class CompanionServer : IDisposable
{
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private volatile string _latestTelemetryJson = "{}";
    private volatile string? _latestReportHtml;
    private readonly int _port;

    public int Port => _port;
    public bool IsRunning { get; private set; }
    public string LocalUrl { get; private set; } = "";
    public string? ErrorMessage { get; private set; }

    public event Action<int>? StartSessionRequested;
    public event Action? StopSessionRequested;
    public event Action? DiscardSessionRequested;
    public event Action? MarkLagRequested;

    public CompanionServer(int port = 5150)
    {
        _port = port > 0 ? port : 5150;
        LocalUrl = $"http://{GetLocalLanIp()}:{_port}";
    }

    public void UpdateTelemetryJson(object payload)
    {
        try
        {
            _latestTelemetryJson = JsonSerializer.Serialize(payload);
        }
        catch { }
    }

    public void UpdateLatestReportHtml(string html)
    {
        _latestReportHtml = html;
    }

    public bool Start()
    {
        if (IsRunning) return true;
        ErrorMessage = null;
        LocalUrl = $"http://{GetLocalLanIp()}:{_port}";

        try
        {
            EnsureFirewallRuleAsync(_port);

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{_port}/");
            _listener.Start();
            IsRunning = true;
        }
        catch
        {
            // Fallback para localhost caso não esteja elevado como Admin
            try
            {
                _listener?.Close();
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();
                LocalUrl = $"http://localhost:{_port}";
                IsRunning = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                IsRunning = false;
                return false;
            }
        }

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        return true;
    }

    public void Stop()
    {
        IsRunning = false;
        try { _cts?.Cancel(); } catch { }
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        _listener = null;
    }

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            try
            {
                var ctx = await _listener.GetContextAsync().WaitAsync(ct);
                _ = Task.Run(() => HandleRequestAsync(ctx), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch
            {
                if (!IsRunning) break;
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext ctx)
    {
        var req = ctx.Request;
        var res = ctx.Response;

        res.Headers["Access-Control-Allow-Origin"] = "*";
        res.Headers["Access-Control-Allow-Methods"] = "GET, POST, OPTIONS";
        res.Headers["Access-Control-Allow-Headers"] = "Content-Type";
        res.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";

        if (req.HttpMethod == "OPTIONS")
        {
            res.StatusCode = 204;
            res.Close();
            return;
        }

        string path = req.Url?.AbsolutePath.ToLowerInvariant() ?? "/";

        try
        {
            switch (path)
            {
                case "/":
                case "/index.html":
                    await WriteTextAsync(res, LoadWebIndexHtml(), "text/html; charset=utf-8");
                    break;

                case "/manifest.json":
                    const string manifest = """
                    {
                      "name": "Game Bottleneck Detector — Segunda Tela",
                      "short_name": "Bottleneck",
                      "start_url": "/",
                      "display": "standalone",
                      "background_color": "#0D1117",
                      "theme_color": "#0D1117",
                      "icons": [
                        {
                          "src": "/icon.ico",
                          "sizes": "256x256 128x128 64x64 48x48 32x32 16x16",
                          "type": "image/x-icon"
                        }
                      ]
                    }
                    """;
                    await WriteTextAsync(res, manifest, "application/manifest+json; charset=utf-8");
                    break;

                case "/icon.ico":
                case "/favicon.ico":
                    await WriteIconAsync(res);
                    break;

                case "/api/telemetry":
                    await WriteTextAsync(res, _latestTelemetryJson, "application/json; charset=utf-8");
                    break;

                case "/api/session/start":
                    if (req.HttpMethod == "POST")
                    {
                        int durationSec = 180;
                        try
                        {
                            using var sr = new StreamReader(req.InputStream, Encoding.UTF8);
                            string body = await sr.ReadToEndAsync();
                            if (!string.IsNullOrWhiteSpace(body))
                            {
                                using var doc = JsonDocument.Parse(body);
                                if (doc.RootElement.TryGetProperty("durationSeconds", out var durProp))
                                    durationSec = durProp.GetInt32();
                            }
                        }
                        catch { }

                        StartSessionRequested?.Invoke(durationSec);
                        await WriteTextAsync(res, "{\"ok\":true}", "application/json; charset=utf-8");
                    }
                    break;

                case "/api/session/stop":
                    if (req.HttpMethod == "POST")
                    {
                        StopSessionRequested?.Invoke();
                        await WriteTextAsync(res, "{\"ok\":true}", "application/json; charset=utf-8");
                    }
                    break;

                case "/api/session/discard":
                    if (req.HttpMethod == "POST")
                    {
                        DiscardSessionRequested?.Invoke();
                        await WriteTextAsync(res, "{\"ok\":true}", "application/json; charset=utf-8");
                    }
                    break;

                case "/api/session/mark":
                    if (req.HttpMethod == "POST")
                    {
                        MarkLagRequested?.Invoke();
                        await WriteTextAsync(res, "{\"ok\":true}", "application/json; charset=utf-8");
                    }
                    break;

                case "/api/report/latest":
                    if (!string.IsNullOrEmpty(_latestReportHtml))
                    {
                        await WriteTextAsync(res, _latestReportHtml!, "text/html; charset=utf-8");
                    }
                    else
                    {
                        const string emptyReport = """
                        <!DOCTYPE html><html lang="pt-BR"><head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1">
                        <title>Sem Relatório</title></head>
                        <body style="background:#0d1117;color:#f0f6fc;font-family:sans-serif;display:grid;place-items:center;min-height:90vh;text-align:center;padding:20px;">
                        <div><h2 style="color:#58a6ff;">Nenhum relatório disponível</h2><p style="color:#8b949e;">Conclua uma análise de sessão primeiro para visualizar o relatório interativo.</p></div>
                        </body></html>
                        """;
                        await WriteTextAsync(res, emptyReport, "text/html; charset=utf-8");
                    }
                    break;

                default:
                    res.StatusCode = 404;
                    await WriteTextAsync(res, "Not Found", "text/plain; charset=utf-8");
                    break;
            }
        }
        catch
        {
            try { res.StatusCode = 500; res.Close(); } catch { }
        }
    }

    private static async Task WriteTextAsync(HttpListenerResponse res, string content, string contentType)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        res.ContentType = contentType;
        res.ContentLength64 = bytes.Length;
        await res.OutputStream.WriteAsync(bytes);
        res.Close();
    }

    private static async Task WriteIconAsync(HttpListenerResponse res)
    {
        try
        {
            var sri = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/app.ico"));
            if (sri?.Stream != null)
            {
                using var ms = new MemoryStream();
                await sri.Stream.CopyToAsync(ms);
                byte[] bytes = ms.ToArray();
                res.ContentType = "image/x-icon";
                res.ContentLength64 = bytes.Length;
                await res.OutputStream.WriteAsync(bytes);
                res.Close();
                return;
            }
        }
        catch { }

        res.StatusCode = 204;
        res.Close();
    }

    private static string LoadWebIndexHtml()
    {
        try
        {
            string diskPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "web", "index.html");
            if (File.Exists(diskPath))
                return File.ReadAllText(diskPath, Encoding.UTF8);
        }
        catch { }

        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("GameBottleneckDetector.web.index.html");
            if (stream != null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8);
                return reader.ReadToEnd();
            }
        }
        catch { }

        return "<html><body style='background:#0d1117;color:#fff'>Interface web nao encontrada.</body></html>";
    }

    public static string GetLocalLanIp()
    {
        try
        {
            foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.OperationalStatus != OperationalStatus.Up) continue;
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback ||
                    ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel) continue;

                string desc = (ni.Description + " " + ni.Name).ToLowerInvariant();
                if (desc.Contains("virtual") || desc.Contains("hyper-v") || desc.Contains("vmware") || desc.Contains("vbox") || desc.Contains("wsl"))
                    continue;

                var props = ni.GetIPProperties();
                if (props.GatewayAddresses.Count == 0) continue;

                foreach (var ua in props.UnicastAddresses)
                {
                    if (ua.Address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ua.Address))
                        return ua.Address.ToString();
                }
            }

            // Fallback sem exigir gateway
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    return ip.ToString();
            }
        }
        catch { }

        return "127.0.0.1";
    }

    private static void EnsureFirewallRuleAsync(int port)
    {
        if (!SensorMonitor.IsElevated) return;

        _ = Task.Run(() =>
        {
            try
            {
                const string ruleName = "GameBottleneckDetector Companion";
                var delPsi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall delete rule name=\"{ruleName}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (var delProc = Process.Start(delPsi))
                {
                    delProc?.WaitForExit(2000);
                }

                var addPsi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow protocol=TCP localport={port} profile=any",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                };
                using (var addProc = Process.Start(addPsi))
                {
                    addProc?.WaitForExit(2000);
                }
            }
            catch { }
        });
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
    }
}
