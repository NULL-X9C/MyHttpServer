using System.Net;

namespace MyHttpServer.Framework.Http.Loging;

public class HttpLoger
{
    public static void LogRequestInfo(HttpListenerContext context)
    {
        var request = context.Request;
        string time = DateTime.Now.ToString("HH:mm:ss.fff");

        string method = request.HttpMethod;
        string rawUrl = request.RawUrl ?? "Unknown URL";
        string clientIp = request.RemoteEndPoint?.ToString() ?? "Unknown IP";
        string userAgent = request.UserAgent ?? "Unknown User-Agent";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{time}] {method} {rawUrl}");
        Console.ResetColor();
        Console.WriteLine($"    -> Клиент: {clientIp}");
        Console.WriteLine($"    -> Браузер: {userAgent}");
        Console.WriteLine(request.Url.LocalPath);
        Console.WriteLine(new string('-', 50));
    }
}