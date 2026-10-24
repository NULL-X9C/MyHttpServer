namespace MyHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PostAttribute(string route) : Attribute
{
    public string Route { get; } = route;
}