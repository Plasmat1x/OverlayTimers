using System.IO;
using System.Text.Json;

namespace OverlayTimers;

public class AppConfig
{
    public string ConfigHotkey { get; set; } = "O";
    public List<TimerSettings> Timers { get; set; } = [];

    public static AppConfig Load()
    {
       if(!File.Exists("config.json")) return new AppConfig();
        string json = File.ReadAllText("config.json"); ;
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public void Save()
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true});
        File.WriteAllText("config.json", json);
    }
}


