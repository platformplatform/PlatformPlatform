using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Builds a self-contained system")
    {
        AddOption(new Option<bool?>(["--backend", "-b"], "Run only backend build"));
        AddOption(new Option<bool?>(["--frontend", "-f"], "Run only frontend build"));
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], "The name of the self-contained system to build (e.g., account-management, back-office)"));
        AddOption(new Option<bool>(["--quiet", "-q"], "Minimal output mode"));

        Handler = CommandHandler.Create<bool, bool, string?, bool>(Execute);
    }

    private static void Execute(bool backend, bool frontend, string? selfContainedSystem, bool quiet)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var buildBackend = backend || !frontend;
        var buildFrontend = frontend || !backend;

        if (quiet)
        {
            ExecuteQuiet(buildBackend, buildFrontend, selfContainedSystem);
        }
        else
        {
            ExecuteVerbose(buildBackend, buildFrontend, selfContainedSystem);
        }
    }

    private static void ExecuteVerbose(bool buildBackend, bool buildFrontend, string? selfContainedSystem)
    {
        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;

            if (buildBackend)
            {
                AnsiConsole.MarkupLine("[blue]Running backend build...[/]");
                var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);
                ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (buildFrontend)
            {
                AnsiConsole.MarkupLine("[blue]Ensure npm packages are up to date...[/]");
                ProcessHelper.StartProcess("npm install", Configuration.ApplicationFolder);

                AnsiConsole.MarkupLine("\n[blue]Running frontend build...[/]");
                ProcessHelper.StartProcess("npm run build", Configuration.ApplicationFolder);
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            AnsiConsole.MarkupLine($"[green]Build completed successfully in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");
            if (buildBackend && buildFrontend)
            {
                AnsiConsole.MarkupLine(
                    $"""
                     Backend:     [green]{backendTime.Format()}[/]
                     Frontend:    [green]{frontendTime.Format()}[/]
                     """
                );
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error during build: {ex.Message}[/]");
            Environment.Exit(1);
        }
    }

    private static void ExecuteQuiet(bool buildBackend, bool buildFrontend, string? selfContainedSystem)
    {
        try
        {
            if (buildBackend)
            {
                var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);
                var result = ProcessHelper.ExecuteQuietly($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);

                if (!result.Success)
                {
                    Console.WriteLine($"Build failed. See: {result.TempFilePathWithSize}");
                    Environment.Exit(1);
                }
            }

            if (buildFrontend)
            {
                var installResult = ProcessHelper.ExecuteQuietly("npm install --silent", Configuration.ApplicationFolder);
                if (!installResult.Success)
                {
                    Console.WriteLine($"npm install failed. See: {installResult.TempFilePathWithSize}");
                    Environment.Exit(1);
                }

                var buildResult = ProcessHelper.ExecuteQuietly("npm run build", Configuration.ApplicationFolder);
                if (!buildResult.Success)
                {
                    var errors = ExtractFrontendErrors(buildResult.CombinedOutput);
                    Console.WriteLine("Frontend build failed.");
                    Console.WriteLine();
                    Console.WriteLine($"Errors ({errors.Count}):");
                    foreach (var error in errors.Take(3))
                    {
                        Console.WriteLine($"  {error}");
                    }

                    if (errors.Count > 3)
                    {
                        Console.WriteLine($"  ... and {errors.Count - 3} more error(s)");
                    }

                    Console.WriteLine();
                    Console.WriteLine($"Full output: {buildResult.TempFilePathWithSize}");
                    Environment.Exit(1);
                }
            }

            // Success - show minimal output
            Console.WriteLine("Build succeeded.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Build failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static List<string> ExtractFrontendErrors(string output)
    {
        var errors = new List<string>();
        var lines = output.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();

            // Look for TypeScript error patterns like "Error: TS2551:" or "× Error:"
            if (line.Contains("Error: TS") || line.Contains("× Error:"))
            {
                // Extract file and error message
                var errorMessage = line;

                // Try to get the file path from previous lines
                for (var j = i - 1; j >= Math.Max(0, i - 3); j--)
                {
                    if (lines[j].Contains("File:") || lines[j].Contains(".tsx:") || lines[j].Contains(".ts:"))
                    {
                        var fileLine = lines[j].Trim();
                        errorMessage = $"{fileLine} - {errorMessage}";
                        break;
                    }
                }

                errors.Add(errorMessage.Replace("[0m", "").Replace("[31m", "").Replace("[39m", ""));
            }
        }

        // If no TypeScript errors found, look for generic error messages
        if (errors.Count == 0)
        {
            foreach (var line in lines)
            {
                if (line.Contains("error") && !line.Contains("npm error") && line.Length < 200)
                {
                    errors.Add(line.Trim());
                }
            }
        }

        return errors.Count > 0 ? errors : ["Build failed. See full output for details."];
    }
}
