using System.Net;

namespace MyHttpServer.Framework.Handlers;

public class NotFoundHandler(string staticFolder) : BaseHandler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        var response = context.Response;
        response.StatusCode = 404;

        var filePath = Path.Combine(staticFolder, "404.html");
        if (File.Exists(filePath))
        {
            await SendResponseAsync(filePath, response);
        }
        else
        {
            await SendTextAsync("<p> 404 - не найден</p>", response);
            throw new FileNotFoundException(
                "ВНИМАНИЕ!!! говнокодер не добавил файл 404.html Рекомендация: оторвать ему руки ");
        }

        response.Close();
    }
}