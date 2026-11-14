/// <summary>
/// Определяет контракт для выполнения базовых
/// CRUD-операций с реляционной базой данных.
/// </summary>
/// <remarks>
/// Поддерживает получение, добавление, изменение
/// и удаление записей с преобразованием между
/// строками таблиц и объектами .NET.
/// </remarks>
public interface IOrmContext
{
    /// <summary>
    /// Асинхронно получает все записи указанной таблицы.
    /// </summary>
    /// <typeparam name="T">
    /// Тип модели, в которую преобразуются записи.
    /// </typeparam>
    /// <param name="tableName">
    /// Имя таблицы базы данных.
    /// </param>
    /// <param name="cancellationToken">
    /// Токен отмены операции.
    /// </param>
    /// <returns>
    /// Коллекция объектов типа <typeparamref name="T"/>.
    /// Если таблица пуста, возвращается пустая коллекция.
    /// </returns>
    Task<IReadOnlyList<T>> GetAllAsync<T>(
        string tableName,
        CancellationToken cancellationToken = default)
        where T : class, new();
 
    /// <summary>
    /// Асинхронно добавляет запись в указанную таблицу.
    /// </summary>
    /// <typeparam name="T">
    /// Тип добавляемой модели.
    /// </typeparam>
    /// <param name="tableName">
    /// Имя таблицы базы данных.
    /// </param>
    /// <param name="entity">
    /// Объект, содержащий значения новой записи.
    /// </param>
    /// <param name="cancellationToken">
    /// Токен отмены операции.
    /// </param>
    /// <returns>
    /// Количество добавленных записей.
    /// </returns>
    Task<int> InsertAsync<T>(
        string tableName,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class;
 
    /// <summary>
    /// Асинхронно изменяет существующую запись.
    /// </summary>
    /// <typeparam name="T">
    /// Тип модели с обновлёнными значениями.
    /// </typeparam>
    /// <param name="tableName">
    /// Имя таблицы базы данных.
    /// </param>
    /// <param name="id">
    /// Значение первичного ключа Id.
    /// </param>
    /// <param name="entity">
    /// Объект с новыми значениями полей.
    /// </param>
    /// <param name="cancellationToken">
    /// Токен отмены операции.
    /// </param>
    /// <returns>
    /// Количество изменённых записей.
    /// </returns>
    Task<int> UpdateAsync<T>(
        string tableName,
        int id,
        T entity,
        CancellationToken cancellationToken = default)
        where T : class;
 
    /// <summary>
    /// Асинхронно удаляет запись по первичному ключу.
    /// </summary>
    /// <param name="tableName">
    /// Имя таблицы базы данных.
    /// </param>
    /// <param name="id">
    /// Значение первичного ключа Id.
    /// </param>
    /// <param name="cancellationToken">
    /// Токен отмены операции.
    /// </param>
    /// <returns>
    /// Количество удалённых записей.
    /// </returns>
    Task<int> DeleteAsync(
        string tableName,
        int id,
        CancellationToken cancellationToken = default);
}