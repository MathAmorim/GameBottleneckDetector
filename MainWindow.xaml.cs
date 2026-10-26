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
    private readonly AppSettings _settings;

    private BottleneckSession? _session;
    private BottleneckSession? _lastFinishedSession;
    private string _cpuName = "Intel Core i7-7700HQ";
    private string _gpuName = "NVIDIA GeForce GTX 1050 Ti";
    private bool _initializingSettings = true;

    public MainWindow()
    {
        InitializeComponent();

        _settings = AppSettings.Load();
        _monitor = new SensorMonitor();
        _frameMonitor = new FrameMonitor();
        _smoother = new BottleneckSmoother();

        // Aplicar preferências nos RadioButtons de configuração
        LoadSettingsUi();

        // Verificar Admin
        bool isAdmin = SensorMonitor.IsElevated;
        BadgeAdmin.Background = isAdmin ? (Brush)new BrushConverter().ConvertFromString("#238636")! : (Brush)new BrushConverter().ConvertFromString("#DA3633")!;
        TxtAdminStatus.Text = isAdmin ? "Admin" : "Não Admin";
        IconAdminStatus.Icon = isAdmin ? FontAwesome.Sharp.IconChar.Check : FontAwesome.Sharp.IconChar.TriangleExclamation;

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

    private void LoadSettingsUi()
    {
        _initializingSettings = true;
        switch (_settings.RamDisplayMode)
        {
            case MemoryDisplayMode.Percentage: RbRamPct.IsChecked = true; break;
            case MemoryDisplayMode.Megabytes: RbRamMb.IsChecked = true; break;
            default: RbRamGb.IsChecked = true; break;
        }

        switch (_settings.VramDisplayMode)
        {
            case MemoryDisplayMode.Percentage: RbVramPct.IsChecked = true; break;
            case MemoryDisplayMode.Megabytes: RbVramMb.IsChecked = true; break;
            default: RbVramGb.IsChecked = true; break;
        }
        _initializingSettings = false;
    }

    private void TabBtn_Checked(object sender, RoutedEventArgs e)
    {
        if (TabContentDetector == null || TabContentSettings == null) return;

        if (TabBtnDetector.IsChecked == true)
        {
            TabContentDetector.Visibility = Visibility.Visible;
            TabContentSettings.Visibility = Visibility.Collapsed;
            TabBtnDetector.Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F6FC")!;
            TabBtnSettings.Foreground = (Brush)new BrushConverter().ConvertFromString("#8B949E")!;
        }
        else
        {
            TabContentDetector.Visibility = Visibility.Collapsed;
            TabContentSettings.Visibility = Visibility.Visible;
            TabBtnDetector.Foreground = (Brush)new BrushConverter().ConvertFromString("#8B949E")!;
            TabBtnSettings.Foreground = (Brush)new BrushConverter().ConvertFromString("#F0F6FC")!;
        }
    }

    private void SettingChanged(object sender, RoutedEventArgs e)
    {
        if (_initializingSettings) return;

        if (RbRamPct.IsChecked == true) _settings.RamDisplayMode = MemoryDisplayMode.Percentage;
        else if (RbRamMb.IsChecked == true) _settings.RamDisplayMode = MemoryDisplayMode.Megabytes;
        else _settings.RamDisplayMode = MemoryDisplayMode.Gigabytes;

        if (RbVramPct.IsChecked == true) _settings.VramDisplayMode = MemoryDisplayMode.Percentage;
        else if (RbVramMb.IsChecked == true) _settings.VramDisplayMode = MemoryDisplayMode.Megabytes;
        else _settings.VramDisplayMode = MemoryDisplayMode.Gigabytes;

        _settings.Save();
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
            ? $"{frames.ProcessName} · {frames.Fps:0} FPS · 1% low {frames.Low1Fps:0} FPS"
            : "Nenhum jogo 3D em primeiro plano (o FPS aparece automaticamente ao jogar)";

        PillStatus.Background = (Brush)new BrushConverter().ConvertFromString(badgeColor)!;
        TxtPillStatus.Text = activeLimiter switch
        {
            Limiter.Thermal => "TÉRMICO",
            Limiter.Vram => "VRAM CHEIA",
            Limiter.Cpu => "GARGALO CPU",
            Limiter.Gpu => "GPU 100% (IDEAL)",
            _ => "ESTÁVEL"
        };
        IconPillStatus.Icon = (activeLimiter == Limiter.Thermal || activeLimiter == Limiter.Vram || activeLimiter == Limiter.Cpu)
            ? FontAwesome.Sharp.IconChar.TriangleExclamation
            : FontAwesome.Sharp.IconChar.CircleCheck;

        // 2. Leituras ao vivo: Cargas
        float gpuLoad = smoothed.GpuLoad ?? 0;
        TxtGpuLoad.Text = $"{gpuLoad:0} %";
        BarGpuLoad.Value = Math.Clamp(gpuLoad, 0, 100);

        float cpuLoad = smoothed.CpuTotal ?? 0;
        TxtCpuLoad.Text = $"{cpuLoad:0} %";
        BarCpuLoad.Value = Math.Clamp(cpuLoad, 0, 100);

        if (smoothed.CpuMaxCore != null)
        {
            TxtCpuCore.Text = $"{smoothed.CpuMaxCore:0} %";
            LblCpuCore.Text = !string.IsNullOrEmpty(smoothed.BusiestCore) ? $"Núcleo ({smoothed.BusiestCore})" : "Núcleo Mais Ocupado";
            BarCpuCore.Value = Math.Clamp(smoothed.CpuMaxCore.Value, 0, 100);
        }

        // 3. Leituras ao vivo: Temperaturas SEPARADAS
        float cpuT = smoothed.CpuTemp ?? 0;
        TxtCpuTemp.Text = cpuT > 0 ? $"{cpuT:0} °C" : "— °C";
        BarCpuTemp.Value = Math.Clamp(cpuT, 0, 100);
        BarCpuTemp.Foreground = (Brush)new BrushConverter().ConvertFromString(
            cpuT >= 90 ? "#DA3633" : cpuT >= 80 ? "#D29922" : "#3FB950")!;

        float gpuT = smoothed.GpuTemp ?? 0;
        TxtGpuTemp.Text = gpuT > 0 ? $"{gpuT:0} °C" : "— °C";
        BarGpuTemp.Value = Math.Clamp(gpuT, 0, 100);
        BarGpuTemp.Foreground = (Brush)new BrushConverter().ConvertFromString(
            gpuT >= 85 ? "#DA3633" : gpuT >= 78 ? "#D29922" : "#3FB950")!;

        // 4. Leituras ao vivo: Memórias com formatação configurável (GB / MB / %)
        double vramPct = smoothed.VramPct ?? 0;
        BarVram.Value = Math.Clamp(vramPct, 0, 100);
        float? vramUsedMb = _monitor.VramUsedMb;
        float vramTotalMb = _monitor.VramTotalMb ?? 4096f;
        float vramUsedGb = (vramUsedMb ?? 0) / 1024f;
        float vramTotalGb = vramTotalMb / 1024f;

        TxtVram.Text = _settings.VramDisplayMode switch
        {
            MemoryDisplayMode.Percentage => $"{vramPct:0} %",
            MemoryDisplayMode.Megabytes => $"{(vramUsedMb ?? 0):0} MB / {vramTotalMb:0} MB ({vramPct:0}%)",
            _ => $"{vramUsedGb:0.1} GB / {vramTotalGb:0.1} GB ({vramPct:0}%)"
        };

        float ramPct = smoothed.RamPct ?? 0;
        BarRam.Value = Math.Clamp(ramPct, 0, 100);
        float ramTotalGb = _monitor.RamTotalGb ?? SensorMonitor.GetSystemTotalRamGb();
        float ramUsedGb = _monitor.RamUsedGb ?? ((ramPct / 100f) * ramTotalGb);

        TxtRam.Text = _settings.RamDisplayMode switch
        {
            MemoryDisplayMode.Percentage => $"{ramPct:0} %",
            MemoryDisplayMode.Megabytes => $"{(ramUsedGb * 1024):0} MB / {(ramTotalGb * 1024):0} MB ({ramPct:0}%)",
            _ => $"{ramUsedGb:0.1} GB / {ramTotalGb:0.1} GB ({ramPct:0}%)"
        };

        // 5. Atualizar gravação de sessão se estiver ativa
        if (_session != null)
        {
            _session.Add(rawSample);
            var elapsed = DateTime.Now - _session.Start;
            string gameName = frames != null ? frames.ProcessName : "Alterne para o jogo agora";

            if (_session.IsContinuous)
            {
                TxtSessionStatus.Text = $"🔴 Gravando sessão livre: {elapsed:m\\:ss} ({_session.Count} amostras) · Jogo: {gameName} · Clique em 'Concluir Análise' ao terminar.";
            }
            else
            {
                TxtSessionStatus.Text = $"🔴 Gravando: {elapsed:m\\:ss} de {_session.Duration:m\\:ss} ({_session.Count} amostras) · Jogo: {gameName}";
            }

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
            // O usuário clicou para CONCLUIR a sessão (seja contínua ou antecipada)
            FinishSession();
            return;
        }

        // Iniciar nova sessão
        TimeSpan duration = TimeSpan.FromMinutes(1);
        if (RbContinuous.IsChecked == true) duration = TimeSpan.Zero; // Modo livre
        else if (Rb3Min.IsChecked == true) duration = TimeSpan.FromMinutes(3);
        else if (Rb5Min.IsChecked == true) duration = TimeSpan.FromMinutes(5);
        else if (Rb10Min.IsChecked == true) duration = TimeSpan.FromMinutes(10);

        _session = new BottleneckSession { Duration = duration };
        PanelResults.Visibility = Visibility.Collapsed;
        PanelDurations.IsEnabled = false;

        BtnSession.Content = "CONCLUIR ANÁLISE";
        BtnSession.Style = (Style)FindResource("SecondaryButton");
        BtnDiscardSession.Visibility = Visibility.Visible;

        TxtSessionStatus.Text = duration <= TimeSpan.Zero
            ? "Gravação contínua iniciada! Jogue normalmente pelo tempo que desejar..."
            : $"Gravação iniciada! Jogue normalmente pelos próximos {duration.TotalMinutes:0} minuto(s)...";
    }

    private void BtnDiscardSession_Click(object sender, RoutedEventArgs e)
    {
        if (_session == null) return;

        _session = null;
        PanelDurations.IsEnabled = true;
        BtnSession.Content = "INICIAR ANÁLISE";
        BtnSession.Style = (Style)FindResource("PrimaryButton");
        BtnDiscardSession.Visibility = Visibility.Collapsed;
        TxtSessionStatus.Text = "Gravação descartada sem salvar relatório.";
    }

    private void FinishSession()
    {
        var sess = _session!;
        _lastFinishedSession = sess;
        _session = null;

        PanelDurations.IsEnabled = true;
        BtnSession.Content = "INICIAR ANÁLISE";
        BtnSession.Style = (Style)FindResource("PrimaryButton");
        BtnDiscardSession.Visibility = Visibility.Collapsed;

        if (sess.Count == 0)
        {
            TxtSessionStatus.Text = "Análise finalizada sem amostras coletadas.";
            return;
        }

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
            TxtThermalAlert.Text = $"SUPER-AQUECIMENTO (THERMAL THROTTLING): A CPU atingiu {maxCpu:0} °C durante a partida! " +
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

            string ramInfo = $"{(_monitor.RamTotalGb ?? SensorMonitor.GetSystemTotalRamGb()):0.1} GB";
            HtmlReportExporter.GenerateHtmlReport(_lastFinishedSession, _cpuName, _gpuName, fullPath, ramInfo);

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
