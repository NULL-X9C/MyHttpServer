namespace MyHttpServer.Framework.Orm;

[AttributeUsage(AttributeTargets.Class)]
public class TableAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Property)]
public class ColumnAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

[AttributeUsage(AttributeTargets.Property)]
public class KeyAttribute : Attribute;

[AttributeUsage(AttributeTargets.Property)]
public class IgnoreAttribute : Attribute;