using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class ChangeDetection
{
    internal static void EnsureCliIsCompiledWithLatestChanges(string[] args)
    {
        var processPath = Environment.ProcessPath!;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // In Windows, the process is renamed to .previous.exe when updating to unblock publishing of new executable
            // We delete the previous executable the next time the process is started
            File.Delete(processPath.Replace(".exe", ".previous.exe"));
        }

        var hashFile = Path.Combine(AliasRegistration.PublishFolder, "source-file-hash.md5");
        var storedHash = File.Exists(hashFile) ? File.ReadAllText(hashFile) : "";
        var currentHash = CalculateMd5HashForSolution();
        if (currentHash == storedHash) return;

        AnsiConsole.MarkupLine("[green]Changes detected, rebuilding the CLI.[/]");

        PublishDeveloperCli();

        // Update the hash file to avoid restarting the process again
        File.WriteAllText(hashFile, currentHash);

        // When running in debug mode, we want to disable automatic change detection but still publish the CLI
        var runningDebugBuild = new FileInfo(processPath).FullName.Contains("debug");
        if (runningDebugBuild) return;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AnsiConsole.MarkupLine("[green]CLI successfully updated. Please rerun the command.[/]");
            // Kill the current process 
            Environment.Exit(0);
        }

        // Restart the process with the same arguments
        Process.Start(new ProcessStartInfo
        {
            FileName = processPath,
            Arguments = string.Join(" ", args),
            WorkingDirectory = AliasRegistration.SolutionFolder
        });

        // Kill the current process 
        Environment.Exit(0);
    }

    private static string CalculateMd5HashForSolution()
    {
        var solutionFiles = Directory.GetFiles(AliasRegistration.SolutionFolder, "*", SearchOption.AllDirectories)
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
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var currentExecutablePath = Environment.ProcessPath!;
            var renamedExecutablePath = currentExecutablePath.Replace(".exe", ".previous.exe");

            // Rename the current executable to .previous.exe to unblock publishing of new executable
            File.Move(currentExecutablePath, renamedExecutablePath, true);
        }

        // Call "dotnet publish --configuration RELEASE" to create a new executable
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "publish --configuration RELEASE /p:DebugType=None /p:DebugSymbols=false",
            WorkingDirectory = AliasRegistration.SolutionFolder,
            UseShellExecute = false
        })!;

        process.WaitForExit();
    }
}