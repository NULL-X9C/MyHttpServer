using System.Text;

namespace MyHttpServer.Framework.Results;

public class HtmlResult(string html) : IActionResult
{
    public string ContentType { get; } = "text/html; charset=utf-8";

    public byte[] ExecuteResult()
    {
        return Encoding.UTF8.GetBytes(html);
    }
}
