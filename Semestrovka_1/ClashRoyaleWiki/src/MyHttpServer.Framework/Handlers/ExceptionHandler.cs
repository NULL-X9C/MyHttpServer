using System.Net;

namespace MyHttpServer.Framework.Handlers;

public class ExceptionHandler(string homeUrl = "/") : BaseHandler
{
    public override async Task HandleAsync(HttpListenerContext context)
    {
        try
        {
            await base.HandleAsync(context);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[Global Error] exception: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();

            var response = context.Response;
            response.StatusCode = 500;

            try
            {
                string htmlResponse =
                    $"""
                     <h1>500 — Внутренняя ошибка сервера</h1>
                     <p>Произошла ошибка при обработке запроса.</p>
                     <p><a href="{homeUrl}">Вернуться на главную</a></p>
                     """;
                await SendTextAsync(htmlResponse, response);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                response.Close();
            }
        }
    }
}