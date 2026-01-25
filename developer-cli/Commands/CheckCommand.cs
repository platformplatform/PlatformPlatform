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
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Run backend checks" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Run frontend checks" };
        var cliOption = new Option<bool>("--cli", "-c") { Description = "Run developer-cli checks" };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to check (e.g., account, back-office)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring before running checks" };
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

    private static void Execute(bool backend, bool frontend, bool cli, string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var noFlags = !backend && !frontend && !cli;
        var checkBackend = backend || noFlags;
        var checkFrontend = frontend || noFlags;
        var checkCli = cli || noFlags;

        // Ensure prerequisites based on what we're checking
        if (checkBackend || checkCli) Prerequisite.Ensure(Prerequisite.Dotnet);
        if (checkFrontend) Prerequisite.Ensure(Prerequisite.Node);

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;
            var cliTime = TimeSpan.Zero;

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

            if (checkCli)
            {
                RunCliChecks(noBuild, quiet);
                cliTime = Stopwatch.GetElapsedTime(startTime) - backendTime - frontendTime;
            }

            if (quiet)
            {
                Console.WriteLine("All checks passed.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]All checks completed successfully in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");

                var multipleTargets = (checkBackend ? 1 : 0) + (checkFrontend ? 1 : 0) + (checkCli ? 1 : 0) > 1;
                if (multipleTargets)
                {
                    var timingLines = new List<string>();
                    if (checkBackend) timingLines.Add($"Backend:       [green]{backendTime.Format()}[/]");
                    if (checkFrontend) timingLines.Add($"Frontend:      [green]{frontendTime.Format()}[/]");
                    if (checkCli) timingLines.Add($"Developer CLI: [green]{cliTime.Format()}[/]");
                    AnsiConsole.MarkupLine(string.Join(Environment.NewLine, timingLines));
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

    private static void RunCliChecks(bool noBuild, bool quiet)
    {
        string[] args = quiet ? ["--quiet"] : [];
        string[] formatArgs = noBuild ? ["--no-build"] : [];

        if (!noBuild)
        {
            new BuildCommand().Parse([.. args, "--cli"]).Invoke();
        }

        new FormatCommand().Parse([.. args, "--cli", .. formatArgs]).Invoke();
        new InspectCommand().Parse([.. args, "--cli", "--no-build"]).Invoke();
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
