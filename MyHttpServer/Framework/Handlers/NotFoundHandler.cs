using System.Net;

namespace MyHttpServer.Framework.Handlers;

public class NotFoundHandler(string staticFolder) : BaseHandler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        var response = context.Response;
        response.StatusCode = 404;

        var filePath = Path.Combine(staticFolder, "404.html");
        await SendResponseAsync(filePath, response);
    }
}