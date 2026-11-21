using System.Data.Common;
using System.Globalization;
using System.Reflection;

namespace MyHttpServer.ORM;

public static class EntityMapper
{
    public static async Task<T> MapFromReaderAsync<T>(
        DbDataReader reader,
        IReadOnlyDictionary<string, int> columns,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        T entity = new();

        EntityMetadata metadata = EntityMetadata.Get(typeof(T));

        foreach (PropertyInfo property in metadata.AllProperties)
        {
            if (!columns.TryGetValue(property.Name, out var ordinal)) 
                continue;

            if (await reader.IsDBNullAsync(ordinal, cancellationToken))
            {
                EnsureNullableProperty(property);
                property.SetValue(entity, null);
                continue;
            }

            object rawValue = reader.GetValue(ordinal);
            object convertedValue = ConvertValue(rawValue, property.PropertyType);
            property.SetValue(entity, convertedValue);
        }

        return entity;
    }
    
    // для тестов замокал
    public static T MapFromDictionary<T>(IReadOnlyDictionary<string, object?> rowValues)
        where T : class, new()
    {
        ArgumentNullException.ThrowIfNull(rowValues);

        T entity = new();
        EntityMetadata metadata = EntityMetadata.Get(typeof(T));
        var columns = new Dictionary<string, object?>(rowValues, StringComparer.OrdinalIgnoreCase);

        foreach (PropertyInfo property in metadata.AllProperties)
        {
            if (!columns.TryGetValue(property.Name, out object? rawValue))
            {
                continue;
            }

            if (rawValue is null or DBNull)
            {
                EnsureNullableProperty(property);
                property.SetValue(entity, null);
                continue;
            }

            object convertedValue = ConvertValue(rawValue, property.PropertyType);
            property.SetValue(entity, convertedValue);
        }

        return entity;
    }

    public static object ConvertValue(object value, Type propertyType)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(propertyType);

        Type targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (targetType.IsInstanceOfType(value))
        {
            return value;
        }

        if (targetType.IsEnum)
        {
            return value is string strValue
                ? Enum.Parse(targetType, strValue, ignoreCase: true)
                : Enum.ToObject(targetType, value);
        }

        if (targetType == typeof(Guid) && value is string guidString)
        {
            return Guid.Parse(guidString);
        }

        if (targetType == typeof(bool) && value is int intBool)
        {
            return intBool != 0;
        }

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }

    private static void EnsureNullableProperty(PropertyInfo property)
    {
        bool isNullable = !property.PropertyType.IsValueType ||
                          Nullable.GetUnderlyingType(property.PropertyType) != null;

        if (!isNullable)
        {
            throw new InvalidOperationException(
                $"Столбец '{property.Name}' в БД содержит NULL, но свойство '{property.Name}' модели не допускает null.");
        }
    }
}