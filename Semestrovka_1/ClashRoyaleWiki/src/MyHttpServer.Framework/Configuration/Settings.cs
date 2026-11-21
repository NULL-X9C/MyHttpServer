using System.Text.Json;
using System.Text.Json.Serialization;
using MyHttpServer.Framework.Abstraction;

namespace MyHttpServer.Framework.Configuration;

public class Settings : AbstractSettings<Server>
{
    [JsonConstructor]
    private Settings() { }

    public override Server Server { get; set; } = new();
    public DatabaseSettings Database { get; set; } = new();

    public static class SingleSettings
    {
        private static readonly Lazy<Settings?> Instance = new(() => SetSettings(ResolveDefaultPath()));

        public static Settings? Current => Instance.Value;

        public static Settings? SetSettings(string jsonPath)
        {
            if (!File.Exists(jsonPath))
                throw new FileNotFoundException($"Не найден файл конфигурации: {jsonPath}");

            var json = DefauilRead(jsonPath);
            var settings = JsonSerializer.Deserialize<Settings>(json, DefaulJsonSerializerOptions);

            if (settings != null)
            {
                var envHost = Environment.GetEnvironmentVariable("SERVER_HOST");
                var envPort = Environment.GetEnvironmentVariable("SERVER_PORT");
                var envConn = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");

                if (!string.IsNullOrWhiteSpace(envHost)) settings.Server.Host = envHost;
                if (!string.IsNullOrWhiteSpace(envPort)) settings.Server.Port = envPort;
                if (!string.IsNullOrWhiteSpace(envConn)) settings.Database.ConnectionString = envConn;
            }

            return settings;
        }

        private static string ResolveDefaultPath()
        {
            string[] candidates =
            [
                Path.Combine(AppContext.BaseDirectory, "settings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "settings.json"),
                Path.Combine(Directory.GetCurrentDirectory(), "Framework", "Configuration", "settings.json")
            ];

            return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
        }
    }
}

public class Server
{
    public string Host { get; set; } = "";
    public string Port { get; set; } = "";
    public string Path { get; set; } = "";
}

public class DatabaseSettings
{
    public string ConnectionString { get; set; } = "";
}