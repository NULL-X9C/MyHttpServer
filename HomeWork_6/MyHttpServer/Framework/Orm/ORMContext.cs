using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Npgsql;

namespace MyHttpServer.Framework.Orm;

public class ORMContext(string connectionString)
{
    private readonly string _connectionString = connectionString;

    // 1. CREATE (INSERT) — возвращает созданный объект (включая сгенерированный БД Id)
    public T Create<T>(T entity) where T : class, new()
    {
        Type type = typeof(T);
        string tableName = GetTableName(type);
        PropertyInfo? keyProp = GetKeyProperty(type);

        // Берем все свойства, кроме первичного ключа Id (его генерирует БД через SERIAL/IDENTITY)
        var props = GetMappedProperties(type)
            .Where(p => p != keyProp)
            .ToList();

        string columns = string.Join(", ", props.Select(GetColumnName));
        string paramNames = string.Join(", ", props.Select(p => "@" + p.Name));

        string keyColumn = keyProp != null ? GetColumnName(keyProp) : "id";
        string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({paramNames}) RETURNING {keyColumn};";

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);

        foreach (var prop in props)
        {
            object? val = prop.GetValue(entity) ?? DBNull.Value;
            cmd.Parameters.AddWithValue("@" + prop.Name, val);
        }

        object? generatedId = cmd.ExecuteScalar();
        if (keyProp != null && generatedId != null && generatedId != DBNull.Value)
        {
            object convertedId = ConvertValue(generatedId, keyProp.PropertyType)!;
            keyProp.SetValue(entity, convertedId);
        }

        return entity;
    }

    // 2. READ BY ID — поиск одной записи по первичному ключу
    public T? ReadById<T>(int id) where T : class, new()
    {
        Type type = typeof(T);
        string tableName = GetTableName(type);
        PropertyInfo keyProp = GetKeyProperty(type) 
            ?? throw new InvalidOperationException($"У типа {type.Name} не найдено свойство Id.");
        string keyColumn = GetColumnName(keyProp);

        string sql = $"SELECT * FROM {tableName} WHERE {keyColumn} = @id LIMIT 1;";

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return MapRowToObject<T>(reader);
        }

        return null;
    }

    // 3. READ ALL — получение всех записей таблицы
    public List<T> ReadAll<T>() where T : class, new()
    {
        Type type = typeof(T);
        string tableName = GetTableName(type);
        string sql = $"SELECT * FROM {tableName};";

        var result = new List<T>();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            result.Add(MapRowToObject<T>(reader));
        }

        return result;
    }

    // 4. UPDATE — обновление записи по Id
    public bool Update<T>(int id, T entity) where T : class
    {
        Type type = typeof(T);
        string tableName = GetTableName(type);
        PropertyInfo keyProp = GetKeyProperty(type)
            ?? throw new InvalidOperationException($"У типа {type.Name} не найдено свойство Id.");
        string keyColumn = GetColumnName(keyProp);

        var props = GetMappedProperties(type)
            .Where(p => p != keyProp)
            .ToList();

        // Формируем строку: col1 = @Prop1, col2 = @Prop2
        string setClause = string.Join(", ", props.Select(p => $"{GetColumnName(p)} = @{p.Name}"));
        string sql = $"UPDATE {tableName} SET {setClause} WHERE {keyColumn} = @targetId;";

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);

        foreach (var prop in props)
        {
            object? val = prop.GetValue(entity) ?? DBNull.Value;
            cmd.Parameters.AddWithValue("@" + prop.Name, val);
        }
        cmd.Parameters.AddWithValue("@targetId", id);

        int affectedRows = cmd.ExecuteNonQuery();
        return affectedRows > 0;
    }

    // 5. DELETE — удаление записи по Id
    public bool Delete<T>(int id) where T : class
    {
        Type type = typeof(T);
        string tableName = GetTableName(type);
        PropertyInfo keyProp = GetKeyProperty(type)
            ?? throw new InvalidOperationException($"У типа {type.Name} не найдено свойство Id.");
        string keyColumn = GetColumnName(keyProp);

        string sql = $"DELETE FROM {tableName} WHERE {keyColumn} = @id;";

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@id", id);

        int affectedRows = cmd.ExecuteNonQuery();
        return affectedRows > 0;
    }

    // 6. ФИЛЬТРАЦИЯ ЧЕРЕЗ EXPRESSION TREES: Where<User>(u => u.Age >= 18 && u.Name == "Ivan")
    public List<T> Where<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        Type type = typeof(T);
        string tableName = GetTableName(type);

        var parameters = new Dictionary<string, object?>();
        int paramCounter = 0;
        string whereClause = ParseExpression(type, predicate.Body, parameters, ref paramCounter);

        string sql = $"SELECT * FROM {tableName} WHERE {whereClause};";
        var result = new List<T>();

        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand(sql, conn);

        foreach (var kvp in parameters)
        {
            cmd.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
        }

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(MapRowToObject<T>(reader));
        }

        return result;
    }

    public T? FirstOrDefault<T>(Expression<Func<T, bool>> predicate) where T : class, new()
    {
        return Where(predicate).FirstOrDefault();
    }

    // =========================================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ РЕФЛЕКСИИ И МАППИНГА
    // =========================================================================

    private static T MapRowToObject<T>(IDataRecord reader) where T : class, new()
    {
        var obj = new T();
        Type type = typeof(T);
        var props = GetMappedProperties(type);

        // Собираем имена колонок из ответа БД (без учета регистра)
        var availableColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < reader.FieldCount; i++)
        {
            availableColumns.Add(reader.GetName(i));
        }

        foreach (var prop in props)
        {
            string colName = GetColumnName(prop);
            if (!availableColumns.Contains(colName)) continue;

            object val = reader[colName];
            if (val == DBNull.Value) continue;

            object? converted = ConvertValue(val, prop.PropertyType);
            prop.SetValue(obj, converted);
        }

        return obj;
    }

    private static object? ConvertValue(object value, Type targetType)
    {
        // Если свойство Nullable (например, int? или DateTime?), достаем базовый тип
        Type underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlyingType.IsEnum)
            return Enum.ToObject(underlyingType, value);

        if (underlyingType == typeof(Guid) && value is string strGuid)
            return Guid.Parse(strGuid);

        return Convert.ChangeType(value, underlyingType);
    }

    private static string GetTableName(Type type)
    {
        var attr = type.GetCustomAttribute<TableAttribute>();
        return attr != null ? attr.Name : type.Name.ToLowerInvariant() + "s"; // User -> users
    }

    private static string GetColumnName(PropertyInfo prop)
    {
        var attr = prop.GetCustomAttribute<ColumnAttribute>();
        return attr != null ? attr.Name : prop.Name.ToLowerInvariant();
    }

    private static PropertyInfo? GetKeyProperty(Type type)
    {
        var props = GetMappedProperties(type);
        return props.FirstOrDefault(p => p.GetCustomAttribute<KeyAttribute>() != null)
            ?? props.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
            ?? props.FirstOrDefault(p => p.Name.Equals(type.Name + "Id", StringComparison.OrdinalIgnoreCase));
    }

    private static List<PropertyInfo> GetMappedProperties(Type type)
    {
        return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetCustomAttribute<IgnoreAttribute>() == null)
            .ToList();
    }

    // =========================================================================
    // ПАРСЕР ДЕРЕВЬЕВ ВЫРАЖЕНИЙ (ДЛЯ МЕТОДА Where)
    // =========================================================================

    private static string ParseExpression(
        Type entityType,
        Expression expr,
        Dictionary<string, object?> parameters,
        ref int paramIndex)
    {
        // 1. Бинарные операции: ==, !=, >, <, >=, <=, &&, ||
        if (expr is BinaryExpression binary)
        {
            if (binary.NodeType is ExpressionType.AndAlso or ExpressionType.OrElse)
            {
                string left = ParseExpression(entityType, binary.Left, parameters, ref paramIndex);
                string right = ParseExpression(entityType, binary.Right, parameters, ref paramIndex);
                string op = binary.NodeType == ExpressionType.AndAlso ? "AND" : "OR";
                return $"({left} {op} {right})";
            }

            string colName = ExtractColumnName(entityType, binary.Left);
            object? val = ExtractConstantValue(binary.Right);

            if (val == null)
            {
                return binary.NodeType == ExpressionType.Equal
                    ? $"{colName} IS NULL"
                    : $"{colName} IS NOT NULL";
            }

            string paramName = $"@p{paramIndex++}";
            parameters[paramName] = val;

            string sqlOp = binary.NodeType switch
            {
                ExpressionType.Equal => "=",
                ExpressionType.NotEqual => "<>",
                ExpressionType.GreaterThan => ">",
                ExpressionType.GreaterThanOrEqual => ">=",
                ExpressionType.LessThan => "<",
                ExpressionType.LessThanOrEqual => "<=",
                _ => throw new NotSupportedException($"Оператор {binary.NodeType} не поддерживается.")
            };

            return $"{colName} {sqlOp} {paramName}";
        }

        // 2. Булево свойство напрямую: u => u.IsActive
        if (expr is MemberExpression member && member.Type == typeof(bool))
        {
            string colName = ExtractColumnName(entityType, member);
            return $"{colName} = TRUE";
        }

        // 3. Отрицание: u => !u.IsBanned
        if (expr is UnaryExpression { NodeType: ExpressionType.Not } unary)
        {
            string inner = ParseExpression(entityType, unary.Operand, parameters, ref paramIndex);
            return $"NOT ({inner})";
        }

        throw new NotSupportedException($"Выражение {expr} не поддерживается парсером ORM.");
    }

    private static string ExtractColumnName(Type entityType, Expression expr)
    {
        if (expr is UnaryExpression unary && unary.NodeType == ExpressionType.Convert)
            expr = unary.Operand;

        if (expr is MemberExpression member && member.Member is PropertyInfo prop)
            return GetColumnName(prop);

        throw new ArgumentException($"Не удалось извлечь имя колонки из выражения: {expr}");
    }

    private static object? ExtractConstantValue(Expression expr)
    {
        if (expr is ConstantExpression constant)
            return constant.Value;

        // Если в лямбду передана локальная переменная из метода контроллера (замыкание)
        var objectMember = Expression.Convert(expr, typeof(object));
        var getterLambda = Expression.Lambda<Func<object>>(objectMember);
        return getterLambda.Compile().Invoke();
    }
}