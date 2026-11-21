namespace MyHttpServer.Framework.Abstraction;

using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

public abstract class AbstractSettings<TServer>
{
    public abstract TServer Server { get; set; }
    protected static readonly JsonSerializerOptions DefaulJsonSerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [JsonConstructor]
    protected AbstractSettings() { }
    protected static string DefauilRead(string path) => File.ReadAllText(path);

    protected static T? BaseSetSettings<T>(string jsonPath) where T : AbstractSettings<TServer>
    {
        var json = DefauilRead(jsonPath);
        return JsonSerializer.Deserialize<T>(json, DefaulJsonSerializerOptions);
    }
}
