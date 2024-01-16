using System.Security.Cryptography;
using System.Text.Json;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class ChangeDetection
{
    internal static void EnsureCliIsCompiledWithLatestChanges(string[] args)
    {
        if (Configuration.IsWindows)
        {
            // In Windows, the process is renamed to .previous.exe when updating to unblock publishing of new executable
            // We delete the previous executable the next time the process is started
            File.Delete(Environment.ProcessPath!.Replace(".exe", ".previous.exe"));
        }

        var storedHash = File.Exists(Configuration.HashFile) ? File.ReadAllText(Configuration.HashFile) : "";
        var currentHash = CalculateMd5HashForSolution();
        if (currentHash == storedHash) return;

        PublishDeveloperCli();
        
        var configuration = JsonSerializer.Serialize(new { SolutionFolder = Configuration.GetSolutionFolder() });
        File.WriteAllText(Configuration.ConfigFile, configuration);

        // Update the hash file to avoid restarting the process again
        File.WriteAllText(Configuration.HashFile, currentHash);

        // When running in debug mode, we want to avoid restarting the process
        var isDebugBuild = new FileInfo(Environment.ProcessPath!).FullName.Contains("debug");
        if (isDebugBuild) return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]CLI successfully updated. Please rerun the command.[/]");
        AnsiConsole.WriteLine();
        Environment.Exit(0);
    }

    private static string CalculateMd5HashForSolution()
    {
        // Get all files C# and C# project files in the Developer CLI solution
        var solutionFiles = Directory
            .EnumerateFiles(Configuration.GetSolutionFolder(), "*.cs*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("artifacts"))
            .ToList();

        using var sha256 = SHA256.Create();
        using var combinedStream = new MemoryStream();

        foreach (var file in solutionFiles)
        {
            using var fileStream = File.OpenRead(file);
            var hash = sha256.ComputeHash(fileStream);
            combinedStream.Write(hash, 0, hash.Length);
        }

        combinedStream.Position = 0;
        return BitConverter.ToString(sha256.ComputeHash(combinedStream));
    }

    private static void PublishDeveloperCli()
    {
        AnsiConsole.MarkupLine("[green]Changes detected, rebuilding and publishing new CLI.[/]");

        var currentExecutablePath = Environment.ProcessPath!;
        var renamedExecutablePath = "";

        try
        {
            // Build project before renaming exe on Windows
            ProcessHelper.StartProcess("dotnet build", Configuration.GetSolutionFolder());

            if (Configuration.IsWindows)
            {
                // In Windows the executing assembly is locked by the process, blocking overwriting it, but not renaming
                // We rename the current executable to .previous.exe to unblock publishing of new executable
                renamedExecutablePath = currentExecutablePath.Replace(".exe", ".previous.exe");
                File.Move(currentExecutablePath, renamedExecutablePath, true);
            }

            // Call "dotnet publish" to create a new executable
            ProcessHelper.StartProcess($"dotnet publish  DeveloperCli.csproj -o {Configuration.PublishFolder}",
                Configuration.GetSolutionFolder());
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Failed to publish new CLI. Please run 'dotnet run' to fix. {e.Message}[/]");
            Environment.Exit(0);
        }
        finally
        {
            if (renamedExecutablePath != "" && !File.Exists(currentExecutablePath))
            {
                // If the publish command did not successfully create a new executable, put back the old one to ensure
                // the CLI is still working
                File.Move(renamedExecutablePath, currentExecutablePath);
            }
        }
    }
}