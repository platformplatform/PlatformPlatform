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
        var skipFormatOption = new Option<bool>("--skip-format") { Description = "Skip the backend format step which can be time consuming" };
        var skipInspectOption = new Option<bool>("--skip-inspect") { Description = "Skip the backend inspection step which can be time consuming" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(skipFormatOption);
        Options.Add(skipInspectOption);
        Options.Add(quietOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(skipFormatOption),
                parseResult.GetValue(skipInspectOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private static void Execute(bool backend, bool frontend, string? selfContainedSystem, bool skipFormat, bool skipInspect, bool quiet)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var checkBackend = backend || !frontend;
        var checkFrontend = frontend || !backend;

        if (quiet)
        {
            ExecuteQuiet(checkBackend, checkFrontend, selfContainedSystem, skipFormat, skipInspect);
            return;
        }

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;

            if (checkBackend)
            {
                RunBackendChecks(selfContainedSystem, skipFormat, skipInspect);
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

    private static void RunBackendChecks(string? selfContainedSystem, bool skipFormat, bool skipInspect)
    {
        string[] systemArgs = selfContainedSystem is not null ? ["--self-contained-system", selfContainedSystem] : [];

        new BuildCommand().Parse([.. systemArgs, "--backend"]).Invoke();
        new TestCommand().Parse([.. systemArgs, "--no-build"]).Invoke();

        if (!skipFormat)
        {
            new FormatCommand().Parse([.. systemArgs, "--backend"]).Invoke();
        }

        if (!skipInspect)
        {
            new InspectCommand().Parse([.. systemArgs, "--backend", "--no-build"]).Invoke();
        }
    }

    private static void RunFrontendChecks()
    {
        new BuildCommand().Parse(["--frontend"]).Invoke();
        new FormatCommand().Parse(["--frontend"]).Invoke();
        new InspectCommand().Parse(["--frontend"]).Invoke();
    }

    private static void ExecuteQuiet(bool checkBackend, bool checkFrontend, string? selfContainedSystem, bool skipFormat, bool skipInspect)
    {
        try
        {
            if (checkBackend)
            {
                var systemArgs = selfContainedSystem is not null ? new[] { "--self-contained-system", selfContainedSystem, "--quiet" } : new[] { "--quiet" };

                // Build
                new BuildCommand().Parse([.. systemArgs, "--backend"]).InvokeAsync().Wait();

                // Test
                new TestCommand().Parse([.. systemArgs, "--no-build"]).InvokeAsync().Wait();

                // Format
                if (!skipFormat)
                {
                    new FormatCommand().Parse([.. systemArgs, "--backend"]).InvokeAsync().Wait();
                }

                // Inspect
                if (!skipInspect)
                {
                    new InspectCommand().Parse([.. systemArgs, "--backend", "--no-build"]).InvokeAsync().Wait();
                }
            }

            if (checkFrontend)
            {
                // Build
                new BuildCommand().Parse(["--frontend", "--quiet"]).InvokeAsync().Wait();

                // Format
                new FormatCommand().Parse(["--frontend", "--quiet"]).InvokeAsync().Wait();

                // Inspect
                new InspectCommand().Parse(["--frontend", "--quiet"]).InvokeAsync().Wait();
            }

            Console.WriteLine("All checks passed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Checks failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
