namespace Settings.CodeGen.UnitTests;

/// <summary>
/// Тесты для задачи SettingsCodeGenTask
/// </summary>
public class SettingsCodeGenTaskTests : IDisposable
{
    private readonly string testDirectory;
    private readonly MockBuildEngine buildEngine;

    /// <summary>
    /// Инициализация тестов
    /// </summary>
    public SettingsCodeGenTaskTests()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(testDirectory);
        buildEngine = new();
    }

    /// <summary>
    /// Тестирование генерации класса из appsettings.json
    /// </summary>
    [Fact]
    public void Execute_WithValidAppSettings_GeneratesClass()
    {
        const string appSettingsContent = /*lang=json,strict*/ """
                                          {
                                            "ConnectionStrings": {
                                              "DefaultConnection": "Server=localhost;Database=Test"
                                            },
                                            "Logging": {
                                              "LogLevel": {
                                                "Default": "Information"
                                              }
                                            },
                                            "AppName": "TestApp"
                                          }
                                          """;
        File.WriteAllText(Path.Combine(testDirectory, "appsettings.json"), appSettingsContent);
        SettingsCodeGenTask task = new()
        {
            SettingsProjectDir = testDirectory,
            SettingsNamespaceName = "TestNamespace",
            BuildEngine = buildEngine,
            MarkerOutputPath = """\bin\Debug\net9.0"""
        };

        bool result = task.Execute();

        Assert.True(result);
        string generatedFile = Path.Combine(Environment.CurrentDirectory, "Generated", "AppSettingsKeys.appsettings.g.cs");
        Assert.True(File.Exists(generatedFile));
        string generatedCode = File.ReadAllText(generatedFile);
        Assert.Contains("namespace TestNamespace;", generatedCode);
        Assert.Contains("public static class AppSettingsKeys", generatedCode);
        Assert.Contains("public const string AppName = \"AppName\";", generatedCode);
        Assert.Contains("public static class ConnectionStrings", generatedCode);
        Assert.Contains("public const string Section = \"ConnectionStrings\";", generatedCode);
    }

    /// <summary>
    /// Тестирование генерации класса с несколькими appsettings.json
    /// </summary>
    [Fact]
    public void Execute_WithMultipleAppSettings_MergesCorrectly()
    {
        const string appSettings = /*lang=json,strict*/ """{"Database": {"Host": "localhost"}}""";
        const string appSettingsDev = /*lang=json,strict*/ """{"Database": {"Port": 5432}, "Debug": true}""";
        File.WriteAllText(Path.Combine(testDirectory, "appsettings.json"), appSettings);
        File.WriteAllText(Path.Combine(testDirectory, "appsettings.Development.json"), appSettingsDev);
        SettingsCodeGenTask task = new()
        {
            SettingsProjectDir = testDirectory,
            BuildEngine = buildEngine,
            MarkerOutputPath = """\bin\Debug\net9.0"""
        };

        bool result = task.Execute();

        Assert.True(result);
        string generatedCode = File.ReadAllText(Path.Combine(Environment.CurrentDirectory, "Generated", "AppSettingsKeys.appsettings.g.cs"));
        Assert.Contains("public const string Host = \"Database:Host\";", generatedCode);
        Assert.Contains("public const string Port = \"Database:Port\";", generatedCode);
        Assert.Contains("public const string Debug = \"Debug\";", generatedCode);
    }

    /// <summary>
    /// Тестирование обработки некорректного JSON
    /// </summary>
    [Fact]
    public void Execute_WithInvalidJson_ContinuesWithWarning()
    {
        File.WriteAllText(Path.Combine(testDirectory, "appsettings.json"), "invalid json");
        File.WriteAllText(Path.Combine(testDirectory, "appsettings.Development.json"), /*lang=json,strict*/ """{"Valid": "setting"}""");
        SettingsCodeGenTask task = new()
        {
            SettingsProjectDir = testDirectory,
            BuildEngine = buildEngine,
            MarkerOutputPath = """\bin\Debug\net9.0"""
        };

        bool result = task.Execute();

        Assert.True(result);
        Assert.Contains(buildEngine.LoggedWarnings, w => w.Contains("Error processing") && w.Contains("appsettings.json"));
        string generatedCode = File.ReadAllText(Path.Combine(
            Environment.CurrentDirectory,
            "Generated",
            "AppSettingsKeys.appsettings.g.cs"));
        Assert.Contains("public const string Valid = \"Valid\";", generatedCode);
    }

    /// <summary>
    /// Тестирование отсутствия appsettings.json
    /// </summary>
    [Fact]
    public void Execute_WithNoAppSettings_ReturnsTrue()
    {
        SettingsCodeGenTask task = new()
        {
            SettingsProjectDir = testDirectory,
            BuildEngine = buildEngine,
            MarkerOutputPath = """\bin\Debug\net9.0"""
        };

        bool result = task.Execute();

        Assert.True(result);
        Assert.Contains(buildEngine.LoggedMessages, m => m.Contains("No appsettings*.json files found"));
        Assert.False(Directory.Exists(Path.Combine(testDirectory, "Generated")));
    }

    /// <summary>
    /// Тестирование генерации классов с именами, содержащими специальные символы
    /// </summary>
    [Fact]
    public void Execute_WithSpecialCharacters_GeneratesValidClassNames()
    {
        const string appSettings = /*lang=json,strict*/ """
                                   {
                                     "some-key": "value",
                                     "another.key": "value2",
                                     "123numeric": "value3"
                                   }
                                   """;
        File.WriteAllText(Path.Combine(testDirectory, "appsettings.json"), appSettings);
        SettingsCodeGenTask task = new()
        {
            SettingsProjectDir = testDirectory,
            BuildEngine = buildEngine,
            MarkerOutputPath = """\bin\Debug\net9.0"""
        };

        bool result = task.Execute();

        Assert.True(result);
        string generatedCode = File.ReadAllText(Path.Combine(
            Environment.CurrentDirectory,
            "Generated",
            "AppSettingsKeys.appsettings.g.cs"));
        Assert.Contains("public const string some_key = \"some-key\";", generatedCode);
        Assert.Contains("public const string another_key = \"another.key\";", generatedCode);
        Assert.Contains("public const string _123numeric = \"123numeric\";", generatedCode);
    }

    /// <summary>
    /// Тестирование генерации ключей для массивов в appsettings.json
    /// </summary>
    [Fact]
    public void Execute_WithArrayInAppSettings_GeneratesIndexedKeys()
    {
        // Arrange
        const string appSettingsContent = /*lang=json,strict*/ """
                                          {
                                            "AllowedHosts": [ "localhost", "example.com" ],
                                            "DetailedSettings": {
                                               "IpAddresses": [ "192.168.1.1", "10.0.0.1" ]
                                            }
                                          }
                                          """;
        File.WriteAllText(Path.Combine(testDirectory, "appsettings.json"), appSettingsContent);
        SettingsCodeGenTask task = new()
        {
            SettingsProjectDir = testDirectory,
            BuildEngine = buildEngine,
            MarkerOutputPath = """\bin\Debug\net9.0"""
        };


        bool result = task.Execute();

        Assert.True(result);
        string generatedFile = Path.Combine(Environment.CurrentDirectory, "Generated", "AppSettingsKeys.appsettings.g.cs");
        Assert.True(File.Exists(generatedFile), $"Expected generated file not found: {generatedFile}");
        string generatedCode = File.ReadAllText(generatedFile);
        Assert.Contains("public const string AllowedHosts_0 = \"AllowedHosts:0\";", generatedCode);
        Assert.Contains("public const string AllowedHosts_1 = \"AllowedHosts:1\";", generatedCode);
        Assert.Contains("public const string IpAddresses_0 = \"DetailedSettings:IpAddresses:0\";", generatedCode);
        Assert.Contains("public const string IpAddresses_1 = \"DetailedSettings:IpAddresses:1\";", generatedCode);
        Assert.Contains("public static class DetailedSettings", generatedCode);
        int detailedSettingsStart = generatedCode.IndexOf("public static class DetailedSettings", StringComparison.Ordinal);
        int ipAddress0Declaration = generatedCode.IndexOf("public const string IpAddresses_0", StringComparison.Ordinal);
        Assert.True(detailedSettingsStart >= 0, "DetailedSettings class not found");
        Assert.True(ipAddress0Declaration > detailedSettingsStart, "IpAddresses_0 not found inside DetailedSettings class");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Directory.Exists(testDirectory))
        {
            Directory.Delete(testDirectory, true);
        }
    }
}
