using System.Net;
using MiniTemplateEngine;
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

    protected IActionResult Page(string templatePath, object dataModel, IHtmlTemplateRenderer? renderer = null) =>
        new PageResult(templatePath, dataModel, renderer);

    protected IActionResult Redirect(string url)
    {
        Context.Response.StatusCode = 303;
        Context.Response.RedirectLocation = url;
        return new HtmlResult(string.Empty);
    }

    protected string? GetCookie(string name) => Context.Request.Cookies[name]?.Value;

    protected void SetHttpOnlyCookie(string name, string value, int maxAgeSeconds) =>
        Context.Response.Headers.Add("Set-Cookie",
            $"{name}={value}; Path=/; HttpOnly; SameSite=Lax; Max-Age={maxAgeSeconds}");

    protected void DeleteCookie(string name) => Context.Response.Headers.Add("Set-Cookie",
        $"{name}=; Path=/; HttpOnly; SameSite=Lax; Max-Age=0");
}
