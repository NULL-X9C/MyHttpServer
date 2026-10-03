using System.Net;

namespace MyHttpServer.Framework.Routing;

public class FileHandler(string StaticFolder, MimeTypeResolver mimeTypeResolver)
{
    private readonly string _staticFolder = StaticFolder;
    private readonly MimeTypeResolver _mimeTypeResolver = mimeTypeResolver;

    public async Task ExequteReqwestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        
        var path = request.Url!.LocalPath;
        var response = context.Response;

        var filePath = Path.GetFullPath(Path.Combine(_staticFolder, path.TrimStart('/')));
        FileInfo fileInfo = new FileInfo(filePath);

        if (!filePath.StartsWith(_staticFolder))
        {
            response.StatusCode = 403;
            filePath = Path.Combine(_staticFolder, "403.html");
        }
    }
}