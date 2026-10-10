using System.Reflection;

namespace MyHttpServer.Framework.Routing;

public class EndpointDefinition
{
    public Type? EndpointType { get; set; }  
    public  MethodInfo? EndpointAction { get; set; }
}