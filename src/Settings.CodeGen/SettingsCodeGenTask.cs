namespace Settings.CodeGen;

/// <summary>
/// MSBuild task to generate constants from appsettings.json
/// </summary>
public class SettingsCodeGenTask : Task
{
    private const string SettingsClassName = "AppSettingsKeys";
    private const string SettingsFileName = $"{SettingsClassName}.appsettings.g.cs";

    private static readonly HashSet<string> ReservedWords =
    [
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
        "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
        "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
        "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
        "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
        "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
        "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    ];

    /// <summary>
    /// Path to the project folder with configs (if not specified, the current project is used)
    /// </summary>
    public string? SettingsProjectDir { get; set; }

    /// <summary>
    /// Namespace name for the generated class
    /// </summary>
    public string SettingsNamespaceName { get; set; } = "Generated";

    /// <summary>
    /// Localization
    /// </summary>
    public string? SettingsCodeGenLocalization { get; set; } = "en";

    /// <summary>
    /// Path to the build output directory.
    /// Used to store service files such as the relevance marker.
    /// </summary>
    [Required]
    public string MarkerOutputPath { get; set; } = string.Empty;

    /// <summary>
    /// Folder with generated file
    /// </summary>
    [Output]
    public string GeneratedFileDir { get; set; } = string.Empty;

    /// <inheritdoc />
    public override bool Execute()
    {
        SettingsProjectDir ??= Environment.CurrentDirectory;
        GeneratedFileDir = Path.Combine(Environment.CurrentDirectory, "Generated");

        if (string.IsNullOrEmpty(SettingsProjectDir))
        {
            Log.LogError("SettingsProjectDir is required");
            return false;
        }

        if (!Directory.Exists(SettingsProjectDir))
        {
            Log.LogError($"SettingsProjectDir does not exist: {SettingsProjectDir}");
            return false;
        }

        string language = SettingsCodeGenLocalization?.ToLowerInvariant() ?? "en";

        try
        {
            string[] settingsFiles = Directory.GetFiles(SettingsProjectDir, "appsettings*.json");

            if (settingsFiles.Length is 0)
            {
                Log.LogMessage(MessageImportance.Low, "No appsettings*.json files found");
                return true;
            }

            string outputFile = Path.Combine(GeneratedFileDir, SettingsFileName);

            CleanupGeneratedFiles(GeneratedFileDir, outputFile);

            if (IsUpToDate(settingsFiles, outputFile, SettingsNamespaceName, language))
            {
                Log.LogMessage(MessageImportance.Low, "Generated file is up to date, skipping generation.");
                return true;
            }

            SettingsNode root = new();

            foreach (string file in settingsFiles)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    JsonDocumentOptions options = new()
                    {
                        CommentHandling = JsonCommentHandling.Skip,
                        AllowTrailingCommas = true
                    };
                    using JsonDocument doc = JsonDocument.Parse(json, options);
                    ProcessElement(doc.RootElement, root);
                    Log.LogMessage(MessageImportance.Low, $"Processed {Path.GetFileName(file)}");
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Error processing {file}: {ex.Message}");
                }
            }

            string generatedClass = GenerateSettingsClass(root, SettingsNamespaceName, language);

            Directory.CreateDirectory(GeneratedFileDir);
            File.WriteAllText(outputFile, generatedClass);

            File.SetLastWriteTimeUtc(outputFile, DateTime.UtcNow);

            SaveUpToDateMarker(settingsFiles, SettingsNamespaceName, language);

            Log.LogMessage(MessageImportance.Normal, $"Generated {SettingsFileName}");
            return true;
        }
        catch (Exception ex)
        {
            Log.LogErrorFromException(ex, true);
            return false;
        }
    }

    private static void ProcessElement(JsonElement element, SettingsNode parent)
    {
        foreach (JsonProperty property in element.EnumerateObject())
        {
            string className = ToValidClassName(property.Name);

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    SettingsNode child = parent.GetOrCreateChild(className, property.Name);
                    ProcessElement(property.Value, child);
                    break;
                case JsonValueKind.Array:
                    for (int i = 0; i < property.Value.GetArrayLength(); i++)
                    {
                        parent.AddValue($"{className}_{i}", $"{property.Name}:{i}");
                    }

                    break;
                default:
                    parent.AddValue(className, property.Name);
                    break;
            }
        }
    }

    private static string GenerateSettingsClass(SettingsNode root, string namespaceName, string language)
    {
        StringBuilder sb = new();

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine();
        sb.AppendLine($"namespace {namespaceName};");
        sb.AppendLine();
        sb.AppendLine("/// <summary>");
        sb.AppendLine(CommentTemplates.ClassSummary(language));
        sb.AppendLine("/// </summary>");
        sb.AppendLine($"""[System.CodeDom.Compiler.GeneratedCode("{nameof(SettingsCodeGenTask)}", "1.0.0")]""");
        sb.AppendLine($"public static class {SettingsClassName}");
        sb.AppendLine("{");
        GenerateNode(sb, root, 1, language);
        sb.AppendLine("}");

        return sb.ToString();
    }

    private static void GenerateNode(StringBuilder sb, SettingsNode node, int indent, string language)
    {
        string indentStr = new(' ', indent * 4);
        bool isFirstElement = true;

        foreach (KeyValuePair<string, SettingsNode> child in node.Children.OrderBy(x => x.Key))
        {
            if (!isFirstElement)
            {
                sb.AppendLine();
            }

            sb.AppendLine($"{indentStr}/// <summary>");
            sb.AppendLine($"{indentStr}/// {CommentTemplates.Section(child.Value.FullPath, language)}");
            sb.AppendLine($"{indentStr}/// </summary>");
            sb.AppendLine($"{indentStr}public static class {child.Key}");
            sb.AppendLine($"{indentStr}{{");
            sb.AppendLine($"{indentStr}    /// <summary>");
            sb.AppendLine($"{indentStr}    /// {CommentTemplates.SectionName(child.Key, language)}");
            sb.AppendLine($"{indentStr}    /// </summary>");
            sb.AppendLine($"{indentStr}    public const string Section = \"{child.Value.FullPath}\";");
            sb.AppendLine();

            GenerateNode(sb, child.Value, indent + 1, language);
            sb.AppendLine($"{indentStr}}}");

            isFirstElement = false;
        }

        foreach (KeyValuePair<string, string> value in node.Values.OrderBy(x => x.Key))
        {
            if (!isFirstElement)
            {
                sb.AppendLine();
            }

            sb.AppendLine($"{indentStr}/// <summary>");
            sb.AppendLine($"{indentStr}/// {CommentTemplates.Key(value.Value, language)}");
            sb.AppendLine($"{indentStr}/// </summary>");
            sb.AppendLine($"{indentStr}public const string {value.Key} = \"{value.Value}\";");

            isFirstElement = false;
        }
    }

    private static string ToValidClassName(string name)
    {
        string result = name.Replace("-", "_").Replace(".", "_");

        result = new([.. result.Where(c => char.IsLetterOrDigit(c) || c is '_')]);

        if (char.IsDigit(result[0]))
        {
            result = "_" + result;
        }

        if (ReservedWords.Contains(result))
        {
            result = "@" + result;
        }

        return result;
    }

    private bool IsUpToDate(string[] settingsFiles, string outputFile, string namespaceName, string language)
    {
        string markerFilePath = GetMarkerFilePath(MarkerOutputPath);

        if (!File.Exists(outputFile))
        {
            Log.LogMessage(MessageImportance.Low, "Output file does not exist, regeneration required.");
            return false;
        }

        if (!File.Exists(markerFilePath))
        {
            Log.LogMessage(MessageImportance.Low, "Marker file does not exist, regeneration required.");
            return false;
        }

        try
        {
            string savedState = File.ReadAllText(markerFilePath).Trim();
            if (string.IsNullOrEmpty(savedState))
            {
                Log.LogMessage(MessageImportance.Low, "Marker file is empty or invalid, regeneration required.");
                return false;
            }

            string currentState = CalculateInputHash(settingsFiles, namespaceName, language);

            if (!string.Equals(currentState, savedState, StringComparison.Ordinal))
            {
                Log.LogMessage(MessageImportance.Low, "Input state changed, regeneration required.");
                return false;
            }

            DateTime outputWriteTime = File.GetLastWriteTimeUtc(outputFile);
            if (!settingsFiles.Any(t => File.GetLastWriteTimeUtc(t) > outputWriteTime))
            {
                return true;
            }

            Log.LogMessage(MessageImportance.Low, "Input file is newer than output, regeneration required.");
            return false;
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Error during up-to-date check: {ex.Message}. Assuming regeneration is required.");
            return false;
        }
    }

    private void SaveUpToDateMarker(string[] settingsFiles, string namespaceName, string language)
    {
        try
        {
            string markerFilePath = GetMarkerFilePath(MarkerOutputPath);

            string currentState = CalculateInputHash(settingsFiles, namespaceName, language);
            File.WriteAllText(markerFilePath, currentState);
            Log.LogMessage(MessageImportance.Low, $"Saved up-to-date marker to: {markerFilePath}");
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Failed to save up-to-date marker: {ex.Message}");
        }
    }

    private static string CalculateInputHash(string[] files, string? settingNamespace, string? language)
    {
        using SHA256? sha256 = SHA256.Create();
        StringBuilder sb = new();

        foreach (string? file in files.OrderBy(f => f))
        {
            sb.Append(file);
            sb.Append(File.GetLastWriteTimeUtc(file).Ticks.ToString(CultureInfo.InvariantCulture));
        }

        sb.Append(settingNamespace ?? string.Empty);
        sb.Append(language ?? string.Empty);

        byte[] inputBytes = Encoding.UTF8.GetBytes(sb.ToString());
        byte[] hashBytes = sha256.ComputeHash(inputBytes);
        return Convert.ToBase64String(hashBytes);
    }

    private void CleanupGeneratedFiles(string generatedDirectory, string currentOutputFile)
    {
        try
        {
            if (!Directory.Exists(generatedDirectory))
            {
                return;
            }

            string[] oldFiles = Directory.GetFiles(generatedDirectory, $"*{SettingsFileName}", SearchOption.TopDirectoryOnly);

            foreach (string oldFile in oldFiles)
            {
                if (!string.Equals(oldFile, currentOutputFile, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        File.Delete(oldFile);
                        Log.LogMessage(MessageImportance.Low, $"Deleted stale generated file: {oldFile}");
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Failed to delete stale generated file '{oldFile}': {ex.Message}");
                    }
                }
                else
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipped current output file during cleanup: {oldFile}");
                }
            }
        }
        catch (Exception ex)
        {
            Log.LogWarning($"Error during cleanup of generated files in '{generatedDirectory}': {ex.Message}");
        }
    }

    private static string GetMarkerFilePath(string markerOutputPath) =>
        Environment.CurrentDirectory.EndsWith(markerOutputPath, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine(Environment.CurrentDirectory, "SettingsCodeGen.marker")
            : Path.Combine(Environment.CurrentDirectory, markerOutputPath, "SettingsCodeGen.marker");
}
