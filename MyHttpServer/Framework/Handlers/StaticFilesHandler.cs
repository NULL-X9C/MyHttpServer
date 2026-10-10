using System.Net;
using MyHttpServer.Framework.Routing;

namespace MyHttpServer.Framework.Handlers;

public class StaticFilesHandler(string staticFolder, MimeTypeResolver mimeTypeResolver) : BaseHandler
{
    public override async Task HandleAsync(HttpListenerContext context)
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
                await base.HandleAsync(context);
                context.Response.Close();
                return;
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
            //исправить чтобы при передаче не вызывал 
            context.Response.Close();
        }
    }
}