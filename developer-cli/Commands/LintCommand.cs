using System.CommandLine;
using System.Diagnostics;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class LintCommand : Command
{
    public LintCommand() : base("lint", "Run code linting for frontend and backend code")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Run backend linting" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Run frontend linting" };
        var cliOption = new Option<bool>("--cli", "-c") { Description = "Run developer-cli linting" };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to lint (e.g., main, account, back-office)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring the solution before running linting" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(cliOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(noBuildOption);
        Options.Add(quietOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(cliOption),
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(noBuildOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private static void Execute(bool backend, bool frontend, bool developerCli, string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var noFlags = !backend && !frontend && !developerCli;
        var lintBackend = backend || noFlags;
        var lintFrontend = frontend || noFlags;
        var lintDeveloperCli = developerCli || noFlags;

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;
            var developerCliTime = TimeSpan.Zero;
            var hasIssues = false;

            if (lintBackend)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                hasIssues = RunBackendLinting(selfContainedSystem, noBuild, quiet);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (lintFrontend)
            {
                Prerequisite.Ensure(Prerequisite.Node);
                RunFrontendLinting(quiet);
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            if (lintDeveloperCli)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                var developerCliHasIssues = RunDeveloperCliLinting(noBuild, quiet);
                hasIssues = hasIssues || developerCliHasIssues;
                developerCliTime = Stopwatch.GetElapsedTime(startTime) - backendTime - frontendTime;
            }

            if (quiet)
            {
                if (hasIssues)
                {
                    Console.WriteLine("Issues found. Check result.json in the project directories.");
                    Environment.Exit(1);
                }

                Console.WriteLine("Linting completed successfully. No issues found.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Code linting completed in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");

                var multipleTargets = (lintBackend ? 1 : 0) + (lintFrontend ? 1 : 0) + (lintDeveloperCli ? 1 : 0) > 1;
                if (multipleTargets)
                {
                    var timingLines = new List<string>();
                    if (lintBackend) timingLines.Add($"Backend:       [green]{backendTime.Format()}[/]");
                    if (lintFrontend) timingLines.Add($"Frontend:      [green]{frontendTime.Format()}[/]");
                    if (lintDeveloperCli) timingLines.Add($"Developer CLI: [green]{developerCliTime.Format()}[/]");
                    AnsiConsole.MarkupLine(string.Join(Environment.NewLine, timingLines));
                }

                if (hasIssues)
                {
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            if (quiet)
            {
                Console.WriteLine($"Linting failed: {ex.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error during code linting: {ex.Message}[/]");
            }

            Environment.Exit(1);
        }
    }

    private static bool RunBackendLinting(string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

        if (!noBuild)
        {
            if (!quiet) AnsiConsole.MarkupLine("[blue]Running backend code linting...[/]");
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
            ProcessHelper.Run($"dotnet build {solutionFile.Name}", solutionFile.Directory!.FullName, "Build", quiet);
        }

        // Delete existing result.json to prevent reading stale results
        var resultJsonPath = Path.Combine(solutionFile.Directory!.FullName, "result.json");
        if (File.Exists(resultJsonPath))
        {
            File.Delete(resultJsonPath);
        }

        ProcessHelper.Run(
            $"dotnet jb inspectcode {solutionFile.Name} --no-build --no-restore --output=result.json --severity=SUGGESTION",
            solutionFile.Directory!.FullName,
            "Linting",
            quiet
        );

        var resultJson = File.ReadAllText(Path.Combine(solutionFile.Directory!.FullName, "result.json"));
        var hasIssues = !resultJson.Contains("\"results\": [],");

        if (!quiet)
        {
            if (hasIssues)
            {
                AnsiConsole.MarkupLine("[yellow]Backend issues found. Opening result.json...[/]");
                ProcessHelper.StartProcess("code result.json", solutionFile.Directory!.FullName);
            }
            else
            {
                AnsiConsole.MarkupLine("[green]No backend issues found![/]");
            }
        }

        return hasIssues;
    }

    private static void RunFrontendLinting(bool quiet)
    {
        if (!quiet) AnsiConsole.MarkupLine("[blue]Running frontend linting...[/]");
        ProcessHelper.Run("npm run lint", Configuration.ApplicationFolder, "Frontend linting", quiet);
    }

    private static bool RunDeveloperCliLinting(bool noBuild, bool quiet)
    {
        var solutionFile = new FileInfo(Path.Combine(Configuration.CliFolder, "DeveloperCli.slnx"));

        if (!noBuild)
        {
            if (!quiet) AnsiConsole.MarkupLine("[blue]Running developer-cli code linting...[/]");
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
            ProcessHelper.Run($"dotnet build {solutionFile.Name}", solutionFile.Directory!.FullName, "Build", quiet);
        }

        // Delete existing result.json to prevent reading stale results
        var resultJsonPath = Path.Combine(solutionFile.Directory!.FullName, "result.json");
        if (File.Exists(resultJsonPath))
        {
            File.Delete(resultJsonPath);
        }

        ProcessHelper.Run(
            $"dotnet jb inspectcode {solutionFile.Name} --no-build --no-restore --output=result.json --severity=SUGGESTION",
            solutionFile.Directory!.FullName,
            "Linting",
            quiet
        );

        var resultJson = File.ReadAllText(Path.Combine(solutionFile.Directory!.FullName, "result.json"));
        var hasIssues = !resultJson.Contains("\"results\": [],");

        if (!quiet)
        {
            if (hasIssues)
            {
                AnsiConsole.MarkupLine("[yellow]Developer-cli issues found. Opening result.json...[/]");
                ProcessHelper.StartProcess("code result.json", solutionFile.Directory!.FullName);
            }
            else
            {
                AnsiConsole.MarkupLine("[green]No developer-cli issues found![/]");
            }
        }

        return hasIssues;
    }
}
