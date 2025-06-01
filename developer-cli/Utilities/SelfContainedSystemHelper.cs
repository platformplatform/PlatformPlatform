using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class SelfContainedSystemHelper
{
    public static string[] GetAvailableSelfContainedSystems()
    {
        return Directory.GetDirectories(Configuration.ApplicationFolder)
            .Where(dir => HasRequiredFolders(dir))
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
}
