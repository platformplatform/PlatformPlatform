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
        AddOption(new Option<string?>(["<solution-name>", "--solution-name", "-s"], "The name of the self-contained system to build (only used for backend builds)"));

        Handler = CommandHandler.Create<bool, bool, string?>(Execute);
    }

    private static void Execute(bool backend, bool frontend, string? solutionName)
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
                var solutionFile = SolutionHelper.GetSolution(solutionName);
                ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (buildFrontend)
            {
                AnsiConsole.MarkupLine("[blue]Running frontend build...[/]");
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
