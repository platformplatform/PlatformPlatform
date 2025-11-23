using System.CommandLine;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class InspectCommand : Command
{
    public InspectCommand() : base("inspect", "Run code inspections for frontend and backend code")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Run only backend inspections" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Run only frontend inspections" };
        var solutionNameOption = new Option<string?>("<solution-name>", "--solution-name", "-s") { Description = "The name of the self-contained system to inspect (only used for backend inspections)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring the solution before running inspections" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(solutionNameOption);
        Options.Add(noBuildOption);

        this.SetAction(parseResult => Execute(
            parseResult.GetValue(backendOption),
            parseResult.GetValue(frontendOption),
            parseResult.GetValue(solutionNameOption),
            parseResult.GetValue(noBuildOption)
        ));
    }

    private static void Execute(bool backend, bool frontend, string? solutionName, bool noBuild)
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
                RunBackendInspections(solutionName, noBuild);
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

    private static void RunBackendInspections(string? solutionName, bool noBuild)
    {
        AnsiConsole.MarkupLine("[blue]Running backend code inspections...[/]");
        var solutionFile = SolutionHelper.GetSolution(solutionName);

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
}
