using System.Collections.ObjectModel;
using LibreHardwareMonitor.Hardware;

namespace GameBottleneckDetector;

public sealed class SensorMonitor : IDisposable
{
    private readonly Computer _computer;
    private readonly Dictionary<string, SensorRow> _index = new();
    private volatile bool _open;

    public ObservableCollection<SensorRow> Rows { get; } = new();
    public bool IsOpen => _open;

    public string CpuName { get; private set; } = "CPU";
    public string GpuName { get; private set; } = "GPU";

    public SensorMonitor()
    {
        _computer = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsMotherboardEnabled = true,
            IsStorageEnabled = false,
            IsNetworkEnabled = false,
            IsControllerEnabled = false
        };
    }

    public Task OpenAsync() => Task.Run(() =>
    {
        _computer.Open();
        _open = true;

        foreach (var hw in _computer.Hardware)
        {
            if (hw.HardwareType == HardwareType.Cpu && CpuName == "CPU")
                CpuName = hw.Name;
            if ((hw.HardwareType == HardwareType.GpuNvidia || hw.HardwareType == HardwareType.GpuAmd || hw.HardwareType == HardwareType.GpuIntel) && GpuName == "GPU")
                GpuName = hw.Name;
        }
    });

    public void Refresh()
    {
        if (!_open) return;
        foreach (var hw in _computer.Hardware)
            Walk(hw);
    }

    private void Walk(IHardware hw)
    {
        hw.Update();
        foreach (var s in hw.Sensors)
        {
            string id = s.Identifier.ToString();
            if (!_index.TryGetValue(id, out var row))
            {
                row = new SensorRow
                {
                    Id = id,
                    Hardware = hw.Name,
                    Category = s.SensorType.ToString(),
                    Name = s.Name,
                    Type = s.SensorType,
                    HardwareType = hw.HardwareType
                };
                _index[id] = row;
                Rows.Add(row);
            }
            row.Update(s.Value, s.Min, s.Max);
        }
        foreach (var sub in hw.SubHardware)
            Walk(sub);
    }

    /// <summary>Temperatura da CPU: Package (Intel) / Tctl-Tdie (AMD), senão o maior núcleo.</summary>
    public float? CpuTemperature
    {
        get
        {
            var temps = Rows.Where(r => r.HardwareType == HardwareType.Cpu
                                     && r.Type == SensorType.Temperature
                                     && r.RawValue is > 0).ToList();
            var main = temps.FirstOrDefault(r => r.Name.Contains("Package") || r.Name.Contains("Tctl") || r.Name.Contains("Tdie"));
            return main?.RawValue ?? (temps.Count > 0 ? temps.Max(r => r.RawValue) : null);
        }
    }

    /// <summary>Carga total da CPU em %.</summary>
    public float? CpuLoad => Rows.FirstOrDefault(r => r.HardwareType == HardwareType.Cpu
                                                   && r.Type == SensorType.Load
                                                   && r.Name == "CPU Total")?.RawValue;

    /// <summary>Carga da GPU em %.</summary>
    public float? GpuLoad
    {
        get
        {
            var gpus = Rows.Where(r => (r.HardwareType == HardwareType.GpuNvidia || r.HardwareType == HardwareType.GpuAmd || r.HardwareType == HardwareType.GpuIntel)
                                    && r.Type == SensorType.Load
                                    && (r.Name == "GPU Core" || r.Name == "D3D 3D")).ToList();
            return gpus.FirstOrDefault(r => r.Name == "GPU Core")?.RawValue ?? gpus.FirstOrDefault()?.RawValue;
        }
    }

    /// <summary>Temperatura da GPU em °C.</summary>
    public float? GpuTemperature
    {
        get
        {
            var temps = Rows.Where(r => (r.HardwareType == HardwareType.GpuNvidia || r.HardwareType == HardwareType.GpuAmd || r.HardwareType == HardwareType.GpuIntel)
                                     && r.Type == SensorType.Temperature).ToList();
            return temps.FirstOrDefault(r => r.Name == "GPU Core")?.RawValue ?? temps.FirstOrDefault()?.RawValue;
        }
    }

    /// <summary>Uso de VRAM em %.</summary>
    public double? VramUsagePct
    {
        get
        {
            var used = Rows.FirstOrDefault(r => (r.HardwareType == HardwareType.GpuNvidia || r.HardwareType == HardwareType.GpuAmd)
                                             && r.Name.Contains("GPU Memory Used"))?.RawValue;
            var total = Rows.FirstOrDefault(r => (r.HardwareType == HardwareType.GpuNvidia || r.HardwareType == HardwareType.GpuAmd)
                                              && r.Name.Contains("GPU Memory Total"))?.RawValue;
            if (used != null && total is > 0)
                return 100.0 * used.Value / total.Value;

            // Alternativa: Sensor de Load "GPU Memory"
            var memLoad = Rows.FirstOrDefault(r => (r.HardwareType == HardwareType.GpuNvidia || r.HardwareType == HardwareType.GpuAmd)
                                                && r.Type == SensorType.Load
                                                && r.Name == "GPU Memory")?.RawValue;
            return memLoad;
        }
    }

    /// <summary>Uso de RAM em %.</summary>
    public float? RamUsagePct => Rows.FirstOrDefault(r => r.HardwareType == HardwareType.Memory
                                                       && r.Type == SensorType.Load
                                                       && r.Name == "Memory")?.RawValue;

    public static bool IsElevated =>
        new System.Security.Principal.WindowsPrincipal(System.Security.Principal.WindowsIdentity.GetCurrent())
            .IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);

    public void Dispose()

    {
        if (_open)
        {
            try { _computer.Close(); } catch { }
        }
    }
}
