using System.Net;

namespace MyHttpServer.Framework.Handlers;

public class ExceptionHandler : BaseHandler
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
                await SendTextAsync("<h1>500 - error</h1>", response);
            }
            catch
            {
            }
            finally
            {
                response.Close();
            }
        }
    }
}