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
                WorkingDirectory = workingDirectory
            }
        };

        process.Start();
        process.WaitForExit();
    }
}