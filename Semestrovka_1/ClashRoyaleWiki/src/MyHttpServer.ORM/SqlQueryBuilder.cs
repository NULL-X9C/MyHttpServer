using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace MyHttpServer.ORM;

public static class SqlQueryBuilder
{
    private static readonly Regex ValideQuery = new(@"^[a-zA-Z_][a-zA-Z0-9_]*\z", RegexOptions.Compiled);

    public static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) || !ValideQuery.IsMatch(identifier))
        {
            throw new ArgumentException("некорректный sql идентификатор ");
        }

        return $"\"{identifier.ToLowerInvariant()}\"";
    }

    public static SqlQuery BuildInsert<T>(string tableName, T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(tableName);
        string table = EscapeIdentifier(tableName);

        var metadata = EntityMetadata.Get(typeof(T));

        if (metadata.WritableProperties.Count == 0)
        {
            throw new InvalidOperationException(
                $"Модель {typeof(T).Name} не содержит доступных свойств");
        }


        string columns = string.Join(", ",
            metadata.WritableProperties.Select(p => EscapeIdentifier(p.Name)));

        string values = string.Join(", ", metadata.WritableProperties.Select(p => $"@{p.Name}"));

        var parameters = ExtractParameters(metadata.WritableProperties, entity);

        var query = $"insert into {table} ({columns})values ({values})";

        return new SqlQuery(query, parameters);
    }

    public static SqlQuery BuildUpdate<T>(string tableName, int id, T entity) where T : class
    {
        ArgumentNullException.ThrowIfNull(entity);
        
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "Id должен быть положительным числом.");
        }

        var (table, updateColumn) = GetSafeTableAndColumnNames(tableName, "id");

        var metaData = EntityMetadata.Get(typeof(T));
        var metaDataWritableProperties = metaData.WritableProperties;

        if (metaDataWritableProperties.Count == 0)
        {
            throw new InvalidOperationException($" сущность {typeof(T)} не имеет свойств для обновления ");
        }

        var setClause = string.Join(", ",
            metaDataWritableProperties.Select(p => $"{EscapeIdentifier(p.Name)} = @{p.Name}"));

        var parameters = ExtractParameters(metaDataWritableProperties, entity);
        parameters["@id"] = id;

        var query = $"UPDATE {table} SET {setClause} WHERE {updateColumn} = @id";

        return new SqlQuery(query, parameters);
    }

    private static (string table, string Column) GetSafeTableAndColumnNames(string tableName, string columnName)
    {
        var table = EscapeIdentifier(tableName);
        var column = EscapeIdentifier(columnName);
        
        return (table, column);
    }

    public static SqlQuery BuildDelete(string tableName, int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "Идентификатор записи Id должен быть положительным числом.");
        }

        var (table, deleteColumn) = GetSafeTableAndColumnNames(tableName, "id");

        var query = $"DELETE FROM {table} WHERE {deleteColumn} = @id";
        var parameters = new Dictionary<string, object?>() { ["@id"] = id };

        return new SqlQuery(query, parameters);
    }
    
    public static SqlQuery BuildSelectAll(string tableName)
    {
        string safeTable = EscapeIdentifier(tableName);
        return new SqlQuery($"SELECT * FROM {safeTable};", new Dictionary<string, object?>());
    }
    
    public static SqlQuery BuildSelectById(string tableName, int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id),
                "Идентификатор записи Id должен быть положительным числом.");
        }

        var (table, column) = GetSafeTableAndColumnNames(tableName, "id");

        string sql = $"SELECT * FROM {table} WHERE {column} = @Id;";
        var parameters = new Dictionary<string, object?>
        {
            ["@Id"] = id
        };

        return new SqlQuery(sql, parameters);
    }

    public static Dictionary<string, object?> ExtractParameters<T>(IReadOnlyCollection<PropertyInfo> properties,
        T entity)
    {
        var parameters = new Dictionary<string, object?>();

        foreach (var property in properties)
        {
            var value = property.GetValue(entity);

            if (value is Enum enumValue)
            {
                value = Convert.ChangeType(
                    enumValue,
                    Enum.GetUnderlyingType(enumValue.GetType()),
                    CultureInfo.InvariantCulture);
            }

            parameters[$"@{property.Name}"] = value;
        }

        return parameters;
    }
}