using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class InspectCommand : Command
{
    public InspectCommand() : base("inspect", "Run code inspections for frontend and backend code")
    {
        AddOption(new Option<bool?>(["--backend", "-b"], "Run only backend inspections"));
        AddOption(new Option<bool?>(["--frontend", "-f"], "Run only frontend inspections"));
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], "The name of the self-contained system to inspect (e.g., account-management, back-office)"));
        AddOption(new Option<bool>(["--no-build"], () => false, "Skip building and restoring the solution before running inspections"));
        AddOption(new Option<bool>(["--quiet", "-q"], "Minimal output mode"));

        Handler = CommandHandler.Create<bool, bool, string?, bool, bool>(Execute);
    }

    private static void Execute(bool backend, bool frontend, string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var inspectBackend = backend || !frontend;
        var inspectFrontend = frontend || !backend;

        if (quiet)
        {
            ExecuteQuiet(inspectBackend, inspectFrontend, selfContainedSystem, noBuild);
            return;
        }

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;

            if (inspectBackend)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                RunBackendInspections(selfContainedSystem, noBuild);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (inspectFrontend)
            {
                Prerequisite.Ensure(Prerequisite.Node);
                RunFrontendInspections();
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            AnsiConsole.MarkupLine($"[green]Code inspections completed in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");
            if (inspectBackend && inspectFrontend)
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
            AnsiConsole.MarkupLine($"[red]Error during code inspections: {ex.Message}[/]");
            Environment.Exit(1);
        }
    }

    private static void RunBackendInspections(string? selfContainedSystem, bool noBuild)
    {
        AnsiConsole.MarkupLine("[blue]Running backend code inspections...[/]");
        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

        if (!noBuild)
        {
            ProcessHelper.StartProcess("dotnet tool restore", solutionFile.Directory!.FullName);
            ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory!.FullName);
        }

        ProcessHelper.StartProcess(
            $"dotnet jb inspectcode {solutionFile.Name} --no-build --no-restore --output=result.json --severity=SUGGESTION",
            solutionFile.Directory!.FullName
        );

        var resultJson = File.ReadAllText(Path.Combine(solutionFile.Directory!.FullName, "result.json"));
        if (resultJson.Contains("\"results\": [],"))
        {
            AnsiConsole.MarkupLine("[green]No backend issues found![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Backend issues found. Opening result.json...[/]");
            ProcessHelper.StartProcess("code result.json", solutionFile.Directory!.FullName);
        }
    }

    private static void RunFrontendInspections()
    {
        AnsiConsole.MarkupLine("[blue]Running frontend type checking...[/]");
        ProcessHelper.StartProcess("npm run check", Configuration.ApplicationFolder);
    }

    private static void ExecuteQuiet(bool inspectBackend, bool inspectFrontend, string? selfContainedSystem, bool noBuild)
    {
        try
        {
            var hasIssues = false;

            if (inspectBackend)
            {
                var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

                if (!noBuild)
                {
                    var restoreResult = ProcessHelper.ExecuteQuietly("dotnet tool restore", solutionFile.Directory!.FullName);
                    if (!restoreResult.Success)
                    {
                        Console.WriteLine(restoreResult.GetErrorSummary("Tool restore"));
                        Environment.Exit(1);
                    }

                    var buildResult = ProcessHelper.ExecuteQuietly($"dotnet build {solutionFile.Name}", solutionFile.Directory!.FullName);
                    if (!buildResult.Success)
                    {
                        Console.WriteLine(buildResult.GetErrorSummary("Build"));
                        Environment.Exit(1);
                    }
                }

                var inspectResult = ProcessHelper.ExecuteQuietly(
                    $"dotnet jb inspectcode {solutionFile.Name} --no-build --no-restore --output=result.json --severity=SUGGESTION",
                    solutionFile.Directory!.FullName
                );

                if (!inspectResult.Success)
                {
                    Console.WriteLine(inspectResult.GetErrorSummary("Inspections"));
                    Environment.Exit(1);
                }

                var resultJson = File.ReadAllText(Path.Combine(solutionFile.Directory!.FullName, "result.json"));
                hasIssues = !resultJson.Contains("\"results\": [],");
            }

            if (inspectFrontend)
            {
                var result = ProcessHelper.ExecuteQuietly("npm run check", Configuration.ApplicationFolder);
                if (!result.Success)
                {
                    Console.WriteLine(result.GetErrorSummary("Frontend type checking"));
                    Environment.Exit(1);
                }
            }

            if (hasIssues)
            {
                Console.WriteLine("Issues found. Check result.json in the project directories.");
                Environment.Exit(1);
            }

            Console.WriteLine("Inspections completed successfully. No issues found.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Inspections failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
