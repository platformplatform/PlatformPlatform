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
    }
    
    public static string StartProcess(
        string command,
        string? solutionFolder = null,
        bool redirectOutput = false,
        bool waitForExit = true
    )
    {
        var processStartInfo = CreateProcessStartInfo(command, solutionFolder, redirectOutput);
        return StartProcess(processStartInfo, waitForExit: waitForExit);
    }
    
    private static ProcessStartInfo CreateProcessStartInfo(
        string command,
        string? solutionFolder,
        bool redirectOutput = false,
        bool useShellExecute = false,
        bool createNoWindow = false
    )
    {
        var fileName = command.Split(' ')[0];
        var arguments = command.Length > fileName.Length ? command.Substring(fileName.Length + 1) : string.Empty;
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
    
    public static bool IsProcessRunning(string process)
    {
        return Process.GetProcessesByName(process).Length > 0;
    }
}
