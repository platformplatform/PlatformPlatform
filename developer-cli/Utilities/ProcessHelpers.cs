using System.Diagnostics;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ProcessHelpers
{
    public static void StartProcess(string command, string arguments, string workingDirectory)
    {
        AnsiConsole.MarkupLine($"[cyan]{command} {arguments}[/]");
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = false
            }
        };

        process.OutputDataReceived += (_, e) => { Console.WriteLine(e.Data); };
        process.ErrorDataReceived += (_, e) => { Console.WriteLine(e.Data); };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();
    }
}