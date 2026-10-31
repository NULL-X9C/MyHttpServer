using System.Collections;
using System.Data;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace MiniTemplateEngine;

/// <summary>
/// реализует интерфейс шаблонизатора
/// </summary>
public class HtmlTemplateRenderer : IHtmlTemplateRenderer
{
    public record TemplateToken(TokenType Type, string Content);

    // Компиляция 1 раз 
    private static readonly Regex TokenRegex = new(
        @"(\$if\s*\(.*?\)|\$else|\$endif|\$foreach\s*\(.*?\)|\$endfor)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex VariableRegex = new(@"[$]\{([^}]+)\}", RegexOptions.Compiled);

    private static readonly Regex ForeachRegex = new(
        @"[$]foreach\s*\(\s*(?:var\s+)?([a-zA-Z0-9_]+)\s+in\s+([a-zA-Z0-9_.]+)\s*\)",
        RegexOptions.Compiled);

    public string RenderFromString(string htmlTemplate, object dataModel)
    {
        // chek if null
        ArgumentNullException.ThrowIfNull(htmlTemplate);
        ArgumentNullException.ThrowIfNull(dataModel);

        List<TemplateToken> tokens = GetTokens(htmlTemplate);
        int index = 0;
        var localVars = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        return RenderBlock(tokens, ref index, dataModel, localVars);
    }

    public string RenderFromFile(string filePath, object dataModel)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(dataModel);

        string content;
        using (var stream = new StreamReader(filePath))
        {
            content = stream.ReadToEnd();
        }

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
            if (part.StartsWith("$if"))
                tokens.Add(new TemplateToken(TokenType.If, part));
            else if (part is "$else")
                tokens.Add(new TemplateToken(TokenType.Else, part));
            else if (part is "$endif")
                tokens.Add(new TemplateToken(TokenType.EndIf, part));
            else if (part.StartsWith("$foreach"))
                tokens.Add(new TemplateToken(TokenType.Foreach, part));
            else if (part is "$endfor")
                tokens.Add(new TemplateToken(TokenType.EndFor, part));
            else
                tokens.Add(new TemplateToken(TokenType.Text, part));
        }

        return tokens;
    }

    private object? ResolveValue(string path, object? dataModel, Dictionary<string, object?> localVars)
    {
        var parts = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0) return null;

        object? current;
        int startIndex = 0;

        if (localVars.TryGetValue(parts[0], out var localObj))
        {
            current = localObj;
            startIndex = 1;
        }
        else
        {
            current = dataModel;
        }

        for (var i = startIndex; i < parts.Length; i++)
        {
            if (current is null) return null;

            Type type = current.GetType();
            PropertyInfo? prop = type.GetProperty(parts[i],
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is not null)
            {
                current = prop.GetValue(current);
                continue;
            }

            FieldInfo? field = type.GetField(parts[i],
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (field is not null)
            {
                current = field.GetValue(current);
                continue;
            }

            return null;
        }

        return current;
    }

    // На кондициях
    private bool EvaluateCondition(string condition, object? dataModel, Dictionary<string, object?> localVars)
    {
        bool invert = false;
        if (condition.StartsWith('!'))
        {
            invert = true;
            condition = condition[1..].Trim();
        }

        var value = ResolveValue(condition, dataModel, localVars);

        var rez = value switch
        {
            null => false,
            bool boolean => boolean,
            string s => !String.IsNullOrWhiteSpace(s),
            int n => n > 0,
            double n => n > 0,
            decimal n => n > 0,
            long n => n > 0,
            float n => n > 0,
            short n => n > 0,
            _ => true
        };

        return invert ? !rez : rez;
    }

    private string EvaluateText(string text, object? globalModel, Dictionary<string, object?> localVars)
    {
        return VariableRegex.Replace(text, match =>
        {
            string path = match.Groups[1].Value.Trim();
            object? val = ResolveValue(path, globalModel, localVars);
            return val?.ToString() ?? string.Empty;
        });
    }

    private void SkipBlock(List<TemplateToken> tokens, ref int index, bool stop)
    {
        int depth = 0;

        while (index < tokens.Count)
        {
            var blockType = tokens[index].Type;

            if (blockType is TokenType.If or TokenType.Foreach)
                depth++;

            else if (blockType is TokenType.EndIf or TokenType.EndFor)
            {
                if (depth == 0) return;

                depth--;
            }
            else if (blockType == TokenType.Else && depth == 0 && stop)
            {
                return;
            }

            index++;
        }
    }

    private string RenderBlock(List<TemplateToken> tokens, ref int index, object? dataModel,
        Dictionary<string, object?> localVars)
    {
        var result = new StringBuilder();

        while (index < tokens.Count)
        {
            TemplateToken token = tokens[index];
            if (token.Type is TokenType.EndIf or TokenType.Else or TokenType.EndFor)
            {
                return result.ToString();
            }

            if (token.Type == TokenType.Text)
            {
                result.Append(EvaluateText(token.Content, dataModel, localVars));
                index++;
            }
            else if (token.Type == TokenType.If)
            {
                string conditionPath = ExtractCondition(token.Content);
                bool isTrue = EvaluateCondition(conditionPath, dataModel, localVars);
                index++;

                if (isTrue)
                {
                    result.Append(RenderBlock(tokens, ref index, dataModel, localVars));

                    if (index < tokens.Count && tokens[index].Type == TokenType.Else)
                    {
                        index++;
                        SkipBlock(tokens, ref index, stop: false);
                    }
                }
                else
                {
                    SkipBlock(tokens, ref index, stop: true);

                    if (index < tokens.Count && tokens[index].Type == TokenType.Else)
                    {
                        index++;
                        result.Append(RenderBlock(tokens, ref index, dataModel, localVars));
                    }
                }

                if (index < tokens.Count && tokens[index].Type == TokenType.EndIf)
                {
                    index++;
                }
            }
            else if (token.Type == TokenType.Foreach)
            {
                var (itemName, collectionPath) = ParseForeach(token.Content);
                index++;

                int bodyStartIndex = index;
                object? collectionObj = ResolveValue(collectionPath, dataModel, localVars);

                if (collectionObj is IEnumerable collection and not string)
                {
                    foreach (var item in collection)
                    {
                        var loopVars = new Dictionary<string, object?>(localVars, StringComparer.OrdinalIgnoreCase)
                        {
                            [itemName] = item
                        };

                        int loopIndex = bodyStartIndex;
                        result.Append(RenderBlock(tokens, ref loopIndex, dataModel, loopVars));
                    }
                }

                index = bodyStartIndex;
                SkipBlock(tokens, ref index, stop: false);

                if (index < tokens.Count && tokens[index].Type == TokenType.EndFor)
                {
                    index++;
                }
            }
        }

        return result.ToString();
    }
}