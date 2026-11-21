using System.Text;
using System.Text.Json;

namespace MyHttpServer.Framework.Results;

public class JsonResult(object data) : IActionResult
{
    public string ContentType => "application/json; charset=utf-8";
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public byte[] ExecuteResult()
    {
        string json = JsonSerializer.Serialize(data, JsonOptions);
        return Encoding.UTF8.GetBytes(json);
    }
}