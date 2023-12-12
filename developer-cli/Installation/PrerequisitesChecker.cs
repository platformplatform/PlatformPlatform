using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class PrerequisitesChecker
{
    public static void EnsurePrerequisitesAreMet()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AnsiConsole.MarkupLine("[red]Only Windows and macOS are currently supported.[/]");
            Environment.Exit(1);
        }

        var checkAzureCli = CheckCommandLineTool("az", successMessage: "Your CLI is up-to-date.");
        var checkBun = CheckCommandLineTool("bun", new Version(1, 0));

        if (checkAzureCli == false || checkBun == false)
        {
            Environment.Exit(1);
        }
    }

    private static bool CheckCommandLineTool(string command, Version? minVersion = null, string? successMessage = null)
    {
        // Check if the command line tool is installed
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var checkCommand = isWindows ? "where" : "which";

        var checkProcess = Process.Start(new ProcessStartInfo
        {
            FileName = checkCommand,
            Arguments = command,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        var checkOutput = checkProcess!.StandardOutput.ReadToEnd();
        if (string.IsNullOrWhiteSpace(checkOutput))
        {
            AnsiConsole.MarkupLine($"[red]´{command}´ is not installed. This tool is required by PlatformPlatform.[/]");
            return false;
        }

        // Get the version of the command line tool
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = command,
            Arguments = "--version",
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        var output = process!.StandardOutput.ReadToEnd();

        // Check if the version is greater than the minimum required version
        if (minVersion is not null)
        {
            var versionString = output.Split('\n')[0]; // Assuming the version is on the first line
            if (!Version.TryParse(versionString, out var installedVersion) || installedVersion < minVersion)
            {
                AnsiConsole.MarkupLine($"[red]´{command}´ version is less than {minVersion}. Please update.[/]");
                return false;
            }
        }

        // Some tools don't have the version easily readable in the output, so we check for a special success message
        if (successMessage is not null && !output.Contains(successMessage))
        {
            AnsiConsole.MarkupLine($"[red]´{command}´ is not up-to-date. Please update to the latest version.[/]");
            return false;
        }

        return true;
    }
}