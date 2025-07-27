# Settings.CodeGen

Generates strongly-typed constants for appsettings.json(appsettings.{Environment}.json) configuration keys. This helps prevent typos and improves refactorability when accessing configuration values.

## Usage

1. Install the package:
```bash
dotnet add package Settings.CodeGen
```
2. Build your project. Constants will be generated automatically in the `Generated` folder (relative to the project file).
3. Use the generated keys in your code:
```cs
string conn = configuration[AppSettingsKeys.ConnectionStrings.DefaultConnection];
```

## Configuration

You can configure the code generation by adding properties to your project file (`.csproj`).

Add a `<PropertyGroup>` and set the desired properties:

```xml
<PropertyGroup>
  <!-- The namespace for the generated class. Default: '$(MSBuildProjectName).Generated' -->
  <SettingsCodeGenNamespace>MyApp.Config</SettingsCodeGenNamespace>

  <!-- The path to the project directory containing the appsettings.json files.
       Useful if settings are in a different project/directory.
       Default: '$(MSBuildProjectDirectory)' (current project directory) -->
  <SettingsCodeGenProjectDir>/src/projectWithAppsettingFiles</SettingsCodeGenProjectDir>

  <!-- The language for the generated comments (e.g., 'en', 'ru').
       Default: 'en' -->
  <SettingsCodeGenLocalization>en</SettingsCodeGenLocalization>

  <!-- Enables or disables the automatic generation of settings keys.
       Set to 'false' to disable. Default: 'true' -->
  <SettingsCodeGenEnabled>false</SettingsCodeGenEnabled>
</PropertyGroup>
```

## Minimal Configuration Examples

__To use defaults (generate in `$(MSBuildProjectName).Generated.AppSettingsKeys`):__ 
> No configuration needed. Just install the package and build.

__To disable generation for a specific project:__
```xml
<PropertyGroup>
  <SettingsCodeGenEnabled>true</SettingsCodeGenEnabled>
</PropertyGroup>
```

__To change the output namespace:__
```xml
<PropertyGroup>
  <SettingsCodeGenNamespace>MyCompany.MyApp.Configuration</SettingsCodeGenNamespace>
</PropertyGroup>
```
