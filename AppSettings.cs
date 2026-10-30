using System.IO;
using System.Text.Json;

namespace GameBottleneckDetector;

public enum MemoryDisplayMode
{
    Percentage,
    Gigabytes,
    Megabytes
}

public sealed class AppSettings
{
    public MemoryDisplayMode RamDisplayMode { get; set; } = MemoryDisplayMode.Gigabytes;
    public MemoryDisplayMode VramDisplayMode { get; set; } = MemoryDisplayMode.Gigabytes;
    public bool CompanionEnabled { get; set; } = true;
    public int CompanionPort { get; set; } = 5150;
    public int MarkerWindowSeconds { get; set; } = 30;

    private static readonly string SettingsFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                string json = File.ReadAllText(SettingsFile);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch { }
    }
}
