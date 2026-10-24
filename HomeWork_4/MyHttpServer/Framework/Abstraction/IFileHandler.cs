using System.Net;

namespace MyHttpServer.Framework.Abstraction;

public interface IFileHandler
{
    public async Task ExecuteRequestAsync(HttpListenerContext context)
    {
        
    }
}