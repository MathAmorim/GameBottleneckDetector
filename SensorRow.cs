using System.ComponentModel;
using LibreHardwareMonitor.Hardware;

namespace GameBottleneckDetector;

public class SensorRow : INotifyPropertyChanged
{
    public string Id { get; init; } = "";
    public string Hardware { get; init; } = "";
    public string Category { get; init; } = "";
    public string Name { get; init; } = "";
    public SensorType Type { get; init; }
    public HardwareType HardwareType { get; init; }

    private float? _value, _min, _max;
    public float? RawValue => _value;

    public string Value => Format(_value);
    public string Min => Format(_min);
    public string Max => Format(_max);

    public event PropertyChangedEventHandler? PropertyChanged;

    public void Update(float? value, float? min, float? max)
    {
        _value = value;
        _min = min;
        _max = max;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Min)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Max)));
    }

    private string Format(float? v)
    {
        if (v == null) return "—";
        return Type switch
        {
            SensorType.Temperature => $"{v:0.#} °C",
            SensorType.Load or SensorType.Control or SensorType.Level => $"{v:0.#} %",
            SensorType.Clock => $"{v:0} MHz",
            SensorType.Power => $"{v:0.#} W",
            SensorType.Voltage => $"{v:0.###} V",
            SensorType.Fan => $"{v:0} RPM",
            _ => $"{v:0.#}"
        };
    }
}
