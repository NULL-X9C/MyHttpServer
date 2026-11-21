using System.Net;
using MyHttpServer.Framework.Routing;

namespace MyHttpServer.Framework.Handlers;

public class StaticFilesHandler(string staticFolder, MimeTypeResolver mimeTypeResolver, string homeUrl="/") : BaseHandler
{
    private readonly string _staticFolder = Path.GetFullPath(staticFolder)
        .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    public override async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url!.LocalPath;
        var filePath = Path.GetFullPath(Path.Combine(_staticFolder, path.TrimStart('/')));

        if (!filePath.StartsWith(_staticFolder + Path.DirectorySeparatorChar) && filePath != _staticFolder)
        {
            response.StatusCode = 403;
            var forbiddenPath = Path.Combine(_staticFolder, "403.html");
            if (File.Exists(forbiddenPath))
            {
                response.ContentType = "text/html; charset=utf-8";
                await SendResponseAsync(forbiddenPath, response);
            }
            else
            {
                await SendTextAsync($"<h1>403 — Доступ запрещен</h1><a href='{homeUrl}'>На главную</a>", response);
            }
            response.Close();
            return;
        }

        if (Directory.Exists(filePath))
        {
            var indexHtmlPath = Path.Combine(filePath, "index.html");
            if (File.Exists(indexHtmlPath))
            {
                if (!path.EndsWith("/"))
                {
                    response.StatusCode = 301;
                    response.Headers.Add("Location", path + "/");
                    response.Close();
                    return;
                }

                filePath = indexHtmlPath;
            }
        }

        if (File.Exists(filePath))
        {
            try
            {
                string ext = Path.GetExtension(filePath);
                string mime = mimeTypeResolver.GetType(ext);
                response.ContentType = mime;
                await SendResponseAsync(filePath, response);
            }
            finally
            {
                response.Close();
            }

            return;
        }

        await base.HandleAsync(context);
    }
}