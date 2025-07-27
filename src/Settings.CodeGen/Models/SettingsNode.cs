namespace Settings.CodeGen.Models;

/// <summary>
/// Ячейка для хранения настроек
/// </summary>
/// <param name="FullPath">Полный путь</param>
public record SettingsNode(string FullPath = "")
{
    /// <summary>
    /// Словарь для хранения значений настроек
    /// </summary>
    public Dictionary<string, string> Values { get; } = new();

    /// <summary>
    /// Словарь для хранения дочерних узлов настроек
    /// </summary>
    public Dictionary<string, SettingsNode> Children { get; } = new();

    /// <summary>Полный путь</summary>
    public string FullPath { get; } = FullPath;

    /// <summary>
    /// Получить или создать дочерний узел настроек
    /// </summary>
    /// <param name="className">Имя класса</param>
    /// <param name="originalName">Оригинальное имя</param>
    public SettingsNode GetOrCreateChild(string className, string originalName)
    {
        if (Children.TryGetValue(className, out SettingsNode? child))
        {
            return child;
        }

        string childPath = string.IsNullOrEmpty(FullPath) ? originalName : $"{FullPath}:{originalName}";
        Children[className] = new(childPath);
        return Children[className];
    }

    /// <summary>
    /// Добавить значение в текущий узел настроек
    /// </summary>
    /// <param name="className">Имя класса</param>
    /// <param name="originalName">Оригинальное имя</param>
    public void AddValue(string className, string originalName)
    {
        string valuePath = string.IsNullOrEmpty(FullPath) ? originalName : $"{FullPath}:{originalName}";
        Values[className] = valuePath;
    }
}
