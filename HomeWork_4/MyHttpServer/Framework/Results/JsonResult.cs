using System.Text;
using System.Text.Json;

namespace MyHttpServer.Framework.Results;

public class JsonResult(object data) : IActionResult
{
    public string ContentType => "application/json; charset=utf-8";

    public byte[] ExecuteResult()
    {
        // Автоматически превращаем объект в JSON-строку
        string json = JsonSerializer.Serialize(data);
        return Encoding.UTF8.GetBytes(json);
    }
}