using System.Collections.ObjectModel;

namespace Settings.CodeGen.UnitTests;

/// <summary>
/// Mock ��� IBuildEngine, ������������ � ������
/// </summary>
public class MockBuildEngine : IBuildEngine
{
    /// <summary>
    /// ��������������� ���������
    /// </summary>
    public Collection<string> LoggedMessages { get; } = [];

    /// <summary>
    /// ��������������� ��������������
    /// </summary>
    public Collection<string> LoggedWarnings { get; } = [];

    /// <summary>
    /// ��������������� ������
    /// </summary>
    public Collection<string> LoggedErrors { get; } = [];

    /// <inheritdoc />
    public bool ContinueOnError => false;

    /// <inheritdoc />
    public int LineNumberOfTaskNode => 0;

    /// <inheritdoc />
    public int ColumnNumberOfTaskNode => 0;

    /// <inheritdoc />
    public string ProjectFileOfTaskNode => Environment.CurrentDirectory;

    /// <inheritdoc />
    public void LogErrorEvent(BuildErrorEventArgs e)
    {
        if (e.Message is not null)
        {
            LoggedErrors.Add(e.Message);
        }
    }

    /// <inheritdoc />
    public void LogWarningEvent(BuildWarningEventArgs e)
    {
        if (e.Message is not null)
        {
            LoggedWarnings.Add(e.Message);
        }
    }

    /// <inheritdoc />
    public void LogMessageEvent(BuildMessageEventArgs e)
    {
        if (e.Message is not null)
        {
            LoggedMessages.Add(e.Message);
        }
    }

    /// <inheritdoc />
    public void LogCustomEvent(CustomBuildEventArgs e)
    {
        if (e.Message is not null)
        {
            LoggedMessages.Add(e.Message);
        }
    }

    /// <inheritdoc />
    public bool BuildProjectFile(
        string projectFileName,
        string[] targetNames,
        IDictionary globalProperties,
        IDictionary targetOutputs) =>
        true;
}
