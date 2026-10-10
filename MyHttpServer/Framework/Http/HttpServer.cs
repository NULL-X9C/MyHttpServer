using System.Net;
using System.Text;
using MyHttpServer.Framework.Abstraction;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Handlers;
using MyHttpServer.Framework.Http.Loging;
using MyHttpServer.Framework.Routing;

namespace MyHttpServer.Framework.Http;

public class HttpServer
{
    private readonly HttpListener _server = new HttpListener();
    private readonly AbstractSettings<Server> _settings;
    private readonly BaseHandler _handler;


    public HttpServer(AbstractSettings<Server> settings, BaseHandler handler)
    {
        _settings = settings;
        _handler = handler;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            // установка адресов прослушки
            {
                Console.WriteLine($"pefix: http://{_settings.Server.Host}:{_settings.Server.Port}/static/");
                var prefix = $"http://{_settings.Server.Host}:{_settings.Server.Port}/";
                _server.Prefixes.Add(prefix);
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
                    HttpLoger.LogRequestInfo(context);
                    Task.Run(() => _handler.HandleAsync(context));
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
}