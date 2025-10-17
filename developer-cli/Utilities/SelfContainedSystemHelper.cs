using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class SelfContainedSystemHelper
{
    public static string[] GetAvailableSelfContainedSystems()
    {
        return Directory.GetDirectories(Configuration.ApplicationFolder)
            .Where(HasRequiredFolders)
            .Select(Path.GetFileName)
            .Where(name => name is not null)
            .Select(name => name!)
            .OrderBy(name => name)
            .ToArray();
    }

    private static bool HasRequiredFolders(string directory)
    {
        return Directory.Exists(Path.Combine(directory, "Api")) &&
               Directory.Exists(Path.Combine(directory, "Core")) &&
               Directory.Exists(Path.Combine(directory, "Tests")) &&
               Directory.Exists(Path.Combine(directory, "WebApp"));
    }

    public static string PromptForSelfContainedSystem(string[] availableSystems)
    {
        if (!AnsiConsole.Profile.Capabilities.Interactive)
        {
            AnsiConsole.MarkupLine("[red]Cannot show selection prompt in non-interactive terminal.[/]");
            AnsiConsole.MarkupLine("[yellow]Please specify a self-contained system using the -s flag.[/]");
            AnsiConsole.MarkupLine($"[yellow]Available systems: {string.Join(", ", availableSystems)}[/]");
            Environment.Exit(1);
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]self-contained system[/] to test:")
                .AddChoices(availableSystems)
        );
    }

    public static FileInfo GetSolutionFile(string? selfContainedSystem)
    {
        if (selfContainedSystem is null)
        {
            // When no system is specified, use the root solution for faster single-pass processing
            var slnxFiles = Directory.GetFiles(Configuration.ApplicationFolder, "*.slnx");

            if (slnxFiles.Length == 0)
            {
                AnsiConsole.MarkupLine("[red]No root solution file (.slnx) found in application/[/]");
                AnsiConsole.MarkupLine("[yellow]Please ensure a .slnx file exists in the application folder, or specify a self-contained system using the -s flag.[/]");
                AnsiConsole.MarkupLine($"[yellow]Available systems: {string.Join(", ", GetAvailableSelfContainedSystems())}[/]");
                Environment.Exit(1);
            }

            if (slnxFiles.Length > 1)
            {
                var fileNames = slnxFiles.Select(Path.GetFileName).ToArray();
                AnsiConsole.MarkupLine("[red]Multiple root solution files (.slnx) found in application/[/]");
                AnsiConsole.MarkupLine($"[yellow]Found: {string.Join(", ", fileNames)}[/]");
                AnsiConsole.MarkupLine("[yellow]Please ensure only one .slnx file exists in the application folder.[/]");
                Environment.Exit(1);
            }

            return new FileInfo(slnxFiles[0]);
        }

        var scsFolder = Path.Combine(Configuration.ApplicationFolder, selfContainedSystem);
        if (!Directory.Exists(scsFolder))
        {
            AnsiConsole.MarkupLine($"[red]Self-contained system '{selfContainedSystem}' not found in application/[/]");
            AnsiConsole.MarkupLine($"[yellow]Available systems: {string.Join(", ", GetAvailableSelfContainedSystems())}[/]");
            Environment.Exit(1);
        }

        var slnfFiles = Directory.GetFiles(scsFolder, "*.slnf");
        if (slnfFiles.Length == 0)
        {
            AnsiConsole.MarkupLine($"[red]No .slnf file found in application/{selfContainedSystem}/[/]");
            Environment.Exit(1);
        }

        if (slnfFiles.Length > 1)
        {
            AnsiConsole.MarkupLine($"[red]Multiple .slnf files found in application/{selfContainedSystem}/[/]");
            Environment.Exit(1);
        }

        return new FileInfo(slnfFiles[0]);
    }
}
