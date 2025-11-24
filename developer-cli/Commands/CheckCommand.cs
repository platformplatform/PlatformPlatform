using System.CommandLine;
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
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to check (e.g., account-management, back-office)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring before running checks" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(noBuildOption);
        Options.Add(quietOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(noBuildOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private static void Execute(bool backend, bool frontend, string? selfContainedSystem, bool noBuild, bool quiet)
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
                RunBackendChecks(selfContainedSystem, noBuild, quiet);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (checkFrontend)
            {
                RunFrontendChecks(noBuild, quiet);
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            if (quiet)
            {
                Console.WriteLine("All checks passed.");
            }
            else
            {
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
        }
        catch (Exception ex)
        {
            if (quiet)
            {
                Console.WriteLine($"Checks failed: {ex.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error during checks: {ex.Message}[/]");
            }

            Environment.Exit(1);
        }
    }

    private static void RunBackendChecks(string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var systemArgs = BuildArgs(selfContainedSystem, quiet);

        if (!noBuild)
        {
            new BuildCommand().Parse([.. systemArgs, "--backend"]).Invoke();
        }

        new TestCommand().Parse([.. systemArgs, "--no-build"]).Invoke();

        string[] formatArgs = noBuild ? ["--no-build"] : [];
        new FormatCommand().Parse([.. systemArgs, "--backend", .. formatArgs]).Invoke();

        new InspectCommand().Parse([.. systemArgs, "--backend", "--no-build"]).Invoke();
    }

    private static void RunFrontendChecks(bool noBuild, bool quiet)
    {
        string[] args = quiet ? ["--quiet"] : [];

        if (!noBuild)
        {
            new BuildCommand().Parse([.. args, "--frontend"]).Invoke();
        }

        new FormatCommand().Parse([.. args, "--frontend"]).Invoke();
        new InspectCommand().Parse([.. args, "--frontend"]).Invoke();
    }

    private static string[] BuildArgs(string? selfContainedSystem, bool quiet)
    {
        if (selfContainedSystem is not null && quiet)
        {
            return ["--self-contained-system", selfContainedSystem, "--quiet"];
        }

        if (selfContainedSystem is not null)
        {
            return ["--self-contained-system", selfContainedSystem];
        }

        if (quiet)
        {
            return ["--quiet"];
        }

        return [];
    }
}
