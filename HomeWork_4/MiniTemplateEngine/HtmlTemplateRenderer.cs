using System.Reflection;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

/// <summary>
/// реализует интерфейс шаблонизатора
/// </summary>
public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        // chek if null
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        ArgumentNullException.ThrowIfNull(dataModel);

        return htmlTemplate;
    }

// todo: узнать насчет гонки данных при текущем варианте using 
    public string RenderFromFile(string filePath, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(dataModel);

        using var stream = new StreamReader(filePath);

        var content = stream.ReadToEnd();
        return RenderFromString(content, dataModel);
    }

    public string RenderToFile(string inputFilePath, string outputFilePath, object dataModel)
    {
        if (inputFilePath == null) throw new ArgumentNullException(nameof(inputFilePath));
        if (outputFilePath == null) throw new ArgumentNullException(nameof(outputFilePath));
        if (dataModel == null) throw new ArgumentNullException(nameof(dataModel));

        var content = RenderFromFile(inputFilePath, dataModel);
        using StreamWriter streamWriter = new(outputFilePath);
        streamWriter.Write(content);
        return content;
    }

    private object? GetValueByPath(string path, object? dataModel, Dictionary<string, object?> localVars)
    {
        if (path == null) throw new ArgumentNullException(nameof(path));
        if (localVars == null) throw new ArgumentNullException(nameof(localVars));

        string cleanPath = path.Trim();

        var parts = cleanPath.Split(".");

        object? current;

        int startIndex = 0;

        if (localVars.TryGetValue(parts[0], out var value))
        {
            current = value;
            startIndex = 1;
        }
        else current = dataModel;

        for (int i = startIndex; i < parts.Length; i++)
        {
            if (current is null) return null;

            var currentType = current.GetType();
            var property = currentType.GetProperty(parts[i], BindingFlags.IgnoreCase);
            if (property is null) return null;

            current = property.GetValue(current);
        }

        return current;
    }

    private string RenderVariables(string template, object dataModel, Dictionary<string, object?> localVars)
    {
        var regex = new Regex(@"\${([^}]+)\}");
        return regex.Replace(template, match =>
        {
            string propertyPath = match.Groups[1].Value;
            var value = GetValueByPath(propertyPath, dataModel, localVars);
            return value?.ToString() ?? "";
        });
    }
}