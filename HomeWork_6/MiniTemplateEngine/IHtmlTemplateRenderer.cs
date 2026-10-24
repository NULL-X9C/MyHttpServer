namespace MiniTemplateEngine;

/// <summary>
/// Определяет контракт HTML-шаблонизатора для формирования HTML-разметки
/// на основе шаблона и переданной модели данных.
/// </summary>
/// <remarks>
/// Поддерживает подстановку значений свойств модели (${Property}),
/// условные конструкции ($if, $else, $endif)
/// и циклы ($foreach, $endfor).
/// </remarks>
public interface IHtmlTemplateRenderer
{
    /// <summary>
    /// Выполняет обработку HTML-шаблона, переданного в виде строки.
    /// </summary>
    /// <param name="htmlTemplate">
    /// Строка, содержащая HTML-шаблон с переменными,
    /// условными конструкциями и циклами.
    /// </param>
    /// <param name="dataModel">
    /// Объект модели данных, значения свойств которого
    /// используются при обработке шаблона.
    /// </param>
    /// <returns>
    /// Строка с готовой HTML-разметкой после обработки шаблона.
    /// </returns>
    string RenderFromString(string htmlTemplate, object dataModel);
 
    /// <summary>
    /// Выполняет обработку HTML-шаблона, загруженного из файла.
    /// </summary>
    /// <param name="filePath">
    /// Путь к файлу, содержащему исходный HTML-шаблон.
    /// </param>
    /// <param name="dataModel">
    /// Объект модели данных, значения свойств которого
    /// используются при обработке шаблона.
    /// </param>
    /// <returns>
    /// Строка с готовой HTML-разметкой после обработки шаблона.
    /// </returns>
    string RenderFromFile(string filePath, object dataModel);
 
    /// <summary>
    /// Выполняет обработку HTML-шаблона из файла
    /// и сохраняет результат в указанный файл.
    /// </summary>
    /// <param name="inputFilePath">
    /// Путь к файлу, содержащему исходный HTML-шаблон.
    /// </param>
    /// <param name="outputFilePath">
    /// Путь к файлу для сохранения сформированной HTML-разметки.
    /// </param>
    /// <param name="dataModel">
    /// Объект модели данных, значения свойств которого
    /// используются при обработке шаблона.
    /// </param>
    /// <returns>
    /// Строка с готовой HTML-разметкой, сохранённой в файл.
    /// </returns>
    string RenderToFile(
        string inputFilePath,
        string outputFilePath,
        object dataModel);
}