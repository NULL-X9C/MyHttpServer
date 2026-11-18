namespace MyHttpServer.Framework.Results;

public interface IActionResult
{
    string ContentType { get; }
    byte[] ExecuteResult();
}