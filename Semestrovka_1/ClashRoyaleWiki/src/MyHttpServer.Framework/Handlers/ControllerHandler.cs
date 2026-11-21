using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Web;
using MyHttpServer.Framework.Attributes;
using MyHttpServer.Framework.Results;
using MyHttpServer.Framework.Routing;

namespace MyHttpServer.Framework.Handlers;

public class ControllersHandler : BaseHandler
{
    private readonly Dictionary<string, EndpointDefinition> _routes = new(StringComparer.OrdinalIgnoreCase);
    private readonly IReadOnlyDictionary<Type, object> _services;

    public ControllersHandler(
        Assembly? controllersAssembly = null,
        IReadOnlyDictionary<Type, object>? services = null)
    {
        _services = services ?? new Dictionary<Type, object>();
        var assembly = controllersAssembly
                       ?? Assembly.GetEntryAssembly()
                       ?? Assembly.GetExecutingAssembly();

        foreach (var type in assembly.GetTypes())
        {
            var controllerAttr = type.GetCustomAttribute<ControllerAttribute>();
            if (controllerAttr == null) continue;

            foreach (var method in type.GetMethods())
            {
                RegisterRoute<GetAttribute>(type, method, controllerAttr.Name, "GET", a => a.Route);
                RegisterRoute<PostAttribute>(type, method, controllerAttr.Name, "POST", a => a.Route);
                RegisterRoute<PutAttribute>(type, method, controllerAttr.Name, "PUT", a => a.Route);
            }
        }

        Console.WriteLine($"[System] Маршрутизатор загружен. Найдено эндпоинтов: {_routes.Count}");
    }

    private void RegisterRoute<TAttr>(
        Type controllerType,
        MethodInfo method,
        string controllerName,
        string httpMethod,
        Func<TAttr, string> routeSelector) where TAttr : Attribute
    {
        var attr = method.GetCustomAttribute<TAttr>();
        if (attr == null) return;

        string route = routeSelector(attr);
        string path = route.StartsWith("/")
            ? route.TrimEnd('/')
            : $"/{controllerName}/{route}".TrimEnd('/');

        if (string.IsNullOrEmpty(path)) path = "/";

        _routes[$"{httpMethod}:{path}"] = new EndpointDefinition
        {
            EndpointType = controllerType,
            EndpointAction = method
        };
    }

    public override async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        string method = request.HttpMethod;
        string path = request.Url!.LocalPath.TrimEnd('/');
        if (string.IsNullOrEmpty(path)) path = "/";
        string routeKey = $"{method}:{path}";

        if (_routes.TryGetValue(routeKey, out var endpoint))
        {
            var controllerInstance = CreateControllerInstance(endpoint.EndpointType!);
            if (controllerInstance is ControllerBase baseController)
            {
                baseController.SetContext(context);
            }

            var invokeArgs = await BindParametersAsync(request, endpoint.EndpointAction!);
            var result = endpoint.EndpointAction!.Invoke(controllerInstance, invokeArgs);

            if (result is Task task)
            {
                await task;
                var resultProperty = task.GetType().GetProperty("Result");
                result = resultProperty?.GetValue(task);
            }

            if (response.StatusCode is 204 or 302 or 303)
            {
                response.Close();
                return;
            }

            switch (result)
            {
                case IActionResult actionResult:
                    byte[] buffer = actionResult.ExecuteResult();
                    await SendBytesAsync(buffer, actionResult.ContentType, response);
                    break;

                case string htmlContent:
                    await SendTextAsync(htmlContent, response);
                    break;

                default:
                    response.StatusCode = 200;
                    break;
            }

            response.Close();
            return;
        }

        await base.HandleAsync(context);
    }

    private object CreateControllerInstance(Type controllerType)
    {
        var ctor = controllerType
            .GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"У контроллера '{controllerType.Name}' нет публичного конструктора.");

        var parameters = ctor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            Type paramType = parameters[i].ParameterType;
            if (!_services.TryGetValue(paramType, out var service))
            {
                throw new InvalidOperationException(
                    $"Зависимость '{paramType.Name}' для контроллера '{controllerType.Name}' не зарегистрирована в словаре сервисов.");
            }
            args[i] = service;
        }

        return ctor.Invoke(args);
    }
    private async Task<object?[]> BindParametersAsync(HttpListenerRequest request, MethodInfo action)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url!.Query);
        string requestBody = string.Empty;

        if ((request.HttpMethod == "POST" || request.HttpMethod == "PUT") && request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            requestBody = await reader.ReadToEndAsync();
            string trimmedBody = requestBody.Trim();

            if (!string.IsNullOrEmpty(trimmedBody))
            {
                if ((request.ContentType != null &&
                     request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                    || trimmedBody.StartsWith('{'))
                {
                    try
                    {
                        using var jsonDoc = JsonDocument.Parse(trimmedBody);
                        if (jsonDoc.RootElement.ValueKind == JsonValueKind.Object)
                        {
                            foreach (var prop in jsonDoc.RootElement.EnumerateObject())
                            {
                                // приоритет у json
                                queryParams[prop.Name] = prop.Value.ValueKind == JsonValueKind.Null
                                    ? null
                                    : prop.Value.ToString();
                            }
                        }
                    }
                    catch
                    {
                        // если корявый json 
                    }
                }
                else
                {
                    // x-www-form-urlencoded
                    var bodyParams = HttpUtility.ParseQueryString(requestBody);
                    foreach (string? key in bodyParams.AllKeys)
                    {
                        if (key != null)
                        {
                            queryParams[key] = bodyParams[key];
                        }
                    }
                }
            }
        }

        var methodParameters = action.GetParameters();
        var invokeArgs = new object?[methodParameters.Length];

        for (int i = 0; i < methodParameters.Length; i++)
        {
            var param = methodParameters[i];
            Type paramType = param.ParameterType;

            if (IsSimpleType(paramType))
            {
                string? rawValue = queryParams[param.Name!];
                invokeArgs[i] =
                    ConvertSingleValue(rawValue, paramType, param.HasDefaultValue ? param.DefaultValue : null);
            }
            else
            {
                if (request.ContentType != null &&
                    request.ContentType.Contains("application/json", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(requestBody))
                {
                    invokeArgs[i] = JsonSerializer.Deserialize(requestBody, paramType, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
                else
                {
                    var dtoInstance = Activator.CreateInstance(paramType);
                    foreach (var prop in paramType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                    {
                        if (!prop.CanWrite) continue;

                        string? rawVal = queryParams[prop.Name];
                        if (rawVal != null || prop.PropertyType == typeof(bool))
                        {
                            object? convertedVal = ConvertSingleValue(rawVal, prop.PropertyType, null);
                            prop.SetValue(dtoInstance, convertedVal);
                        }
                    }

                    invokeArgs[i] = dtoInstance;
                }
            }
        }

        return invokeArgs;
    }

    private static bool IsSimpleType(Type type)
    {
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string) || underlying == typeof(decimal);
    }
    private static object? ConvertSingleValue(string? rawValue, Type targetType, object? defaultValue)
    {
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType == typeof(bool))
        {
            return rawValue == "on" || string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        if (string.IsNullOrWhiteSpace(rawValue))
        {
            if (targetType == typeof(string)) return string.Empty;
            if (Nullable.GetUnderlyingType(targetType) != null) return null;
            return defaultValue ?? (targetType.IsValueType ? Activator.CreateInstance(targetType) : null);
        }

        try
        {
            if (underlyingType.IsEnum)
            {
                return Enum.Parse(underlyingType, rawValue, ignoreCase: true);
            }
            return Convert.ChangeType(rawValue, underlyingType, System.Globalization.CultureInfo.InvariantCulture);
        }
        catch
        {
            return Nullable.GetUnderlyingType(targetType) != null
                ? null
                : (targetType.IsValueType ? Activator.CreateInstance(targetType) : null);
        }
    }
}