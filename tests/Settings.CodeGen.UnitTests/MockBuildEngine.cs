namespace Settings.CodeGen.UnitTests;

public class MockBuildEngine : IBuildEngine
{
    private readonly List<string> loggedMessages = [];
    private readonly List<string> loggedWarnings = [];
    private readonly List<string> loggedErrors = [];

    public List<string> LoggedMessages => loggedMessages;

    public List<string> LoggedWarnings => loggedWarnings;

    public List<string> LoggedErrors => loggedErrors;

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
            loggedErrors.Add(e.Message);
        }
    }

    /// <inheritdoc />
    public void LogWarningEvent(BuildWarningEventArgs e)
    {
        if (e.Message is not null)
        {
            loggedWarnings.Add(e.Message);
        }
    }

    /// <inheritdoc />
    public void LogMessageEvent(BuildMessageEventArgs e)
    {
        if (e.Message is not null)
        {
            loggedMessages.Add(e.Message);
        }
    }

    /// <inheritdoc />
    public void LogCustomEvent(CustomBuildEventArgs e)
    {
        if (e.Message is not null)
        {
            loggedMessages.Add(e.Message);
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
