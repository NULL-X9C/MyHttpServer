using MyHttpServer.Framework.Configuration;
using MyHttpServer.Framework.Handlers;
using MyHttpServer.Framework.Http;
using MyHttpServer.Framework.Routing;

namespace MyHttpServer;

public class Welcome
{
    public async Task Run()
    {
        while (true)
        {
            Console.WriteLine("Напишите start для запуска сервера, exit - для выхода из программы ");
            var userCommand = Console.ReadLine();
            if (userCommand?.ToLower() == "exit")
            {
                break;
            }

            if (userCommand?.Trim().ToLower() == "start")
            {
                var root = Path.Combine(Directory.GetCurrentDirectory(), "static");
                var settings = Settings.SingleSettings.Current;
                var mimeResolver = new MimeTypeResolver();

                var exceptionHandler = new ExceptionHandler();
                var staticHandler = new StaticFilesHandler(root, mimeResolver);
                var controllersHandler = new ControllersHandler();
                var notFoundHandler = new NotFoundHandler(root);

                exceptionHandler
                    .SetNext(staticHandler)
                    .SetNext(controllersHandler)
                    .SetNext(notFoundHandler);

                var server = new HttpServer(settings, exceptionHandler);
                var serverHost = new ServerHost(server);
                await serverHost.StartHostAsync();
            }
        }
    }
}