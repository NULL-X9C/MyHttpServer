using System.Collections.Concurrent;
using System.Reflection;

namespace MyHttpServer.ORM;

public class EntityMetadata
{
    private static readonly ConcurrentDictionary<Type, EntityMetadata> Cache = new();

    // white list
    private static readonly HashSet<Type> SupportedScalarTypes =
    [
        typeof(byte), typeof(short), typeof(int),
        typeof(long), typeof(float), typeof(double),
        typeof(decimal), typeof(bool), typeof(string), typeof(char),
        typeof(DateTime), typeof(DateTimeOffset), typeof(Guid)
    ];

    public Type EntityType { get; }

    public IReadOnlyList<PropertyInfo> AllProperties { get; }

    public IReadOnlyList<PropertyInfo> WritableProperties { get; }

    private EntityMetadata(Type type)
    {
        EntityType = type;

        AllProperties = type
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p is
                        {
                            CanRead: true, CanWrite: true,
                            GetMethod.IsPublic: true, SetMethod.IsPublic: true
                        } &&
                        p.GetIndexParameters().Length == 0 &&
                        IsSupportedScalarType(p.PropertyType))
            .ToArray();

        // удаляем первичный ключ Id 
        WritableProperties = [.. AllProperties.Where(p =>
            !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))];
    }

    /// <summary>
    /// Возвращает метаданные или создает их 
    /// </summary>
    public static EntityMetadata Get(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return Cache.GetOrAdd(type, static t => new EntityMetadata(t));
    }

    public static bool IsSupportedScalarType(Type type)
    {
        // игнор Nullable 
        Type underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsEnum || SupportedScalarTypes.Contains(underlying);
    }
}