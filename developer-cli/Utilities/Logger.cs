using PlatformPlatform.DeveloperCli.Installation;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class Logger
{
    private const string LogFileNameFormat = "developer-cli-{0:yyyy-MM-dd}.log";
    private static readonly AsyncLocal<string?> CurrentContext = new();
    private static readonly AsyncLocal<string?> CurrentBranch = new();

    private static string GetLogDirectory()
    {
        var context = CurrentContext.Value;

        // Branch-agnostic agents (pair-programmer, tech-lead) use logs/ subdirectory
        if (context is "pair-programmer" or "tech-lead")
        {
            return Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", context, "logs");
        }

        // Branch-specific agents (coordinator, engineers, reviewers) use branch root
        var branch = CurrentBranch.Value ?? GitHelper.GetCurrentBranch();
        return Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch);
    }

    public static void SetContext(string context)
    {
        CurrentContext.Value = context;
    }

    public static void SetBranch(string branch)
    {
        CurrentBranch.Value = branch;
    }

    public static void ClearContext()
    {
        CurrentContext.Value = null;
    }

    public static void Info(string message)
    {
        WriteLog("INFO", message);
    }

    public static void Debug(string message)
    {
        WriteLog("DEBUG", message);
    }

    public static void Warning(string message)
    {
        WriteLog("WARNING", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var logMessage = ex is null ? message : $"{message}: {ex}";
        WriteLog("ERROR", logMessage);
    }

    private static void WriteLog(string level, string message)
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:sszzz");
        var context = CurrentContext.Value != null ? $"[{CurrentContext.Value}] " : "";
        var logEntry = $"[{timestamp}] [{level}] {context}{message}";
        var logFileName = string.Format(LogFileNameFormat, DateTime.Today);
        var logDirectory = GetLogDirectory();
        var logFilePath = Path.Combine(logDirectory, logFileName);

        Directory.CreateDirectory(logDirectory);
        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
    }
}
