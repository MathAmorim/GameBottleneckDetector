using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace GameBottleneckDetector;

public sealed record DistributionItem(string Label, double Pct, string PctText, Brush ColorBrush);

public partial class MainWindow : Window
{
    private readonly SensorMonitor _monitor;
    private readonly FrameMonitor _frameMonitor;
    private readonly BottleneckSmoother _smoother;
    private readonly DispatcherTimer _timer;

    private BottleneckSession? _session;
    private BottleneckSession? _lastFinishedSession;
    private string _cpuName = "Intel Core i7-7700HQ";
    private string _gpuName = "NVIDIA GeForce GTX 1050 Ti";

    public MainWindow()
    {
        InitializeComponent();

        _monitor = new SensorMonitor();
        _frameMonitor = new FrameMonitor();
        _smoother = new BottleneckSmoother();

        // Verificar Admin
        bool isAdmin = SensorMonitor.IsElevated;
        BadgeAdmin.Background = isAdmin ? (Brush)new BrushConverter().ConvertFromString("#238636")! : (Brush)new BrushConverter().ConvertFromString("#DA3633")!;
        TxtAdminStatus.Text = isAdmin ? "Admin ✓" : "Não Admin ⚠";

        // Carregar sensores e inicializar hardware
        Loaded += async (_, _) => await InitHardwareAsync();

        // Timer de atualização ao vivo (1 segundo)
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateTelemetry();
        _timer.Start();

        Closed += (_, _) =>
        {
            _timer.Stop();
            _frameMonitor.Dispose();
            _monitor.Dispose();
        };
    }

    private async Task InitHardwareAsync()
    {
        TxtHardwareInfo.Text = "Detectando componentes...";
        await _monitor.OpenAsync();
        _frameMonitor.Start();

        if (!string.IsNullOrEmpty(_monitor.CpuName) && _monitor.CpuName != "CPU")
            _cpuName = _monitor.CpuName;
        if (!string.IsNullOrEmpty(_monitor.GpuName) && _monitor.GpuName != "GPU")
            _gpuName = _monitor.GpuName;

        TxtHardwareInfo.Text = $"{_cpuName} · {_gpuName}";
        TxtCpuShort.Text = $"CPU: {_cpuName}";
        TxtGpuShort.Text = $"GPU: {_gpuName}";

        // Balanço teórico padrão para i7-7700HQ (índice ~42) + GTX 1050 Ti (índice ~18)
        var (ratio, text) = BottleneckAnalyzer.Balance(18.0, 42.0);
        BarBalance.Value = Math.Clamp(ratio * 50.0, 10, 90);
        TxtBalanceDesc.Text = text + " Em condições ideais de temperatura, a GTX 1050 Ti é o fator limitante padrão.";
    }

    private void UpdateTelemetry()
    {
        _monitor.Refresh();
        var frames = _frameMonitor.GetForegroundStats();
        var rawSample = BottleneckAnalyzer.Sample(_monitor, frames);

        // Processa através da média móvel de 5 segundos e histerese
        var (smoothed, activeLimiter, badgeColor) = _smoother.Process(rawSample);

        // 1. Atualizar card AGORA (Suavizado e Estável)
        TxtLiveTitle.Text = BottleneckAnalyzer.Title(activeLimiter);
        TxtLiveAdvice.Text = BottleneckAnalyzer.Advice(activeLimiter, smoothed.CpuTemp);
        TxtLiveGame.Text = frames != null
            ? $"🎮 {frames.ProcessName} · {frames.Fps:0} FPS · 1% low {frames.Low1Fps:0} FPS"
            : "Nenhum jogo 3D em primeiro plano (o FPS aparece automaticamente ao jogar)";

        PillStatus.Background = (Brush)new BrushConverter().ConvertFromString(badgeColor)!;
        TxtPillStatus.Text = activeLimiter switch
        {
            Limiter.Thermal => "TÉRMICO ⚠",
            Limiter.Vram => "VRAM CHEIA",
            Limiter.Cpu => "GARGALO CPU",
            Limiter.Gpu => "GPU 100% (IDEAL)",
            _ => "ESTÁVEL"
        };

        // 2. Leituras ao vivo
        float gpuLoad = smoothed.GpuLoad ?? 0;
        TxtGpuLoad.Text = $"{gpuLoad:0} %";
        BarGpuLoad.Value = Math.Clamp(gpuLoad, 0, 100);

        float cpuLoad = smoothed.CpuTotal ?? 0;
        TxtCpuLoad.Text = $"{cpuLoad:0} %";
        BarCpuLoad.Value = Math.Clamp(cpuLoad, 0, 100);

        if (smoothed.CpuMaxCore != null)
        {
            TxtCpuCore.Text = $"{smoothed.CpuMaxCore:0} %";
            LblCpuCore.Text = !string.IsNullOrEmpty(smoothed.BusiestCore) ? $"Núcleo ({smoothed.BusiestCore})" : "Núcleo mais ocupado";
            BarCpuCore.Value = Math.Clamp(smoothed.CpuMaxCore.Value, 0, 100);
        }

        double vram = smoothed.VramPct ?? 0;
        TxtVram.Text = $"{vram:0} %";
        BarVram.Value = Math.Clamp(vram, 0, 100);

        float ram = smoothed.RamPct ?? 0;
        TxtRam.Text = $"{ram:0} %";
        BarRam.Value = Math.Clamp(ram, 0, 100);

        float gpuT = smoothed.GpuTemp ?? 0;
        float cpuT = smoothed.CpuTemp ?? 0;
        TxtTemps.Text = $"{(gpuT > 0 ? $"{gpuT:0} °C" : "—")} / {(cpuT > 0 ? $"{cpuT:0} °C" : "—")}";
        BarTemps.Value = Math.Clamp(Math.Max(gpuT, cpuT), 0, 100);
        BarTemps.Foreground = (Brush)new BrushConverter().ConvertFromString(cpuT >= 90 ? "#DA3633" : cpuT >= 80 ? "#D29922" : "#238636")!;

        // 3. Atualizar gravação de sessão se estiver ativa
        if (_session != null)
        {
            _session.Add(rawSample);
            var elapsed = DateTime.Now - _session.Start;
            TxtSessionStatus.Text = $"Gravando: {elapsed:m\\:ss} de {_session.Duration:m\\:ss} ({_session.Count} amostras) · " +
                                   (frames != null ? $"Jogo: {frames.ProcessName}" : "Alterne para o jogo agora");

            if (_session.Done)
            {
                FinishSession();
            }
        }
    }

    private void BtnSession_Click(object sender, RoutedEventArgs e)
    {
        if (_session != null)
        {
            // Cancelar
            _session = null;
            BtnSession.Content = "INICIAR ANÁLISE";
            BtnSession.Style = (Style)FindResource("PrimaryButton");
            TxtSessionStatus.Text = "Análise cancelada pelo usuário.";
            return;
        }

        int minutes = 1;
        if (Rb3Min.IsChecked == true) minutes = 3;
        else if (Rb5Min.IsChecked == true) minutes = 5;
        else if (Rb10Min.IsChecked == true) minutes = 10;

        _session = new BottleneckSession { Duration = TimeSpan.FromMinutes(minutes) };
        PanelResults.Visibility = Visibility.Collapsed;
        BtnSession.Content = "CANCELAR GRAVAÇÃO";
        BtnSession.Style = (Style)FindResource("DangerButton");
        TxtSessionStatus.Text = $"Gravação iniciada! Alterne para o jogo e jogue pelos próximos {minutes} minuto(s)...";
    }

    private void FinishSession()
    {
        var sess = _session!;
        _lastFinishedSession = sess;
        _session = null;

        BtnSession.Content = "INICIAR NOVA ANÁLISE";
        BtnSession.Style = (Style)FindResource("PrimaryButton");
        TxtSessionStatus.Text = $"Análise finalizada com sucesso às {DateTime.Now:HH:mm:ss} ({sess.Count} amostras registradas).";

        var main = sess.Main ?? Limiter.Capped;
        TxtResultTitle.Text = $"Resultado: {BottleneckAnalyzer.Title(main)}";

        double maxCpu = sess.Max(x => x.CpuTemp) ?? 0;
        double maxGpu = sess.Max(x => x.GpuTemp) ?? 0;
        double maxVram = sess.Max(x => x.VramPct) ?? 0;
        double avgFps = sess.AvgFps ?? 0;
        double low1 = sess.AvgLow1 ?? 0;

        // Resumo com CPU Máxima expressamente incluída
        var parts = new List<string>();
        if (avgFps > 0) parts.Add($"FPS médio: {avgFps:0.0}");
        if (low1 > 0) parts.Add($"1% Low: {low1:0.0}");
        parts.Add($"CPU máx: {maxCpu:0} °C");
        parts.Add($"GPU máx: {maxGpu:0} °C");
        if (maxVram > 0) parts.Add($"VRAM máx: {maxVram:0} %");

        TxtResultSummary.Text = string.Join("  ·  ", parts);

        // Alerta de Throttling Térmico Crítico
        if (maxCpu >= 95)
        {
            BoxThermalAlert.Visibility = Visibility.Visible;
            TxtThermalAlert.Text = $"⚠️ SUPER-AQUECIMENTO (THERMAL THROTTLING): A CPU atingiu {maxCpu:0} °C durante a partida! " +
                                   $"Ao ultrapassar 95 °C, o processador Core i7-7700HQ corta bruscamente a frequência para evitar queima física. " +
                                   $"Isso causa quedas instantâneas de FPS e congelamentos de tela.";
        }
        else
        {
            BoxThermalAlert.Visibility = Visibility.Collapsed;
        }

        // Distribuição do tempo
        var distItems = sess.Distribution().Select(d =>
        {
            string colorHex = d.Limiter switch
            {
                Limiter.Thermal => "#DA3633",
                Limiter.Vram => "#D29922",
                Limiter.Cpu => "#E91E63",
                Limiter.Gpu => "#238636",
                Limiter.Ram => "#BC8CFF",
                _ => "#58A6FF"
            };
            return new DistributionItem(
                BottleneckAnalyzer.Title(d.Limiter),
                d.Pct,
                $"{d.Pct:0} %",
                (Brush)new BrushConverter().ConvertFromString(colorHex)!
            );
        }).ToList();

        ListDistribution.ItemsSource = distItems;
        TxtResultAdvice.Text = BottleneckAnalyzer.Advice(main, maxCpu);
        TxtExportStatus.Text = "";
        PanelResults.Visibility = Visibility.Visible;
    }

    private void BtnExportHtml_Click(object sender, RoutedEventArgs e)
    {
        if (_lastFinishedSession == null) return;

        try
        {
            string reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Relatorios");
            Directory.CreateDirectory(reportsDir);

            string gameName = _lastFinishedSession.Samples.FirstOrDefault(s => !string.IsNullOrEmpty(s.GameProcess))?.GameProcess ?? "Jogo";
            string fileName = $"Relatorio_{gameName}_{DateTime.Now:yyyyMMdd_HHmmss}.html";
            string fullPath = Path.Combine(reportsDir, fileName);

            HtmlReportExporter.GenerateHtmlReport(_lastFinishedSession, _cpuName, _gpuName, fullPath);

            TxtExportStatus.Text = $"✓ Relatório gerado com sucesso: {fullPath}";

            // Abre o arquivo gerado no navegador padrão do Windows
            Process.Start(new ProcessStartInfo
            {
                FileName = fullPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            TxtExportStatus.Text = $"Erro ao exportar relatório: {ex.Message}";
        }
    }
}
