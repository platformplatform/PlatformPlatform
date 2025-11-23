using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class CheckCommand : Command
{
    public CheckCommand() : base("check", "Performs all checks including build, test, format, and inspect for backend and frontend code")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Run only backend checks" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Run only frontend checks" };
        var solutionNameOption = new Option<string?>("<solution-name>", "--solution-name", "-s") { Description = "The name of the self-contained system to check (only used for backend checks)" };
        var skipFormatOption = new Option<bool>("--skip-format") { Description = "Skip the backend format step which can be time consuming" };
        var skipInspectOption = new Option<bool>("--skip-inspect") { Description = "Skip the backend inspection step which can be time consuming" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(solutionNameOption);
        Options.Add(skipFormatOption);
        Options.Add(skipInspectOption);

        this.SetAction(parseResult => Execute(
            parseResult.GetValue(backendOption),
            parseResult.GetValue(frontendOption),
            parseResult.GetValue(solutionNameOption),
            parseResult.GetValue(skipFormatOption),
            parseResult.GetValue(skipInspectOption)
        ));
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

        new BuildCommand().Parse([.. solutionArgs, "--backend"]).Invoke();
        new TestCommand().Parse([.. solutionArgs, "--no-build"]).Invoke();

        if (!skipFormat)
        {
            new FormatCommand().Parse([.. solutionArgs, "--backend"]).Invoke();
        }

        if (!skipInspect)
        {
            new InspectCommand().Parse([.. solutionArgs, "--backend", "--no-build"]).Invoke();
        }
    }

    private static void RunFrontendChecks()
    {
        new BuildCommand().Parse(["--frontend"]).Invoke();
        new FormatCommand().Parse(["--frontend"]).Invoke();
        new InspectCommand().Parse(["--frontend"]).Invoke();
    }
}
