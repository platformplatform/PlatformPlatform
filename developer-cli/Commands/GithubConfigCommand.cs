using System.CommandLine;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class GithubConfigCommand : Command
{
    private static readonly Dictionary<string, GithubConfig> Configurations = new()
    {
        ["GOOGLE_OAUTH_CLIENT_ID"] = new GithubConfig(
            "Google OAuth Client ID from Google Cloud Console",
            GithubScope.Environment,
            GithubType.Variable,
            "123456789012-abcdefghijklmnopqrstuvwxyz012345.apps.googleusercontent.com",
            "Google OAuth"
        ),
        ["GOOGLE_OAUTH_CLIENT_SECRET"] = new GithubConfig(
            "Google OAuth Client Secret from Google Cloud Console",
            GithubScope.Environment,
            GithubType.Secret,
            "GOCSPX-xxxxxxxxxxxxxxxxxxxxxxxxxx",
            "Google OAuth"
        )
    };

    public GithubConfigCommand() : base("github-config", "Configure GitHub repository variables and secrets for external integrations like Google OAuth")
    {
        SetAction(_ => Execute());
    }

    private static void Execute()
    {
        Prerequisite.Ensure(Prerequisite.GithubCli);

        var githubUri = GithubHelper.GetGithubUri();
        var githubInfo = GithubHelper.GetGithubInfo(githubUri);

        if (!IsLoggedInToGitHub())
        {
            AnsiConsole.MarkupLine("[yellow]You need to be logged in to GitHub CLI.[/]");
            ProcessHelper.StartProcess("gh auth login --git-protocol https --web");

            if (!IsLoggedInToGitHub())
            {
                AnsiConsole.MarkupLine("[red]Failed to log in to GitHub. Please try again.[/]");
                Environment.Exit(1);
            }
        }

        AnsiConsole.MarkupLine($"[blue]GitHub Configuration for {githubInfo.Path}[/]");
        AnsiConsole.WriteLine();

        var groups = Configurations.GroupBy(c => c.Value.Group).ToList();

        var groupChoices = groups.Select(g => g.Key).Concat(["Exit"]).ToArray();
        var selectedGroup = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select configuration group:")
                .AddChoices(groupChoices)
        );

        if (selectedGroup == "Exit")
        {
            return;
        }

        AnsiConsole.WriteLine();

        var configurationsInGroup = Configurations.Where(c => c.Value.Group == selectedGroup).ToList();
        var pendingChanges = new List<PendingChange>();

        foreach (var (name, config) in configurationsInGroup)
        {
            AnsiConsole.MarkupLine($"[bold]{name}[/]");
            AnsiConsole.MarkupLine($"[dim]Type:[/] {config.GithubType}");
            AnsiConsole.MarkupLine($"[dim]Scope:[/] {config.GithubScope}");
            AnsiConsole.MarkupLine($"[dim]Description:[/] {config.Description}");
            AnsiConsole.MarkupLine($"[dim]Example:[/] [blue]{config.ExampleValue}[/]");
            AnsiConsole.WriteLine();

            if (!AnsiConsole.Confirm($"Configure {name}?"))
            {
                AnsiConsole.WriteLine();
                continue;
            }

            string? environment = null;
            if (config.GithubScope == GithubScope.Environment)
            {
                var scopeChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Select scope:")
                        .AddChoices("Staging environment", "Production environment", "Both environments", "Repository level")
                );

                environment = scopeChoice switch
                {
                    "Staging environment" => "staging",
                    "Production environment" => "production",
                    "Both environments" => "both",
                    _ => null
                };
            }

            var valuePrompt = config.GithubType == GithubType.Secret
                ? new TextPrompt<string>($"Enter value for {name}:").Secret()
                : new TextPrompt<string>($"Enter value for {name}:");

            var value = AnsiConsole.Prompt(valuePrompt);

            if (string.IsNullOrWhiteSpace(value))
            {
                AnsiConsole.MarkupLine($"[yellow]Skipping {name} (empty value).[/]");
                AnsiConsole.WriteLine();
                continue;
            }

            pendingChanges.Add(new PendingChange(name, config.GithubType, environment, value));
            AnsiConsole.WriteLine();
        }

        if (pendingChanges.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]No changes to apply.[/]");
            return;
        }

        PrintPendingChanges(pendingChanges);

        if (!AnsiConsole.Confirm("Apply these changes?"))
        {
            AnsiConsole.MarkupLine("[dim]Changes cancelled.[/]");
            return;
        }

        AnsiConsole.WriteLine();
        ApplyChanges(githubInfo, pendingChanges);
        PrintSummary(githubInfo);
    }

    private static bool IsLoggedInToGitHub()
    {
        var result = ProcessHelper.StartProcess("gh auth status", redirectOutput: true, exitOnError: false);
        return result.Contains("Logged in to github.com");
    }

    private static void PrintPendingChanges(List<PendingChange> pendingChanges)
    {
        AnsiConsole.MarkupLine("[bold]Pending changes:[/]");
        AnsiConsole.WriteLine();

        foreach (var change in pendingChanges)
        {
            var typeLabel = change.Type == GithubType.Variable ? "Variable" : "Secret";
            var scopeLabel = change.Environment switch
            {
                "staging" => "staging environment",
                "production" => "production environment",
                "both" => "both environments",
                _ => "repository level"
            };
            var valueDisplay = change.Type == GithubType.Secret ? "[dim](hidden)[/]" : $"[blue]{change.Value}[/]";

            AnsiConsole.MarkupLine($"  [green]>[/] {change.Name} ({typeLabel}, {scopeLabel}): {valueDisplay}");
        }

        AnsiConsole.WriteLine();
    }

    private static void ApplyChanges(GithubInfo githubInfo, List<PendingChange> pendingChanges)
    {
        foreach (var change in pendingChanges)
        {
            var environments = change.Environment switch
            {
                "both" => new[] { "staging", "production" },
                null => new string?[] { null },
                _ => new[] { change.Environment }
            };

            foreach (var env in environments)
            {
                if (change.Type == GithubType.Variable)
                {
                    SetGithubVariable(githubInfo, change.Name, change.Value, env);
                }
                else
                {
                    SetGithubSecret(githubInfo, change.Name, change.Value, env);
                }

                var scopeLabel = env is not null ? $" ({env})" : "";
                AnsiConsole.MarkupLine($"[green]Set {change.Name}{scopeLabel}[/]");
            }
        }

        AnsiConsole.WriteLine();
    }

    private static void SetGithubVariable(GithubInfo githubInfo, string name, string value, string? environment)
    {
        var envFlag = environment is not null ? $" --env {environment}" : "";
        ProcessHelper.StartProcess($"gh variable set {name} -b\"{value}\" --repo={githubInfo.Path}{envFlag}");
    }

    private static void SetGithubSecret(GithubInfo githubInfo, string name, string value, string? environment)
    {
        var envFlag = environment is not null ? $" --env {environment}" : "";
        ProcessHelper.StartProcess($"gh secret set {name} -b\"{value}\" --repo={githubInfo.Path}{envFlag}");
    }

    private static void PrintSummary(GithubInfo githubInfo)
    {
        AnsiConsole.MarkupLine("[green]GitHub configuration complete![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[yellow]Tip:[/] View your configuration at [blue]{githubInfo.Url}/settings/secrets/actions[/]");
    }

    private record GithubConfig(string Description, GithubScope GithubScope, GithubType GithubType, string ExampleValue, string Group);

    private record PendingChange(string Name, GithubType Type, string? Environment, string Value);

    private enum GithubScope
    {
        Environment
    }

    private enum GithubType
    {
        Variable,
        Secret
    }
}
