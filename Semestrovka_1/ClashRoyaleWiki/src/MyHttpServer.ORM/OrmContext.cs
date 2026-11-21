using System.Data.Common;
using Npgsql;

namespace MyHttpServer.ORM;

public sealed class OrmContext : IOrmContext
{
    private readonly string _connectionString;

    public OrmContext(string connectionString)
    {
        ArgumentNullException.ThrowIfNull(connectionString);
        _connectionString = connectionString;
    }

    public async Task<int> InsertAsync<T>(string tableName, T entity, CancellationToken cancellationToken = default) where T : class
    {
        var query = SqlQueryBuilder.BuildInsert(tableName, entity);

        return await ExecuteNonQueryInternalAsync(query, cancellationToken);
    }

    public async Task<int> UpdateAsync<T>(string tableName, int id, T entity, CancellationToken cancellationToken = default) where T : class
    {
        var sqlQuery = SqlQueryBuilder.BuildUpdate(tableName, id, entity);

        return await ExecuteNonQueryInternalAsync(sqlQuery, cancellationToken);
    }

    public async Task<int> DeleteAsync(string tableName, int id, CancellationToken cancellationToken = default)
    {
        var query = SqlQueryBuilder.BuildDelete(tableName, id);

        return await ExecuteNonQueryInternalAsync(query, cancellationToken);
    }
    
    public async Task<IReadOnlyList<T>> GetAllAsync<T>(
    string tableName,
    CancellationToken cancellationToken = default)
    where T : class, new()
{
    SqlQuery query = SqlQueryBuilder.BuildSelectAll(tableName);
    var result = new List<T>();

    await using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);

    await using var command = CreateCommand(connection, query);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    Dictionary<string, int> columns = GetColumnOrdinals(reader);

    while (await reader.ReadAsync(cancellationToken))
    {
        T entity = await EntityMapper.MapFromReaderAsync<T>(reader, columns, cancellationToken);
        result.Add(entity);
    }

    return result;
}


public async Task<T?> GetByIdAsync<T>(
    string tableName,
    int id,
    CancellationToken cancellationToken = default)
    where T : class, new()
{
    SqlQuery query = SqlQueryBuilder.BuildSelectById(tableName, id);

    await using var connection = new NpgsqlConnection(_connectionString);
    await connection.OpenAsync(cancellationToken);

    await using var command = CreateCommand(connection, query);
    await using var reader = await command.ExecuteReaderAsync(cancellationToken);

    if (await reader.ReadAsync(cancellationToken))
    {
        Dictionary<string, int> columns = GetColumnOrdinals(reader);
        return await EntityMapper.MapFromReaderAsync<T>(reader, columns, cancellationToken);
    }

    return null;
}

private static Dictionary<string, int> GetColumnOrdinals(DbDataReader reader)
{
    var columns = new Dictionary<string, int>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
    for (int index = 0; index < reader.FieldCount; index++)
    {
        columns[reader.GetName(index)] = index;
    }
    return columns;
}
    
    private async Task<int> ExecuteNonQueryInternalAsync(
        SqlQuery query,
        CancellationToken cancellationToken)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateCommand(connection, query);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    } 
    
    private static NpgsqlCommand CreateCommand(NpgsqlConnection connection, SqlQuery query)
    {
        var command = new NpgsqlCommand(query.Command, connection);

        foreach (var queryParameter in query.Parameters)
        {
            command.Parameters.AddWithValue(queryParameter.Key, queryParameter.Value ?? DBNull.Value);
        }

        return command;
    }
}