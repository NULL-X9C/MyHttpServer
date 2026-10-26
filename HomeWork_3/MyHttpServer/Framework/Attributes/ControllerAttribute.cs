namespace MyHttpServer.Framework.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ControllerAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}