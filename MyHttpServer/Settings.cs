namespace MyHttpServer;

public class Settings
{
    public Server Server { get; set; } = new Server();
}

public class Server
{
    public string Host { get; set; } = "";
    public string Port { get; set; } = "";
    public string Path { get; set; } = "";
}