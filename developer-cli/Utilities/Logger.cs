using PlatformPlatform.DeveloperCli.Installation;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class Logger
{
    private const string LogFileNameFormat = "developer-cli-{0:yyyy-MM-dd}.log";
    private static readonly string LogDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "logs");
    private static readonly AsyncLocal<string?> CurrentContext = new();

    public static void SetContext(string context)
    {
        CurrentContext.Value = context;
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
        var timestamp = DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm.sszzz");
        var context = CurrentContext.Value != null ? $"[{CurrentContext.Value}] " : "";
        var logEntry = $"[{timestamp}] [{level}] {context}{message}";
        var logFileName = string.Format(LogFileNameFormat, DateTime.Today);
        var logFilePath = Path.Combine(LogDirectory, logFileName);

        Directory.CreateDirectory(LogDirectory);
        File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
    }

    public static void CleanupOldLogs()
    {
        var cutoffDate = DateTime.Today.AddDays(30);
        var logFiles = Directory.GetFiles(LogDirectory, "developer-cli-*.log");

        var filesToDelete = logFiles
            .Select(f => new FileInfo(f))
            .Where(fi => fi.LastWriteTime.Date < cutoffDate)
            .ToArray();

        foreach (var file in filesToDelete)
        {
            File.Delete(file.FullName);
            Info($"Removed old log file: {file.Name}");
        }
    }
}
