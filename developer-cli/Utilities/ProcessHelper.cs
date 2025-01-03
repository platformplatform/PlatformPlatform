using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ProcessHelper
{
    public static void StartProcessWithSystemShell(string command, string? solutionFolder = null)
    {
        var processStartInfo = CreateProcessStartInfo(command, solutionFolder, useShellExecute: true, createNoWindow: false);
        using var process = Process.Start(processStartInfo)!;
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            Environment.Exit(process.ExitCode);
        }
    }

    public static void OpenBrowser(string url)
    {
        if (Configuration.IsWindows)
        {
            StartProcess($"start {url}");
        }
        else if (Configuration.IsMacOs)
        {
            StartProcess($"open {url}");
        }
        else if (Configuration.IsLinux)
        {
            StartProcess($"xdg-open {url}");
        }
    }

    public static string StartProcess(
        string command,
        string? solutionFolder = null,
        bool redirectOutput = false,
        bool waitForExit = true,
        bool exitOnError = true,
        bool throwOnError = false
    )
    {
        var processStartInfo = CreateProcessStartInfo(command, solutionFolder, redirectOutput);
        return StartProcess(processStartInfo, waitForExit: waitForExit, exitOnError: exitOnError, throwOnError: throwOnError);
    }

    private static ProcessStartInfo CreateProcessStartInfo(
        string command,
        string? solutionFolder,
        bool redirectOutput = false,
        bool useShellExecute = false,
        bool createNoWindow = false
    )
    {
        var originalFileName = command.Split(' ')[0];
        var fileName = FindFullPathFromPath(originalFileName) ?? throw new FileNotFoundException($"Command '{command}' not found");
        var arguments = command.Contains(' ') ? command[(originalFileName.Length + 1)..] : string.Empty;
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = redirectOutput,
            RedirectStandardError = redirectOutput,
            UseShellExecute = useShellExecute,
            CreateNoWindow = createNoWindow
        };

        if (solutionFolder is not null)
        {
            processStartInfo.WorkingDirectory = solutionFolder;
        }

        return processStartInfo;
    }

    public static string StartProcess(
        ProcessStartInfo processStartInfo,
        string? input = null,
        bool waitForExit = true,
        bool exitOnError = true,
        bool throwOnError = false
    )
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

        if (process.ExitCode != 0)
        {
            if (throwOnError)
            {
                throw new ProcessExecutionException(process.ExitCode, $"Process exited with code {process.ExitCode}");
            }

            if (exitOnError)
            {
                Environment.Exit(process.ExitCode);
            }
        }

        return output;
    }

    public static bool IsProcessRunning(string process)
    {
        return Process.GetProcessesByName(process).Length > 0;
    }

    private static string? FindFullPathFromPath(string command)
    {
        Debug.Assert(!string.IsNullOrWhiteSpace(command));

        string[] commandFormats = OperatingSystem.IsWindows() ? ["{0}.exe", "{0}.cmd"] : ["{0}"];

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        foreach (var directory in pathVariable?.Split(';') ?? [])
        {
            foreach (var format in commandFormats)
            {
                var fullPath = Path.Combine(directory, string.Format(format, command));

                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }
}

public class ProcessExecutionException(int exitCode, string message)
    : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}
