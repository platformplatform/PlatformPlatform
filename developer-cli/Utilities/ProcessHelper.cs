using System.Diagnostics;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ProcessHelper
{
    public static string StartProcess(ProcessStartInfo processStartInfo, string? input = null, bool waitForExit = true)
    {
        if (Environment.VerboseLogging)
        {
            var escapedArguments = processStartInfo.Arguments.Replace("[", "[[").Replace("]", "]]");
            AnsiConsole.MarkupLine($"[cyan]{processStartInfo.FileName} {escapedArguments}[/]");
        }

        var process = Process.Start(processStartInfo)!;
        if (input is not null)
        {
            process.StandardInput.WriteLine(input);
            process.StandardInput.Close();
        }

        var output = string.Empty;
        if (processStartInfo.RedirectStandardOutput) output += process.StandardOutput.ReadToEnd();
        if (processStartInfo.RedirectStandardError) output += process.StandardError.ReadToEnd();

        if (!waitForExit) return string.Empty;
        process.WaitForExit();

        return output;
    }
}