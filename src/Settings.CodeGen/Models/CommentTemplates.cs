namespace Settings.CodeGen.Models;

internal static class CommentTemplates
{
    public static string Key(string path, string lang) => lang switch
    {
        "ru" => $"Ключ конфигурации: <c>{path}</c>",
        _ => $"Configuration key: <c>{path}</c>"
    };

    public static string Section(string path, string lang) => lang switch
    {
        "ru" => $"Секция конфигурации: <c>{path}</c>",
        _ => $"Configuration section: <c>{path}</c>"
    };

    public static string SectionName(string sectionName, string lang) => lang switch
    {
        "ru" => $"Путь секции: <c>{sectionName}</c>",
        _ => $"Section path: <c>{sectionName}</c>"
    };

    public static string ClassSummary(string lang) => lang switch
    {
        "ru" => "/// Строго типизированные ключи для конфигурации приложения",
        _ => "/// Strongly-typed keys for application settings"
    };
}
