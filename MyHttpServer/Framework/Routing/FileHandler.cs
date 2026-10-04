using System.Net;
using MyHttpServer.Framework.Abstraction;

namespace MyHttpServer.Framework.Routing;

public class FileHandler(string staticFolder, MimeTypeResolver mimeTypeResolver) : IFileHandler
{
    public async Task ExecuteRequestAsync(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;
            var path = request.Url!.LocalPath;
            var filePath = Path.GetFullPath(Path.Combine(staticFolder, path.TrimStart('/')));

            Console.WriteLine(filePath);

            if (!filePath.StartsWith(staticFolder + Path.DirectorySeparatorChar))
            {
                response.StatusCode = 403;
                filePath = Path.Combine(staticFolder, "403.html");
            }
                
            else if (Directory.Exists(filePath))
            {
                if (!path.EndsWith("/"))
                {
                    response.StatusCode = 301;
                    response.Headers.Add("Location", path + "/");
                    return;
                }

                filePath = Path.Combine(filePath, "index.html");
            }

            if (response.StatusCode == 200 && !File.Exists(filePath))
            {
                response.StatusCode = 404;
                filePath = Path.Combine(staticFolder, "404.html");
            }

            string ext = Path.GetExtension(filePath);
            string mime = mimeTypeResolver.GetType(ext);
            response.ContentType = mime;
            if (File.Exists(filePath))
            {
                await SendResponseAsync(filePath, response);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            context.Response.StatusCode = 500;
        }
        finally
        {
            context.Response.Close();
        }
    }

    private async Task SendResponseAsync(string filePath, HttpListenerResponse response)
    {
        try
        {
            byte[] buffer = await File.ReadAllBytesAsync(filePath);

            // получаем поток ответа и пишем в него ответ
            response?.ContentLength64 = buffer.Length;
            await using Stream output = response.OutputStream;
            // отправляем данные
            await output.WriteAsync(buffer);
            await output.FlushAsync();
            Console.WriteLine("Запрос обработан введите exit для остановки");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }
}