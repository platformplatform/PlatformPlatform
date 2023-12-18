using System.Diagnostics;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ProcessHelper
{
    public static string StartProcess(
        string command,
        string arguments,
        string? workingDirectory = null,
        bool redirectStandardOutput = false,
        bool createNoWindow = false,
        bool waitForExit = true,
        bool printCommand = true
    )
    {
        if (printCommand) AnsiConsole.MarkupLine($"[cyan]{command} {arguments}[/]");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = command,
            Arguments = arguments,
            RedirectStandardOutput = redirectStandardOutput,
            CreateNoWindow = createNoWindow
        };

        if (workingDirectory is not null) processStartInfo.WorkingDirectory = workingDirectory;

        var process = Process.Start(processStartInfo)!;

        if (!waitForExit) return string.Empty;

        var output = string.Empty;
        if (redirectStandardOutput) output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }
}