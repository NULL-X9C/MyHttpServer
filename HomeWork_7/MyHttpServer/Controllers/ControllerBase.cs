using System.Net;
using MyHttpServer.Framework.Results;

namespace MyHttpServer.Framework;

public abstract class ControllerBase
{
    protected HttpListenerContext Context { get; private set; } = null!;

    public void SetContext(HttpListenerContext context)
    {
        Context = context;
    }

    protected IActionResult Html(string html) => new HtmlResult(html);
    protected IActionResult Json(object data) => new JsonResult(data);
}