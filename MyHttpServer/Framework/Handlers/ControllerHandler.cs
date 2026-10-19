using System.Collections.Specialized;
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

    public ControllersHandler()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var types = assembly.GetTypes();

        foreach (var type in types)
        {
            var controllerAttr = type.GetCustomAttribute<ControllerAttribute>();
            if (controllerAttr != null)
            {
                var methods = type.GetMethods();
                foreach (var method in methods)
                {
                    var getAttr = method.GetCustomAttribute<GetAttribute>();
                    if (getAttr != null)
                    {
                        string path = getAttr.Route.StartsWith("/")
                            ? getAttr.Route
                            : $"/{controllerAttr.Name}/{getAttr.Route}";
                        _routes[$"GET:{path}"] = new EndpointDefinition
                            { EndpointType = type, EndpointAction = method };
                    }

                    var postAttr = method.GetCustomAttribute<PostAttribute>();
                    if (postAttr != null)
                    {
                        string path = postAttr.Route.StartsWith("/")
                            ? postAttr.Route
                            : $"/{controllerAttr.Name}/{postAttr.Route}";
                        _routes[$"POST:{path}"] = new EndpointDefinition
                            { EndpointType = type, EndpointAction = method };
                    }
                }
            }
        }

        Console.WriteLine($"[System] Маршрутизатор загружен. Найдено эндпоинтов: {_routes.Count}");
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
            var controllerInstance = Activator.CreateInstance(endpoint.EndpointType);
            if (controllerInstance is ControllerBase baseController)
            {
                baseController.SetContext(context);
            }

            var invokeArgs = await BindParametersAsync(request, endpoint.EndpointAction);

            var result = endpoint.EndpointAction.Invoke(controllerInstance, invokeArgs);

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

    private async Task<object?[]> BindParametersAsync(HttpListenerRequest request, MethodInfo action)
    {
        var queryParams = HttpUtility.ParseQueryString(request.Url!.Query);
        string requestBody = string.Empty;

        if (request.HttpMethod == "POST" && request.HasEntityBody)
        {
            using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
            requestBody = await reader.ReadToEndAsync();

            if (request.ContentType != null && request.ContentType.Contains("application/x-www-form-urlencoded"))
            {
                var bodyParams = HttpUtility.ParseQueryString(requestBody);
                queryParams.Add(bodyParams);
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
                invokeArgs[i] = ConvertSingleValue(rawValue, paramType, param.HasDefaultValue ? param.DefaultValue : null);
            }
            else
            {
                if (request.ContentType != null && request.ContentType.Contains("application/json") && !string.IsNullOrWhiteSpace(requestBody))
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
        return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal);
    }

    private static object? ConvertSingleValue(string? rawValue, Type targetType, object? defaultValue)
    {
        if (targetType == typeof(bool))
        {
            return rawValue == "on" || string.Equals(rawValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        if (rawValue == null)
        {
            return targetType == typeof(string) ? string.Empty : defaultValue;
        }

        return Convert.ChangeType(rawValue, targetType);
    }
}