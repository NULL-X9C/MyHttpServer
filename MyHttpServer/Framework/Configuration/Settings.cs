using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyHttpServer.Framework.Configuration;

public class Settings
{
    [JsonConstructor]
    private Settings()
    {
        
    }
    public Server Server { get; set; } = new Server();
    
    public static class SingleSettings
    {
        private static readonly Lazy<Settings?> Instance = new(() => SetSettings(@"Framework/Configuration/settings.json"));

        public static Settings? Current => Instance.Value;
        
        public static string Read(string path) => File.ReadAllText(path);

        public static Settings? SetSettings(string jsonPath)
        {
            var json =  Read(jsonPath);
            return JsonSerializer.Deserialize<Settings>(json,
                new JsonSerializerOptions{PropertyNameCaseInsensitive = true});
        }

    }
}

public class Server
{
    public string Host { get; set; } = "";
    public string Port { get; set; } = "";
    public string Path { get; set; } = "";
}