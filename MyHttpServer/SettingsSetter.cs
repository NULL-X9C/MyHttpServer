using System.Text.Json;

namespace MyHttpServer;

public class SettingsSetter
{
    public static async Task<string> Read(string path) => await File.ReadAllTextAsync(path);

    public static Settings? SetSettings(string json)
    {
        return JsonSerializer.Deserialize<Settings>(json);
    }
}