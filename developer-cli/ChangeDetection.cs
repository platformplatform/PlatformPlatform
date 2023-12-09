using System.Diagnostics;
using System.Security.Cryptography;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli;

public static class ChangeDetection
{
    private static readonly string SolutionFolder =
        new DirectoryInfo(Environment.ProcessPath!).Parent!.Parent!.Parent!.Parent!.Parent!.FullName;

    private static readonly string PublishFolder =
        Path.Combine(SolutionFolder, "artifacts", "publish", "DeveloperCli", "release");

    internal static void EnsureCliIsCompiledWithLatestChanges(string[] args)
    {
        var runningDebugBuild = new FileInfo(Environment.ProcessPath!).FullName.Contains("/debug/");

        var hashFile = Path.Combine(PublishFolder, "source-file-hash.md5");
        var storedHash = File.Exists(hashFile) ? File.ReadAllText(hashFile) : "";
        var currentHash = CalculateMd5HashForSolution();
        if (currentHash == storedHash) return;

        if (!runningDebugBuild)
        {
            AnsiConsole.MarkupLine("[green]Changes detected, rebuilding the CLI.[/]");
        }

        PublishDeveloperCli();

        // Update the hash file to avoid restarting the process again
        File.WriteAllText(hashFile, currentHash);

        if (runningDebugBuild) return;

        // Restart the process with the same arguments
        Process.Start(new ProcessStartInfo
        {
            FileName = Environment.ProcessPath,
            Arguments = string.Join(" ", args),
            WorkingDirectory = SolutionFolder
        });

        // Kill the current process 
        Environment.Exit(0);
    }

    private static string CalculateMd5HashForSolution()
    {
        var solutionFiles = Directory.GetFiles(SolutionFolder, "*", SearchOption.AllDirectories)
            .Where(f => !f.Contains("/artifacts/"));

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
        // Call "dotnet publish --configuration RELEASE" to create a new executable
        Process.Start(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "publish --configuration RELEASE",
            WorkingDirectory = SolutionFolder,
            RedirectStandardOutput = true
        })!.WaitForExit();
    }
}