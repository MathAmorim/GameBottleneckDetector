using LibreHardwareMonitor.Hardware;

namespace GameBottleneckDetector;

public enum Limiter { Gpu, Cpu, Vram, Ram, Thermal, Capped }

public sealed record LoadSample(
    DateTime Timestamp,
    float? GpuLoad,
    float? CpuTotal,
    float? CpuMaxCore,
    string? BusiestCore,
    double? VramPct,
    float? RamPct,
    float? GpuTemp,
    float? CpuTemp,
    bool GpuThermalCap,
    double? Fps,
    double? Low1Fps,
    string? GameProcess)
{
    public Limiter Classify()
    {
        if (VramPct >= 97) return Limiter.Vram;
        if (GpuThermalCap || CpuTemp >= 95 || GpuTemp >= 88) return Limiter.Thermal;
        if (GpuLoad >= 90) return Limiter.Gpu;
        if (CpuMaxCore >= 85 || CpuTotal >= 85) return Limiter.Cpu;
        if (RamPct >= 92) return Limiter.Ram;
        return Limiter.Capped;
    }
}

/// <summary>
/// Média móvel de 5 segundos e histerese para evitar oscilações bruscas no card AGORA.
/// </summary>
public sealed class BottleneckSmoother
{
    private readonly Queue<LoadSample> _window = new();
    private const int WindowCapacity = 5;
    private Limiter _activeLimiter = Limiter.Capped;
    private Limiter _candidateLimiter = Limiter.Capped;
    private int _candidateCycles = 0;
    private int _thermalHoldCycles = 0;

    public int SampleCount => _window.Count;

    public (LoadSample Smoothed, Limiter Limiter, string StatusBadgeColor) Process(LoadSample raw)
    {
        _window.Enqueue(raw);
        while (_window.Count > WindowCapacity) _window.Dequeue();

        var list = _window.ToList();

        float? avgGpu = Average(list.Select(x => x.GpuLoad));
        float? avgCpu = Average(list.Select(x => x.CpuTotal));
        float? avgMaxCore = Average(list.Select(x => x.CpuMaxCore));
        double? avgVram = Average(list.Select(x => (float?)x.VramPct));
        float? avgRam = Average(list.Select(x => x.RamPct));
        float? avgGpuTemp = Average(list.Select(x => x.GpuTemp));
        float? avgCpuTemp = Average(list.Select(x => x.CpuTemp));
        bool anyThermal = list.Any(x => x.GpuThermalCap || (x.CpuTemp >= 95) || (x.GpuTemp >= 88));
        double? avgFps = Average(list.Select(x => (float?)x.Fps));
        double? avgLow1 = Average(list.Select(x => (float?)x.Low1Fps));

        var smoothed = new LoadSample(
            raw.Timestamp,
            avgGpu,
            avgCpu,
            avgMaxCore,
            raw.BusiestCore,
            avgVram,
            avgRam,
            avgGpuTemp,
            avgCpuTemp,
            anyThermal,
            avgFps,
            avgLow1,
            raw.GameProcess
        );

        var candidate = smoothed.Classify();

        // Histerese inteligente: se for térmico ou VRAM, retém por mais tempo para leitura estável
        if (anyThermal)
        {
            _activeLimiter = Limiter.Thermal;
            _thermalHoldCycles = 4; // segura 4s
        }
        else if (_thermalHoldCycles > 0)
        {
            _thermalHoldCycles--;
            _activeLimiter = Limiter.Thermal;
        }
        else
        {
            if (candidate == _candidateLimiter)
            {
                _candidateCycles++;
                if (_candidateCycles >= 2) // Necessita 2 amostras consistentes para mudar
                {
                    _activeLimiter = candidate;
                }
            }
            else
            {
                _candidateLimiter = candidate;
                _candidateCycles = 1;
            }
        }

        string badgeColor = _activeLimiter switch
        {
            Limiter.Thermal => "#FF4C4C", // Vermelho Alerta
            Limiter.Vram => "#FF9800",    // Laranja
            Limiter.Cpu => "#E91E63",     // Rosa/Vermelho
            Limiter.Ram => "#9C27B0",     // Roxo
            Limiter.Gpu => "#4CAF50",     // Verde (esperado)
            _ => "#607D8B"                // Cinza azulado (Capped/Menu)
        };

        return (smoothed, _activeLimiter, badgeColor);
    }

    private static float? Average(IEnumerable<float?> values)
    {
        var valid = values.Where(v => v.HasValue).Select(v => v!.Value).ToList();
        return valid.Count > 0 ? valid.Average() : null;
    }
}

public static class BottleneckAnalyzer
{
    public static LoadSample Sample(SensorMonitor m, FrameStats? frames)
    {
        var rows = m.Rows;
        var cores = rows.Where(r => r.HardwareType == HardwareType.Cpu && r.Type == SensorType.Load &&
                                    r.Name.StartsWith("CPU Core #") && !r.Name.Contains("Thread") && r.RawValue != null).ToList();
        var busiest = cores.OrderByDescending(r => r.RawValue).FirstOrDefault();

        return new LoadSample(
            DateTime.Now,
            m.GpuLoad,
            m.CpuLoad,
            busiest?.RawValue,
            busiest?.Name.Replace("CPU Core #", "núcleo "),
            m.VramUsagePct,
            m.RamUsagePct,
            m.GpuTemperature,
            m.CpuTemperature,
            false,
            frames?.Fps,
            frames?.Low1Fps,
            frames?.ProcessName
        );
    }

    public static string Title(Limiter l) => l switch
    {
        Limiter.Gpu => "GPU no limite (Cenário Ideal)",
        Limiter.Cpu => "Gargalo de CPU",
        Limiter.Vram => "Memória de Vídeo (VRAM) Cheia",
        Limiter.Ram => "Memória RAM Cheia",
        Limiter.Thermal => "Limitado por Temperatura (Throttling)",
        _ => "Sem gargalo ativo (Trava de FPS ou Menu)"
    };

    public static string Advice(Limiter l, double? maxCpuTemp = null) => l switch
    {
        Limiter.Gpu => "A placa de vídeo está sendo aproveitada a 100%. Para obter mais FPS, diminua a resolução gráfica ou ative redimensionamento (FSR/DLSS/NIS).",
        Limiter.Cpu => "A CPU não está conseguindo acompanhar a placa de vídeo. Reduza opções que usam CPU (distância de visão, multidões, física e sombras complexas).",
        Limiter.Vram => "A VRAM da sua placa de vídeo esgotou! As texturas estão sendo transferidas pela memória RAM do sistema, causando congelamentos e engasgos. Reduza a qualidade das texturas.",
        Limiter.Ram => "Pouca memória RAM livre. O Windows começa a recorrer ao arquivo de paginação no disco. Feche navegadores e programas em segundo plano.",
        Limiter.Thermal => $"Temperatura crítica atingida {(maxCpuTemp != null ? $"({maxCpuTemp:0} °C)" : "")}! O processador/GPU baixou suas frequências de operação drasticamente para proteção física. Verifique coolers, ventilação e pasta térmica.",
        _ => "Nem CPU nem GPU estão no limite: taxa de quadros limitada por V-Sync/G-Sync, limitador de FPS ativado no jogo ou jogo em tela de menu/carregamento."
    };

    public static (double Ratio, string Text) Balance(double gpuIdx, double cpuIdx)
    {
        if (cpuIdx <= 0) return (1.0, "Hardware equilibrado.");
        double ratio = gpuIdx / cpuIdx;
        string text = ratio switch
        {
            > 1.8 => "GPU muito mais forte que a CPU: em 1080p a CPU será o limite com frequência.",
            > 1.3 => "GPU um pouco à frente da CPU: jogos competitivos tendem a ficar limitados pela CPU.",
            >= 0.7 => "Hardware equilibrado: a GPU trabalhará perto de 100% na maioria dos jogos.",
            > 0.4 => "CPU mais forte que a GPU: a placa de vídeo é o limite natural de desempenho.",
            _ => "Placa de vídeo fraca em relação ao processador."
        };
        return (ratio, text);
    }
}

/// <summary>
/// Sessão de gravação de diagnóstico de jogo.
/// </summary>
public sealed class BottleneckSession
{
    private readonly List<LoadSample> _samples = new();
    public DateTime Start { get; } = DateTime.Now;
    public TimeSpan Duration { get; init; }
    public bool IsContinuous => Duration <= TimeSpan.Zero;
    public int Count => _samples.Count;
    public bool Done => !IsContinuous && (DateTime.Now - Start >= Duration);
    public IReadOnlyList<LoadSample> Samples => _samples;

    public void Add(LoadSample s) => _samples.Add(s);

    public IReadOnlyList<(Limiter Limiter, double Pct)> Distribution() =>
        _samples.GroupBy(x => x.Classify())
                .Select(g => (g.Key, 100.0 * g.Count() / Math.Max(1, _samples.Count)))
                .OrderByDescending(x => x.Item2).ToList();

    public Limiter? Main => _samples.Count == 0 ? null : Distribution()[0].Limiter;

    public double? AvgFps => _samples.Where(x => x.Fps > 0).Select(x => x.Fps!.Value).DefaultIfEmpty().Average();
    public double? MinFps => _samples.Where(x => x.Fps > 0).Select(x => x.Fps!.Value).DefaultIfEmpty().Min();
    public double? AvgLow1 => _samples.Where(x => x.Low1Fps > 0).Select(x => x.Low1Fps!.Value).DefaultIfEmpty().Average();

    public double? Avg(Func<LoadSample, float?> sel)
    {
        var v = _samples.Select(x => sel(x)).Where(x => x != null).Select(x => (double)x!.Value).ToList();
        return v.Count > 0 ? v.Average() : null;
    }

    public double? Max(Func<LoadSample, float?> sel)
    {
        var v = _samples.Select(x => sel(x)).Where(x => x != null).Select(x => (double)x!.Value).ToList();
        return v.Count > 0 ? v.Max() : null;
    }

    public double? Max(Func<LoadSample, double?> sel)
    {
        var v = _samples.Select(x => sel(x)).Where(x => x != null).Select(x => x!.Value).ToList();
        return v.Count > 0 ? v.Max() : null;
    }
}
