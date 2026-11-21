using System.Text;
using MiniTemplateEngine;

namespace MyHttpServer.Framework.Results;

public class PageResult : IActionResult
{
    private readonly string _templatePath;
    private readonly object _dataModel;
    private readonly IHtmlTemplateRenderer _renderer;

    public string ContentType { get; }  = "text/html; charset=utf-8";
    
    public PageResult(string templatePath, object dataModel, IHtmlTemplateRenderer? renderer = null)
    {
        _templatePath = ResolveTemplatePath(templatePath);
        _dataModel = dataModel;
        _renderer = renderer ?? new HtmlTemplateRenderer();
    }


    public byte[] ExecuteResult()
    {
        string renderedHtml = _renderer.RenderFromFile(_templatePath, _dataModel);
        return Encoding.UTF8.GetBytes(renderedHtml);
    }

    private static string ResolveTemplatePath(string path)
    {
        string fromBaseDir = Path.Combine(AppContext.BaseDirectory, path);
        if (File.Exists(fromBaseDir))
            return fromBaseDir;

        string fromCurrentDir = Path.Combine(Directory.GetCurrentDirectory(), path);
        if (File.Exists(fromCurrentDir))
            return fromCurrentDir;

        string projectRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        string fromProjectRoot = Path.Combine(projectRoot, path);
        if (File.Exists(fromProjectRoot))
            return fromProjectRoot;

        return path;
    }
}