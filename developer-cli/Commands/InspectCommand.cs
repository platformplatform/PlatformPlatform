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

        Handler = CommandHandler.Create<bool, bool, string?, bool>(Execute);
    }

    private static void Execute(bool backend, bool frontend, string? selfContainedSystem, bool noBuild)
    {
        var inspectBackend = backend || !frontend;
        var inspectFrontend = frontend || !backend;

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

        if (selfContainedSystem is null)
        {
            // Inspect all self-contained systems
            var systems = SelfContainedSystemHelper.GetAvailableSelfContainedSystems();
            var allIssuesFound = false;

            foreach (var system in systems)
            {
                AnsiConsole.MarkupLine($"[dim]Inspecting {system}...[/]");
                var hasIssues = InspectSystem(system, noBuild);
                if (hasIssues) allIssuesFound = true;
            }

            if (allIssuesFound)
            {
                Environment.Exit(1);
            }
        }
        else
        {
            // Inspect specific system
            var hasIssues = InspectSystem(selfContainedSystem, noBuild);
            if (hasIssues)
            {
                Environment.Exit(1);
            }
        }
    }

    private static bool InspectSystem(string selfContainedSystem, bool noBuild)
    {
        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

        ProcessHelper.StartProcess("dotnet tool restore", solutionFile.Directory!.FullName);

        if (!noBuild)
        {
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
            return false;
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Backend issues found. Opening result.json...[/]");
            ProcessHelper.StartProcess("code result.json", solutionFile.Directory!.FullName);
            return true;
        }
    }

    private static void RunFrontendInspections()
    {
        AnsiConsole.MarkupLine("[blue]Running frontend type checking...[/]");
        ProcessHelper.StartProcess("npm run check", Configuration.ApplicationFolder);
    }
}
