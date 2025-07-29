using System.Security.Cryptography;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class ChangeDetection
{
    internal static void EnsureCliIsCompiledWithLatestChanges(bool isDebugBuild)
    {
        if (Configuration.IsWindows)
        {
            // In Windows, the process is renamed to .previous.exe when updating to unblock publishing of new executable
            // We delete the previous executable the next time the process is started
            var previousExePath = Environment.ProcessPath!.Replace(".exe", ".previous.exe");
            if (File.Exists(previousExePath))
            {
                try
                {
                    // First try simple deletion
                    File.Delete(previousExePath);
                }
                catch (IOException)
                {
                    // If file is locked, throw to handle below with admin privileges
                    throw;
                }
                catch (UnauthorizedAccessException)
                {
                    // Check command line args to determine behavior
                    var args = Environment.GetCommandLineArgs();
                    var isForceCommand = args.Any(arg => arg.Equals("--force", StringComparison.OrdinalIgnoreCase));
                    var isWatchCommand = args.Any(arg => arg.Equals("watch", StringComparison.OrdinalIgnoreCase));
                    var isStopCommand = args.Any(arg => arg.Equals("--stop", StringComparison.OrdinalIgnoreCase));
                    
                    // For watch command without force, or with stop, skip cleanup (watch command will handle it)
                    if (isWatchCommand && (!isForceCommand || isStopCommand))
                    {
                        return;
                    }
                    
                    // In non-interactive mode or with --force, automatically clean up
                    if (!AnsiConsole.Profile.Capabilities.Interactive || isForceCommand)
                    {
                        TryDeletePreviousExe(previousExePath);
                        return;
                    }
                    
                    // In interactive mode, ask the user
                    AnsiConsole.MarkupLine("[yellow]The previous CLI executable is still running.[/]");
                    if (AnsiConsole.Confirm("Do you want to kill the running process and continue?"))
                    {
                        TryDeletePreviousExe(previousExePath);
                    }
                    else
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        var currentHash = CalculateMd5HashForSolution();
        if (currentHash == Configuration.GetConfigurationSetting().Hash) return;

        PublishDeveloperCli(currentHash);

        // When running in debug mode, we want to avoid restarting the process
        if (isDebugBuild) return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]The CLI was successfully updated. Please rerun the command.[/]");
        AnsiConsole.WriteLine();
        Environment.Exit(0);
    }

    private static void TryDeletePreviousExe(string previousExePath)
    {
        try
        {
            // Kill processes that have the file locked
            ProcessHelper.StartProcess("""powershell -Command "Get-Process pp.previous -ErrorAction SilentlyContinue | Stop-Process -Force" """, redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess($$"""powershell -Command "Get-Process | Where-Object {$_.Path -eq '{{previousExePath}}'} | Stop-Process -Force" """, redirectOutput: true, exitOnError: false);
            
            Thread.Sleep(1000);
            
            // Try to delete with retries
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    if (File.Exists(previousExePath))
                    {
                        File.SetAttributes(previousExePath, FileAttributes.Normal);
                        File.Delete(previousExePath);
                    }
                    return;
                }
                catch
                {
                    if (i < 2)
                    {
                        Thread.Sleep(500);
                    }
                }
            }
        }
        catch
        {
            // Ignore - file will be cleaned up next time
        }
    }

    private static string CalculateMd5HashForSolution()
    {
        // Get all files C# and C# project files in the Developer CLI solution
        var solutionFiles = Directory
            .EnumerateFiles(Configuration.CliFolder, "*.cs*", SearchOption.AllDirectories)
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

    private static void PublishDeveloperCli(string currentHash)
    {
        AnsiConsole.MarkupLine("[green]Changes detected, rebuilding and publishing new CLI.[/]");

        var currentExecutablePath = Environment.ProcessPath!;
        var renamedExecutablePath = "";

        try
        {
            // Build the project before renaming exe on Windows
            ProcessHelper.StartProcess("dotnet build", Configuration.CliFolder);

            if (Configuration.IsWindows)
            {
                // In Windows the executing assembly is locked by the process, blocking overwriting it, but not renaming
                // We rename the current executable to .previous.exe to unblock publishing of new executable
                renamedExecutablePath = currentExecutablePath.Replace(".exe", ".previous.exe");
                File.Move(currentExecutablePath, renamedExecutablePath, true);
            }

            // Call "dotnet publish" to create a new executable
            ProcessHelper.StartProcess(
                $"dotnet publish DeveloperCli.csproj -o \"{Configuration.PublishFolder}\"",
                Configuration.CliFolder
            );

            var configurationSetting = Configuration.GetConfigurationSetting();
            configurationSetting.Hash = currentHash;
            Configuration.SaveConfigurationSetting(configurationSetting);
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
