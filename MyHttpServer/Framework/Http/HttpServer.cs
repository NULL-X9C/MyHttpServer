using System.Net;
using System.Text;
using MyHttpServer.Framework.Configuration;

namespace MyHttpServer.Framework.Http;

public class HttpServer
{
    private readonly HttpListener _server = new HttpListener();

    public async Task StartAsynk(CancellationToken cancellationToken)
    {
        try
        {
            var settings = Settings.SingleSettings.Current;

            // установка адресов прослушки
            if (settings != null)
            {
                Console.WriteLine($"pefix: http://{settings.Server.Host}:{settings.Server.Port}/HomeWork_3/");
                var prefix = $"http://{settings.Server.Host}:{settings.Server.Port}/";
                _server.Prefixes.Add(prefix);
            }
            else
            {
                throw new NullReferenceException();
            }

            Console.WriteLine(_server.Prefixes);

            _server.Start(); // начинаем прослушивать входящие подключения
            await using var registration = cancellationToken.Register(() => _server.Stop());

            // получаем контекст
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _server.GetContextAsync();
                    Console.WriteLine("Пришел запрос");
                    SendResponseAsync(context, cancellationToken);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("ERR:" + e);
            throw;
        }
        finally
        {
            _server.Close();
        }
    }

    private async Task SendResponseAsync(HttpListenerContext context, CancellationToken  cancellationToken)
    {
        var request = context.Request;
        
        var path = request.Url!.LocalPath ?? "/";
        var response = context.Response;

        var filePath = Directory.GetCurrentDirectory() + $"/HomeWork_3/static/{path.TrimStart('/')}";
        
        if (!File.Exists(filePath))
        {
            response.StatusCode = 404;
            filePath = Directory.GetCurrentDirectory() + $"/HomeWork_3/static/404.html";
        }
        
        FileInfo fileInfo = new FileInfo(filePath);

        Console.WriteLine(path);

        switch (fileInfo.Extension)
        {
            // case ".html":
            //     response.ContentType = "text/html; charset=utf-8";
            //     break;
            case ".css":
                response.ContentType = "text/css; charset=utf-8";
                break;
            case ".js":
                response.ContentType = "text/javascript; charset=utf-8";
                break;
            case ".png":
                response.ContentType = "image/png";
                break;
            case ".ico":
                response.ContentType = "image/x-icon";
                break;
            case ".svg":
                response.ContentType = "image/svg+xml";
                break;
            case ".jpg":
                response.ContentType = "image/jpeg";
                break;
        }

        LogRequestInfo(request);

        // отправляемый в ответ код htmlвозвращает
        // var filePath = Directory.GetCurrentDirectory() + $"/static{p}"
        // byte[] buffer = Encoding.UTF8.GetBytes(responseText);

        byte[] buffer = await File.ReadAllBytesAsync(filePath, cancellationToken);

        // получаем поток ответа и пишем в него ответ
        response.ContentLength64 = buffer.Length;
        await using Stream output = response.OutputStream;
        // отправляем данные
        await output.WriteAsync(buffer);
        await output.FlushAsync(cancellationToken);
        Console.WriteLine("Запрос обработан введите exit для остановки");
    }

    private void LogRequestInfo(HttpListenerRequest request)
    {
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