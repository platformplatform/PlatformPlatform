using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class CheckCommand : Command
{
    public CheckCommand() : base("check", "Performs all checks including build, test, format, and inspect for backend and frontend code")
    {
        AddOption(new Option<bool?>(["--backend", "-b"], "Run only backend checks"));
        AddOption(new Option<bool?>(["--frontend", "-f"], "Run only frontend checks"));
        AddOption(new Option<string?>(["<solution-name>", "--solution-name", "-s"], "The name of the self-contained system to check (only used for backend checks)"));
        AddOption(new Option<bool>(["--skip-format"], () => false, "Skip the backend format step which can be time consuming"));
        AddOption(new Option<bool>(["--skip-inspect"], () => false, "Skip the backend inspection step which can be time consuming"));

        Handler = CommandHandler.Create<bool, bool, string?, bool, bool>(Execute);
    }

    private static void Execute(bool backend, bool frontend, string? solutionName, bool skipFormat, bool skipInspect)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var checkBackend = backend || !frontend;
        var checkFrontend = frontend || !backend;

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;

            if (checkBackend)
            {
                RunBackendChecks(solutionName, skipFormat, skipInspect);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (checkFrontend)
            {
                RunFrontendChecks();
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            AnsiConsole.MarkupLine($"[green]All checks completed successfully in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");
            if (checkBackend && checkFrontend)
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
            AnsiConsole.MarkupLine($"[red]Error during checks: {ex.Message}[/]");
            Environment.Exit(1);
        }
    }

    private static void RunBackendChecks(string? solutionName, bool skipFormat, bool skipInspect)
    {
        string[] solutionArgs = solutionName is not null ? ["--solution-name", solutionName] : [];

        new BuildCommand().InvokeAsync([.. solutionArgs, "--backend"]);
        new TestCommand().InvokeAsync([.. solutionArgs, "--no-build"]);

        if (!skipFormat)
        {
            new FormatCommand().InvokeAsync([.. solutionArgs, "--backend"]);
        }

        if (!skipInspect)
        {
            new InspectCommand().InvokeAsync([.. solutionArgs, "--backend", "--no-build"]);
        }
    }

    private static void RunFrontendChecks()
    {
        new BuildCommand().InvokeAsync(["--frontend"]);
        new FormatCommand().InvokeAsync(["--frontend"]);
        new InspectCommand().InvokeAsync(["--frontend"]);
    }
}
