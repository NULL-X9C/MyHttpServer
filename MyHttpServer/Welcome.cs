namespace MyHttpServer;

public class Welcome
{
    public async Task RunUserOrder()
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
                var server = new HttpServer();
                var serverHost = new ServerHost(server);

                await serverHost.StartHostAsync();
            }

        }
        
    }
}