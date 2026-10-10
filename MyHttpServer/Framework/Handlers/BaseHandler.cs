using System.Net;

namespace MyHttpServer.Framework.Handlers;

public abstract class BaseHandler
{
    private BaseHandler? _next;

    public BaseHandler SetNext(BaseHandler next)
    {
        _next = next;
        return next;
    }

    public virtual async Task HandleAsync(HttpListenerContext context)
    {
        if (_next != null)
        {
            await _next.HandleAsync(context);
        }
    }
    
    // extensions
    
    public async Task SendResponseAsync(string filePath, HttpListenerResponse response)
    {
        byte[] buffer = await File.ReadAllBytesAsync(filePath);
        response.ContentLength64 = buffer.Length;
        await using Stream output = response.OutputStream;
        await output.WriteAsync(buffer);
        await output.FlushAsync();
        Console.WriteLine($"Файл отдан: {filePath}");
    }
    
    public async Task SendTextAsync(string text, HttpListenerResponse response)
    {
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(text);
        response.ContentLength64 = buffer.Length;
        await using Stream output = response.OutputStream;
        await output.WriteAsync(buffer);
        await output.FlushAsync();
    }
}