using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Builds a self-contained system")
    {
        Handler = CommandHandler.Create<bool, bool, string?>(Execute);

        AddOption(new Option<bool?>(["--backend", "-b"], "Run only backend build"));
        AddOption(new Option<bool?>(["--frontend", "-f"], "Run only frontend build"));
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], "The name of the self-contained system to build (e.g., account-management, back-office)"));

        Handler = CommandHandler.Create<bool, bool, string?>(Execute);
    }

    private static void Execute(bool backend, bool frontend, string? selfContainedSystem)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var buildBackend = backend || !frontend;
        var buildFrontend = frontend || !backend;

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;

            if (buildBackend)
            {
                AnsiConsole.MarkupLine("[blue]Running backend build...[/]");

                if (selfContainedSystem is null)
                {
                    // Build all self-contained systems
                    var systems = SelfContainedSystemHelper.GetAvailableSelfContainedSystems();
                    foreach (var system in systems)
                    {
                        AnsiConsole.MarkupLine($"[dim]Building {system}...[/]");
                        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(system);
                        ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                    }
                }
                else
                {
                    // Build specific system
                    var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);
                    ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                }

                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (buildFrontend)
            {
                AnsiConsole.MarkupLine("[blue]Ensure npm packages are up to date...[/]");
                ProcessHelper.StartProcess("npm install", Configuration.ApplicationFolder);

                AnsiConsole.MarkupLine("\n[blue]Running frontend build...[/]");
                ProcessHelper.StartProcess("npm run build", Configuration.ApplicationFolder);
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            AnsiConsole.MarkupLine($"[green]Build completed successfully in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");
            if (buildBackend && buildFrontend)
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
            AnsiConsole.MarkupLine($"[red]Error during build: {ex.Message}[/]");
            Environment.Exit(1);
        }
    }
}
