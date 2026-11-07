using System.Data;
using MiniTemplateEngine;

namespace HttpServer.Test;

[TestClass]
public class HtmlTemplateRendererTests
{
    private IHtmlTemplateRenderer _renderer = null!;

    [TestInitialize]
    public void Init()
    {
        _renderer = new HtmlTemplateRenderer();
    }

    [TestMethod]
    public void RenderFromString_SingleProperty_ReplacesCorrectly()
    {
        string template = "<h1>${Title}</h1>";
        var model = new { Title = "Чистый код" };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<h1>Чистый код</h1>", html);
    }

    [TestMethod]
    public void RenderFromString_MultipleProperties_ReplacesAllCorrectly()
    {
        string template = "<div>${Title} — ${Author} (${Year})</div>";
        var model = new { Title = "CLR via C#", Author = "Джеффри Рихтер", Year = 2012 };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<div>CLR via C# — Джеффри Рихтер (2012)</div>", html);
    }

    [TestMethod]
    public void RenderFromString_RepeatedProperty_ReplacesAllOccurrences()
    {
        string template = "<a href='/${Slug}' title='${Slug}'>${Slug}</a>";
        var model = new { Slug = "profile" };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<a href='/profile' title='profile'>profile</a>", html);
    }

    [TestMethod]
    public void RenderFromString_ReplacesSimpleAndNestedProperties()
    {
        string template = "<h1>${Title}</h1><p>${Author.Name} (${Author.Address.City})</p>";
        var model = new
        {
            Title = "Грязный код",
            Author = new
            {
                Name = "Фёдор Ларионов",
                Address = new { City = "Казань" }
            }
        };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<h1>Грязный код</h1><p>Фёдор Ларионов (Казань)</p>", html);
    }

    [TestMethod]
    public void RenderFromString_EmptyTemplate_ReturnsEmptyString()
    {
        string template = string.Empty;
        var model = new { Name = "Тест" };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual(string.Empty, html);
    }

    [TestMethod]
    public void RenderFromString_IfConditionTrue_RendersBlock()
    {
        string template = "Статус: $if(IsActive)Активен$endif";
        var model = new { IsActive = true };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("Статус: Активен", html);
    }

    [TestMethod]
    public void RenderFromString_IfConditionFalse_SkipsBlock()
    {
        string template = "Статус: $if(IsActive)Активен$endif";
        var model = new { IsActive = false };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("Статус: ", html);
    }

    [TestMethod]
    public void RenderFromString_IfElseConstruct_RendersCorrectBranch()
    {
        string template = "$if(IsOnline)В сети$elseНе в сети$endif";

        Assert.AreEqual("В сети", _renderer.RenderFromString(template, new { IsOnline = true }));
        Assert.AreEqual("Не в сети", _renderer.RenderFromString(template, new { IsOnline = false }));
    }

    [TestMethod]
    public void RenderFromString_ForeachWithMultipleElements_RendersAllItems()
    {
        string template = "<ul>$foreach(var item in Items)<li>${item}</li>$endfor</ul>";
        var model = new { Items = new[] { "Один", "Два", "Три" } };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<ul><li>Один</li><li>Два</li><li>Три</li></ul>", html);
    }

    [TestMethod]
    public void RenderFromString_ForeachWithEmptyCollection_RendersEmptyBody()
    {
        string template = "<ul>$foreach(var item in Items)<li>${item}</li>$endfor</ul>";
        var model = new { Items = Array.Empty<string>() };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<ul></ul>", html);
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
    public void RenderFromString_NestedForeachLoops_RendersAllLevels()
    {
        string template =
            "$foreach(var group in Groups)" +
                "[${group.Title}: $foreach(var student in group.Students)${student} $endfor]" +
            "$endfor";

        var model = new
        {
            Groups = new[]
            {
                new { Title = "11-507", Students = new[] { "Фёдор", "Иван" } },
                new { Title = "11-506", Students = new[] { "Магомед" } }
            }
        };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("[11-507: Фёдор Иван ][11-506: Магомед ]", html);
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
                new { Name = "Дональд", IsVip = true },
                new { Name = "Джеффри", IsVip = false }
            }
        };

        string html = _renderer.RenderFromString(template, model);

        Assert.AreEqual("<ul><li>[Portal] Дональд: VIP</li><li>[Portal] Джеффри: Обычный</li></ul>", html);
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
    public void RenderFromFile_ReadsTemplateAndRendersCorrectly()
    {
        string inputPath = Path.GetTempFileName();

        try
        {
            File.WriteAllText(inputPath, "<h2>Привет, ${User}!</h2>");
            var model = new { User = "Фёдор" };

            string html = _renderer.RenderFromFile(inputPath, model);

            Assert.AreEqual("<h2>Привет, Фёдор!</h2>", html);
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
        }
    }

    [TestMethod]
    public void RenderToFile_IsIdempotent_AndWritesExpectedHtml()
    {
        string inputPath = Path.GetTempFileName();
        string outputPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.html");

        try
        {
            File.WriteAllText(inputPath, "<span>${Message}</span>");
            var model = new { Message = "Готово" };

            string first = _renderer.RenderToFile(inputPath, outputPath, model);
            string second = _renderer.RenderToFile(inputPath, outputPath, model);

            Assert.AreEqual("<span>Готово</span>", first);
            Assert.AreEqual(first, second);
            Assert.AreEqual("<span>Готово</span>", File.ReadAllText(outputPath));
        }
        finally
        {
            if (File.Exists(inputPath)) File.Delete(inputPath);
            if (File.Exists(outputPath)) File.Delete(outputPath);
        }
    }

    [TestMethod]
    public void RenderFromString_InvalidForeachSyntax_ThrowsSyntaxErrorException()
    {
        string brokenTemplate = "$foreach(invalid syntax)<li>Тест</li>$endfor";
        var model = new { };

        Assert.ThrowsExactly<SyntaxErrorException>(() => _renderer.RenderFromString(brokenTemplate, model));
    }
}