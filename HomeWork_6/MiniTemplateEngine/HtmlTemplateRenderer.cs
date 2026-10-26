using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

/// <summary>
/// реализует интерфейс шаблонизатора
/// </summary>
public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public record TemplateToken(KeyWords Type, string? Content);

    private readonly Regex TokenRegex =
        new Regex(@"(\$if\s*\(.*?\) | \$else\b | \$endif\b | \$foreach\s*\(.*?\) | \$endfor\b)");

    private readonly Regex VariableRegex = new(@"[@\$]\{([^}]+)\}", RegexOptions.Compiled);

    private static readonly Regex ForeachRegex = new(
        @"[@\$]foreach\s*\(\s*(?:var\s+)?([a-zA-Z0-9_]+)\s+in\s+([a-zA-Z0-9_.]+)\s*\)",
        RegexOptions.Compiled);

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

    private string ExtractCondition(string rawIfToken)
    {
        var open = rawIfToken.IndexOf('(');
        var close = rawIfToken.IndexOf(')');

        if (open != -1 && close > open)
        {
            return rawIfToken.Substring(open + 1, close - open - 1).Trim();
        }

        return string.Empty;
    }

    private (string itemName, string collectionPath) ParseForeach(string rawForeachToken)
    {
        var match = ForeachRegex.Match(rawForeachToken);
        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value);
        }

        throw new SyntaxErrorException("некорректный цикл");
        
    }

    private List<TemplateToken> GetTokens(string template)
    {
        var tokens = new List<TemplateToken>();
        var parts = TokenRegex.Split(template);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
                continue;
            if (part.StartsWith("$if") || part.StartsWith("@if"))
                // Добавляем токен с типом TokenType.If
                tokens.Add(new TemplateToken(KeyWords.If, part));
            // Если фрагмент равен $else или @else, добавляем токен альтернативной ветки
            else if (part is "$else" or "@else")
                // Добавляем токен с типом TokenType.Else
                tokens.Add(new TemplateToken(KeyWords.Else, part));
            // Если фрагмент равен $endif или @endif, добавляем токен конца условия
            else if (part is "$endif" or "@endif")
                // Добавляем токен с типом TokenType.EndIf
                tokens.Add(new TemplateToken(KeyWords.EndIf, part));
            // Если фрагмент начинается с $foreach или @foreach, добавляем токен начала цикла
            else if (part.StartsWith("$foreach") || part.StartsWith("@foreach"))
                // Добавляем токен с типом TokenType.Foreach
                tokens.Add(new TemplateToken(KeyWords.Foreach, part));
            // Если фрагмент равен $endfor или @endfor, добавляем токен конца цикла
            else if (part is "$endfor" or "@endfor")
                // Добавляем токен с типом TokenType.EndFor
                tokens.Add(new TemplateToken(KeyWords.EndFor, part));
            // Во всех остальных случаях фрагмент является обычным HTML-текстом
            else
                // Добавляем токен с типом TokenType.Text
                tokens.Add(new TemplateToken(KeyWords.Text, part));
        }
        
        return tokens;
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