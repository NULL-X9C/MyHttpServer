using System.Net;
using MyHttpServer.Framework.Abstraction;
using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Handlers;
using MyHttpServer.Framework.Http.Loging;

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
            var prefix = $"http://{_settings.Server.Host}:{_settings.Server.Port}{_settings.Server.Path}";
            Console.WriteLine($"Prefix: {prefix}");
            _server.Prefixes.Add(prefix);

            _server.Start();
            await using var registration = cancellationToken.Register(() => _server.Stop());

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var context = await _server.GetContextAsync();
                    Console.WriteLine("Пришел запрос");
                    HttpLoger.LogRequestInfo(context);

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _handler.HandleAsync(context);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Unhandled Request Error]: {ex}");
                        }
                    }, cancellationToken);
                }
                catch (HttpListenerException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[Server Loop Error]: {e.Message}");
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("ERR: " + e);
            throw;
        }
        finally
        {
            _server.Close();
        }
    }
}