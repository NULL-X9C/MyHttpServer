using System.Net;
using System.Text;
using System.Text.Json;

namespace MyHttpServer;

public class HttpServer
{
    private readonly HttpListener _server = new HttpListener();
                         
    public async Task StartAsynk(CancellationToken cancellationToken)
    {
        try
        {
            var settigsText = await SettingsSetter.Read("settings.json");
            var settings = SettingsSetter.SetSettings(settigsText);

            // установка адресов прослушки
            Console.WriteLine($"http://{settings.Server.Host}:{settings.Server.Port}/{settings.Server.Path}");

            _server.Prefixes.Add($"http://{settings.Server.Host}:{settings.Server.Port}/{settings.Server.Path}");
            Console.WriteLine(_server.Prefixes);

            _server.Start(); // начинаем прослушивать входящие подключения
            await using var registration = cancellationToken.Register(() => _server.Stop());

            // получаем контекст
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {

                    var context = await _server.GetContextAsync();

                    var response = context.Response;
                    var request = context.Request;
                    
                    LogRequestInfo(request);
                    
                    // отправляемый в ответ код htmlвозвращает
                    string responseText = File.ReadAllText("index.html");

                    byte[] buffer = Encoding.UTF8.GetBytes(responseText);
                    // получаем поток ответа и пишем в него ответ
                    response.ContentLength64 = buffer.Length;
                    await using Stream output = response.OutputStream;
                    // отправляем данные
                    await output.WriteAsync(buffer);
                    await output.FlushAsync();
                    Console.WriteLine("Запрос обработан введите exit для остановки");
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
        Console.WriteLine(new string('-', 50));
    }
}