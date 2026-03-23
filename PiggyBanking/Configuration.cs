using Newtonsoft.Json;
using TShockAPI;

namespace PiggyBanking;

public class Configuration
{
    private static readonly string ConfigPath = Path.Combine(TShock.SavePath, "PiggyBanking.json");

    [JsonProperty("Enable")]
    public bool Enable { get; set; } = true;

    [JsonProperty("ShowMessage")]
    public bool ShowMessage { get; set; } = true;

    public static Configuration Load()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                var config = new Configuration();
                config.Save();
                return config;
            }

            var json = File.ReadAllText(ConfigPath);
            return JsonConvert.DeserializeObject<Configuration>(json) ?? new Configuration();
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[PiggyBanking] Error loading config: {ex.Message}");
            return new Configuration();
        }
    }

    public void Save()
    {
        try
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        catch (Exception ex)
        {
            TShock.Log.ConsoleError($"[PiggyBanking] Error saving config: {ex.Message}");
        }
    }
}