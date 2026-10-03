using System.Text.Json;

namespace MyHttpServer.Framework.Configuration;

public class SettingsSetter
{
    public static async Task<string> Read(string path) => await File.ReadAllTextAsync(path);

    public static async Task<Settings?> SetSettings(string jsonPath)
    {
        return JsonSerializer.Deserialize<Settings>(await Read(jsonPath));
    }
}