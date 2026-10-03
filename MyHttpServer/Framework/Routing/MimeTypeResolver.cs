namespace MyHttpServer.Framework.Routing;

public class MimeTypeResolver
{
    private static Dictionary<string, string?> _mime = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".html", "text/html" },
        { ".htm", "text/html" },
        { ".css", "text/css" },
        { ".js", "application/javascript" },
        { ".json", "application/json" },
        { ".xml", "application/xml" },
        { ".txt", "text/plain" },

        // Изображения
        { ".png", "image/png" },
        { ".jpg", "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".gif", "image/gif" },
        { ".svg", "image/svg+xml" },
        { ".webp", "image/webp" },
        { ".ico", "image/x-icon" },

        // Документы
        { ".pdf", "application/pdf" },
        { ".doc", "application/msword" },
        { ".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" },
        { ".xls", "application/vnd.ms-excel" },
        { ".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" },

        // Архивы
        { ".zip", "application/zip" },
        { ".rar", "application/vnd.rar" },
        { ".7z", "application/x-7z-compressed" }
    };

    public string GetType(string extension) =>
        (_mime.GetValueOrDefault(extension, "application/octet-stream"))!;
        
}