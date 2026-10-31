namespace MyHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public class PutAttribute(string route = "") : Attribute
{
    public string Route { get; } = route;
}