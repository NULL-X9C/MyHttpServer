using System.Net;
using System.Reflection;
using System.Web;
using MyHttpServer.Framework.Attributes;
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
                        string path = $"/{controllerAttr.Name}/{getAttr.Route}";
                        _routes[$"GET:{path}"] = new EndpointDefinition
                            { EndpointType = type, EndpointAction = method };
                    }

                    var postAttr = method.GetCustomAttribute<PostAttribute>();
                    if (postAttr != null)
                    {
                        string path = $"/{controllerAttr.Name}/{postAttr.Route}";
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
        string routeKey = $"{method}:{path}";

        if (_routes.TryGetValue(routeKey, out var endpoint))
        {
            try
            {
                var controllerInstance = Activator.CreateInstance(endpoint.EndpointType);

                var queryParams = HttpUtility.ParseQueryString(request.Url.Query);

                if (method == "POST" && request.HasEntityBody)
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string requestBody = await reader.ReadToEndAsync();
                    var bodyParams = HttpUtility.ParseQueryString(requestBody);
                    queryParams.Add(bodyParams);
                }

                var methodParameters = endpoint.EndpointAction.GetParameters();
                var invokeArgs = new object[methodParameters.Length];

                for (int i = 0; i < methodParameters.Length; i++)
                {
                    var param = methodParameters[i];
                    string paramName = param.Name!;
                    string? stringValue = queryParams[paramName]; 

                    if (param.ParameterType == typeof(bool))
                    {
                        invokeArgs[i] = (stringValue == "on" || stringValue == "true");
                    }
                    else
                    {
                        invokeArgs[i] = stringValue ?? string.Empty; 
                    }
                }

                var result = endpoint.EndpointAction.Invoke(controllerInstance, invokeArgs);

                if (result is string htmlContent)
                {
                    await SendTextAsync(htmlContent, response);
                }
                else
                {
                    response.StatusCode = 200; 
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка контроллера: {ex.Message}");
                response.StatusCode = 500;
            }
            finally
            {
                response.Close();
            }

            return; 
        }

        await base.HandleAsync(context);
    }
}