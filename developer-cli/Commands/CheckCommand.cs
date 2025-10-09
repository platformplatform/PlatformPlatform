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
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], "The name of the self-contained system to check (e.g., account-management, back-office)"));
        AddOption(new Option<bool>(["--skip-format"], () => false, "Skip the backend format step which can be time consuming"));
        AddOption(new Option<bool>(["--skip-inspect"], () => false, "Skip the backend inspection step which can be time consuming"));
        AddOption(new Option<bool>(["--quiet", "-q"], "Minimal output mode"));

        Handler = CommandHandler.Create<bool, bool, string?, bool, bool, bool>(Execute);
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

        new BuildCommand().InvokeAsync([.. systemArgs, "--backend"]);
        new TestCommand().InvokeAsync([.. systemArgs, "--no-build"]);

        if (!skipFormat)
        {
            new FormatCommand().InvokeAsync([.. systemArgs, "--backend"]);
        }

        if (!skipInspect)
        {
            new InspectCommand().InvokeAsync([.. systemArgs, "--backend", "--no-build"]);
        }
    }

    private static void RunFrontendChecks()
    {
        new BuildCommand().InvokeAsync(["--frontend"]);
        new FormatCommand().InvokeAsync(["--frontend"]);
        new InspectCommand().InvokeAsync(["--frontend"]);
    }

    private static void ExecuteQuiet(bool checkBackend, bool checkFrontend, string? selfContainedSystem, bool skipFormat, bool skipInspect)
    {
        try
        {
            if (checkBackend)
            {
                var systemArgs = selfContainedSystem is not null ? new[] { "--self-contained-system", selfContainedSystem, "--quiet" } : new[] { "--quiet" };

                // Build
                new BuildCommand().InvokeAsync([.. systemArgs, "--backend"]).Wait();

                // Test
                new TestCommand().InvokeAsync([.. systemArgs, "--no-build"]).Wait();

                // Format
                if (!skipFormat)
                {
                    new FormatCommand().InvokeAsync([.. systemArgs, "--backend"]).Wait();
                }

                // Inspect
                if (!skipInspect)
                {
                    new InspectCommand().InvokeAsync([.. systemArgs, "--backend", "--no-build"]).Wait();
                }
            }

            if (checkFrontend)
            {
                // Build
                new BuildCommand().InvokeAsync(["--frontend", "--quiet"]).Wait();

                // Format
                new FormatCommand().InvokeAsync(["--frontend", "--quiet"]).Wait();

                // Inspect
                new InspectCommand().InvokeAsync(["--frontend", "--quiet"]).Wait();
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
