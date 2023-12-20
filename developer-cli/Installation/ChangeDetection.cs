using System.Security.Cryptography;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class ChangeDetection
{
    internal static void EnsureCliIsCompiledWithLatestChanges(string[] args)
    {
        var currentExecutablePath = System.Environment.ProcessPath!; // Don't inline as it can be renamed in Windows

        if (Environment.IsWindows)
        {
            // In Windows, the process is renamed to .previous.exe when updating to unblock publishing of new executable
            // We delete the previous executable the next time the process is started
            File.Delete(currentExecutablePath.Replace(".exe", ".previous.exe"));
        }

        var hashFile = Path.Combine(Environment.PublishFolder, "source-file-hash.md5");
        var storedHash = File.Exists(hashFile) ? File.ReadAllText(hashFile) : "";
        var currentHash = CalculateMd5HashForSolution();
        if (currentHash == storedHash) return;

        PublishDeveloperCli();

        // Update the hash file to avoid restarting the process again
        File.WriteAllText(hashFile, currentHash);

        // When running in debug mode, we want to avoid restarting the process
        var isDebugBuild = new FileInfo(currentExecutablePath).FullName.Contains("debug");
        if (isDebugBuild) return;

        if (Environment.IsWindows)
        {
            // In Windows we have not found a reliable way to restart the process with the same arguments
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[green]CLI successfully updated. Please rerun the command.[/]");
            AnsiConsole.WriteLine();
            System.Environment.Exit(0);
        }

        // Restart the process with the same arguments
        ProcessHelper.StartProcess(
            currentExecutablePath,
            string.Join(" ", args),
            Environment.SolutionFolder,
            waitForExit: false,
            printCommand: false
        );

        System.Environment.Exit(0);
    }

    private static string CalculateMd5HashForSolution()
    {
        var solutionFiles = Directory.GetFiles(Environment.SolutionFolder, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("artifacts"));

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

        var currentExecutablePath = System.Environment.ProcessPath!;
        var renamedExecutablePath = "";

        try
        {
            // Build project before renaming exe on Windows
            ProcessHelper.StartProcess("dotnet", "build");

            if (Environment.IsWindows)
            {
                // In Windows the executing assembly is locked by the process, blocking overwriting it, but not renaming it
                // We rename the current executable to .previous.exe to unblock publishing of new executable
                renamedExecutablePath = currentExecutablePath.Replace(".exe", ".previous.exe");
                File.Move(currentExecutablePath, renamedExecutablePath, true);
            }

            // Call "dotnet publish" to create a new executable
            ProcessHelper.StartProcess(
                "dotnet",
                "publish",
                Environment.SolutionFolder
            );
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine(
                $"[red]Failed to publish new CLI. Please run 'dotnet run' to fix. Error message: {e.Message}[/]");
            System.Environment.Exit(0);
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