using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ProcessHelper
{
    public static string StartProcess(string command, string? solutionFolder = null, bool redirectOutput = false)
    {
        var fileName = command.Split(' ')[0];
        var arguments = command.Length > fileName.Length ? command.Substring(fileName.Length + 1) : null;
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments ?? string.Empty,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput,
            UseShellExecute = false
        };

        if (solutionFolder is not null)
        {
            processStartInfo.WorkingDirectory = solutionFolder;
        }

        return StartProcess(processStartInfo);
    }

    public static string StartProcess(ProcessStartInfo processStartInfo, string? input = null, bool waitForExit = true)
    {
        if (Configuration.VerboseLogging)
        {
            var escapedArguments = Markup.Escape(processStartInfo.Arguments);
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