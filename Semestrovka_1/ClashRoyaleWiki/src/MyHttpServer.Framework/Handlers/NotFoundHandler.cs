using System.Net;

namespace MyHttpServer.Framework.Handlers;

public class NotFoundHandler(string staticFolder, string homeUrl = "/") : BaseHandler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        var response = context.Response;
        response.StatusCode = 404;

        try
        {
            var notFoundFilePath = Path.Combine(staticFolder, "404.html");
            if (File.Exists(notFoundFilePath))
            {
                response.ContentType = "text/html; charset=utf-8";
                await SendResponseAsync(notFoundFilePath, response);
            }
            else
            {
                string htmlResponse =
                    $"""
                     <h1>404 — Страница не найдена</h1>
                     <p><a href="{homeUrl}">Вернуться на главную</a></p>
                     """;
                await SendTextAsync(htmlResponse, response);
            }
        }
        finally
        {
            response.Close();
        }
    }
}