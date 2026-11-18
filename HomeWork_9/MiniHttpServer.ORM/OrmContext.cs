using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using Npgsql;

namespace MiniHttpServer.ORM;

public sealed class OrmContext : IOrmContext
{
    private readonly string _connectionString;
 
    /// <summary>
    /// Создаёт экземпляр контекста для работы
    /// с базой данных SQL Server.
    /// </summary>
    /// <param name="connectionString">
    /// Строка подключения к базе данных.
    /// </param>
    /// <exception cref="ArgumentException">
    /// Строка подключения не задана.
    /// </exception>
    public OrmContext(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            connectionString);
 
        _connectionString = connectionString;
    }
 
    /// <inheritdoc />
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(
        string tableName,
        CancellationToken cancellationToken = default)
        where T : class, new()
    {
        string safeTableName = EscapeIdentifier(tableName);
 
        string sql = $"SELECT * FROM {safeTableName}";
 
        var result = new List<T>();
 
        await using var connection =
            new NpgsqlConnection(_connectionString);
 
        await connection.OpenAsync(cancellationToken);
 
        await using var command =
            new NpgsqlCommand(sql, connection);
 
        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);
 
        PropertyInfo[] properties = typeof(T)
            .GetProperties(BindingFlags.Public |
                           BindingFlags.Instance)
            .Where(property => property.CanWrite &&
                               property.SetMethod?.IsPublic == true)
            .ToArray();
 
        var columns = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);
 
        for (int index = 0; index < reader.FieldCount; index++)
        {
            columns[reader.GetName(index)] = index;
        }
 
        while (await reader.ReadAsync(cancellationToken))
        {
            T entity = new();
 
            foreach (PropertyInfo property in properties)
            {
                if (!columns.TryGetValue(
                        property.Name, out int ordinal))
                {
                    continue;
                }
 
                if (await reader.IsDBNullAsync(
                        ordinal, cancellationToken))
                {
                    if (!property.PropertyType.IsValueType ||
                        Nullable.GetUnderlyingType(
                            property.PropertyType) != null)
                    {
                        property.SetValue(entity, null);
                    }
 
                    continue;
                }
 
                object value = reader.GetValue(ordinal);
 
                Type targetType =
                    Nullable.GetUnderlyingType(
                        property.PropertyType)
                    ?? property.PropertyType;
 
                object convertedValue =
                    targetType.IsInstanceOfType(value)
                        ? value
                        : targetType.IsEnum
                            ? Enum.ToObject(targetType, value)
                            : Convert.ChangeType(
                                value,
                                targetType,
                                CultureInfo.InvariantCulture);
 
                property.SetValue(entity, convertedValue);
            }
 
            result.Add(entity);
        }
 
        return result;
    }
 
    /// <summary>
    /// Проверяет имя SQL-идентификатора и экранирует его.
    /// </summary>
    /// <param name="identifier">
    /// Имя таблицы или столбца.
    /// </param>
    /// <returns>
    /// Проверенный SQL-идентификатор в квадратных скобках.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Имя идентификатора имеет недопустимый формат.
    /// </exception>
    private static string EscapeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier) ||
            !Regex.IsMatch(
                identifier,
                @"^[A-Za-z_][A-Za-z0-9_]*$"))
        {
            throw new ArgumentException(
                "Недопустимое имя SQL-идентификатора.",
                nameof(identifier));
        }
 
        return $"[{identifier}]";
    }
 
    /// <inheritdoc />
    public Task<int> InsertAsync<T>(
        string tableName,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class
    {
        throw new NotImplementedException();
    }
 
    /// <inheritdoc />
    public Task<int> UpdateAsync<T>(
        string tableName,
        int id,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class
    {
        throw new NotImplementedException();
    }
 
    /// <inheritdoc />
    public Task<int> DeleteAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}