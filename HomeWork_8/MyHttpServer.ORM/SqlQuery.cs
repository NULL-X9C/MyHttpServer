namespace MyHttpServer.ORM;

/// <summary>
/// <param name="Command"> sql запрос с параметрами (например @name)</param>
/// <param name="Parameters"> ключ параметр значение - данные сущности (например [@name] = Васян </param>
/// </summary>
public record SqlQuery(
    string Command,
    Dictionary<string, object?> Parameters
);


    