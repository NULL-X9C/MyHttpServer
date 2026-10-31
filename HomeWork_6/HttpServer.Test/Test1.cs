using System.Data;
using MiniTemplateEngine;

namespace HttpServer.Test;

[TestClass]
public class Tests
{
    private IHtmlTemplateRenderer _renderer = null!;

    [TestInitialize]
    public void Init()
    {
        _renderer = new HtmlTemplateRenderer();
    }

    [TestMethod]
    public void RenderFromString_ReplacesSimpleAndNestedProperties()
    {
        string template = "<h1>${Title}</h1><p>${Author.Name} (${Author.City})</p>";
        var model = new
        {
            Title = "Чистый код",
            Author = new { Name = "Роберт Мартин", City = "Чикаго" }
        };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<h1>Чистый код</h1><p>Роберт Мартин (Чикаго)</p>", html);
    }

    [TestMethod]
    public void RenderFromString_IfInsideForeach_EvaluatesEachItemAndGlobalModel()
    {
        string template =
            "<ul>" +
            "$foreach(var user in Users)" +
                "<li>[${AppName}] ${user.Name}: $if(user.IsVip)VIP$elseОбычный$endif</li>" +
            "$endfor" +
            "</ul>";

        var model = new
        {
            AppName = "Portal",
            Users = new[]
            {
                new { Name = "Анна", IsVip = true },
                new { Name = "Борис", IsVip = false }
            }
        };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<ul><li>[Portal] Анна: VIP</li><li>[Portal] Борис: Обычный</li></ul>", html);
    }

    [TestMethod]
    public void RenderFromString_ForeachInsideIf_RendersLoopOnlyOnTrueBranch()
    {
        string template =
            "$if(ShowTags)" +
                "$foreach(var tag in Tags)#${tag} $endfor" +
            "$else" +
                "Тегов нет" +
            "$endif";

        var activeModel = new { ShowTags = true, Tags = new List<string> { "csharp", "dotnet" } };
        var hiddenModel = new { ShowTags = false, Tags = new List<string> { "csharp", "dotnet" } };

        Assert.AreEqual("#csharp #dotnet ", _renderer.RenderFromString(template, activeModel));
        Assert.AreEqual("Тегов нет", _renderer.RenderFromString(template, hiddenModel));
    }

    [TestMethod]
    public void RenderFromString_NestedIfInsideIf_SkipsInnerElseCorrectly()
    {
        string template =
            "$if(IsAuthorized)" +
                "$if(IsAdmin)" +
                "Панель админа" +
                "$else" +
                "Личный кабинет$endif" +
            "$else" +
                "Вход не выполнен" +
            "$endif";

        var guest = new { IsAuthorized = false, IsAdmin = true };
        var regularUser = new { IsAuthorized = true, IsAdmin = false };
        var adminUser = new { IsAuthorized = true, IsAdmin = true };

        Assert.AreEqual("Вход не выполнен", _renderer.RenderFromString(template, guest));
        Assert.AreEqual("Личный кабинет", _renderer.RenderFromString(template, regularUser));
        Assert.AreEqual("Панель админа", _renderer.RenderFromString(template, adminUser));
    }

    [TestMethod]
    public void RenderFromString_InvalidForeachSyntax_ThrowsFormatExceptionAtBoundary()
    {
        string brokenTemplate = "$foreach(invalid syntax)<li>Тест</li>$endfor";
        var model = new { };

        Assert.ThrowsExactly<SyntaxErrorException>(() => _renderer.RenderFromString(brokenTemplate, model));
    }

    [TestMethod]
    public void RenderToFile_IsIdempotent_AndWritesExpectedHtml()
    {
        string inputPath = Path.GetTempFileName();
        string outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.html");

        try
        {
            File.WriteAllText(inputPath, "<span>${Message}</span>");
            var model = new { Message = "Готово" };

            string firstRun = _renderer.RenderToFile(inputPath, outputPath, model);
            string secondRun = _renderer.RenderToFile(inputPath, outputPath, model);

            Assert.AreEqual("<span>Готово</span>", firstRun);
            Assert.AreEqual(firstRun, secondRun);
            Assert.AreEqual("<span>Готово</span>", File.ReadAllText(outputPath));
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }
}