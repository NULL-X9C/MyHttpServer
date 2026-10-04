using MyHttpServer.Framework.Configuration;
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
            if (userCommand?.Trim().ToLower() == "exit")
            {
                break;
            }

            if (userCommand?.Trim().ToLower() == "start")
            {
                var root = Directory.GetCurrentDirectory() + @"\HomeWork_3\static";
                var settings = Settings.SingleSettings.Current;
                var mimeResolver = new MimeTypeResolver();
                var server = new HttpServer(settings ?? throw new InvalidOperationException("Settings is null((("),
                    new FileHandler(root, mimeResolver));
                var serverHost = new ServerHost(server);

                await serverHost.StartHostAsync();
            }
        }
    }
}