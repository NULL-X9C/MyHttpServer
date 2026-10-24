namespace MyHttpServer.Framework.Http;

public class ServerHost(HttpServer server)
{
    public async Task StartHostAsync()
    {
        Console.WriteLine("Запуск");
        var cts = new CancellationTokenSource();
        var serverTask = Task.Run(() => server.StartAsync(cts.Token));

        Console.WriteLine("Сервер поднят введите exit для остановки ");
        while (true)
        {
            var command = Console.ReadLine();
            if (command?.Trim().ToLower() == "exit")
            {
                Console.WriteLine("Начат процесс остановки сервера ");
               cts.Cancel();
               break;
            }
            else
            {
                Console.WriteLine("для остановки напишите exit");
            }
        }

        await serverTask;
        Console.WriteLine("Сервер остановлен");
    }
    
}