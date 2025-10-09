using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ProcessHelper
{
    private static readonly string TempOutputDirectory = Path.Combine(Path.GetTempPath(), "platformplatform-mcp");

    static ProcessHelper()
    {
        // Ensure temp directory exists
        Directory.CreateDirectory(TempOutputDirectory);

        // Clean up old temp files (older than 24 hours)
        CleanupOldTempFiles();
    }

    private static void CleanupOldTempFiles()
    {
        try
        {
            if (!Directory.Exists(TempOutputDirectory)) return;

            var cutoffTime = DateTime.UtcNow.AddHours(-24);
            foreach (var file in Directory.GetFiles(TempOutputDirectory, "*.log"))
            {
                if (File.GetCreationTimeUtc(file) < cutoffTime)
                {
                    File.Delete(file);
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

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
            StartProcess(new ProcessStartInfo { FileName = url, UseShellExecute = true });
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

    public static ProcessResult ExecuteQuietly(
        string command,
        string? workingDirectory = null,
        params (string Name, string Value)[] environmentVariables
    )
    {
        var processStartInfo = CreateProcessStartInfo(command, workingDirectory, true);
        processStartInfo.RedirectStandardOutput = true;
        processStartInfo.RedirectStandardError = true;

        foreach (var environmentVariable in environmentVariables)
        {
            processStartInfo.Environment[environmentVariable.Name] = environmentVariable.Value;
        }

        using var process = Process.Start(processStartInfo)!;

        // Read stdout and stderr asynchronously to prevent deadlock
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        process.WaitForExit();

        var stdout = stdoutTask.Result;
        var stderr = stderrTask.Result;

        // Save full output to temp file
        var tempFile = Path.Combine(TempOutputDirectory, $"{Guid.NewGuid()}.log");
        var fullOutput = $"Command: {command}\nWorking Directory: {workingDirectory ?? "N/A"}\nExit Code: {process.ExitCode}\n\n=== STDOUT ===\n{stdout}\n\n=== STDERR ===\n{stderr}";
        File.WriteAllText(tempFile, fullOutput);

        return new ProcessResult(process.ExitCode, stdout, stderr, tempFile);
    }

    public static string StartProcess(
        string command,
        string? solutionFolder = null,
        bool redirectOutput = false,
        bool waitForExit = true,
        bool exitOnError = true,
        bool throwOnError = false,
        params (string Name, string Value)[] environmentVariables
    )
    {
        var processStartInfo = CreateProcessStartInfo(command, solutionFolder, redirectOutput);

        foreach (var environmentVariable in environmentVariables)
        {
            processStartInfo.Environment[environmentVariable.Name] = environmentVariable.Value;
        }

        return StartProcess(processStartInfo, waitForExit: waitForExit, exitOnError: exitOnError, throwOnError: throwOnError);
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
        string[] commandFormats = OperatingSystem.IsWindows() ? ["{0}.exe", "{0}.cmd"] : ["{0}"];

        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (pathVariable is null) return null;

        var separator = OperatingSystem.IsWindows() ? ';' : ':';
        foreach (var directory in pathVariable.Split(separator))
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

public record ProcessResult(int ExitCode, string StdOut, string StdErr, string TempFilePath)
{
    public bool Success => ExitCode == 0;

    public string CombinedOutput => $"{StdOut}\n{StdErr}";
}
