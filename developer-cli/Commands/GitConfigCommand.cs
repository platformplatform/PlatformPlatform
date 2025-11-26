using System.CommandLine;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class GitConfigCommand : Command
{
    private static readonly (string Setting, string Value, string Description)[] RecommendedSettings =
    [
        ("push.default", "current", "Always push only the current branch (avoids accidentally pushing other branches)"),
        ("push.autoSetupRemote", "true", "Automatically set up remote tracking on first push (no need for 'git push -u')"),
        ("push.useForceIfIncludes", "true", "Block force push if your local branch is missing commits from remote (prevents overwriting others' work)"),
        ("rerere.enabled", "true", "Remember how you resolved merge conflicts and auto-apply the same resolution next time"),
        ("rerere.autoupdate", "true", "Automatically stage files that were auto-resolved by rerere"),
        ("fetch.prune", "true", "Automatically remove local references to deleted remote branches when fetching")
    ];

    public GitConfigCommand() : base("git-config", "Configure Git author identity and recommended settings for safer pushes, conflict resolution, and branch management")
    {
        SetAction(_ => Execute());
    }

    private static void Execute()
    {
        var repositoryName = new DirectoryInfo(Configuration.SourceCodeFolder).Name;

        AnsiConsole.MarkupLine($"[blue]Git Configuration for {repositoryName}[/]");
        AnsiConsole.WriteLine();

        var globalConfigPath = GetGitConfigPath(true);
        var localConfigPath = GetGitConfigPath(false);
        AnsiConsole.MarkupLine($"[dim]Global config:[/] [blue]{globalConfigPath}[/]");
        AnsiConsole.MarkupLine($"[dim]Local config:[/]  [blue]{localConfigPath}[/]");
        AnsiConsole.WriteLine();

        var appliedSettings = new List<(string Setting, string Value, bool IsGlobal)>();

        ConfigureAuthorIdentity(appliedSettings);
        var skipped = ConfigureRecommendedSettings(appliedSettings);
        PrintSummary(appliedSettings, skipped);
    }

    private static void ConfigureAuthorIdentity(List<(string Setting, string Value, bool IsGlobal)> appliedSettings)
    {
        AnsiConsole.MarkupLine("For consistency, your local git email should match your GitHub email.");
        AnsiConsole.MarkupLine("[dim]Note: When merging pull requests via GitHub's web interface, GitHub uses your primary email from [/][blue]https://github.com/settings/emails[/]");
        AnsiConsole.WriteLine();

        var localName = GetGitConfig("user.name", false);
        var globalName = GetGitConfig("user.name", true);
        var localEmail = GetGitConfig("user.email", false);
        var globalEmail = GetGitConfig("user.email", true);

        var effectiveName = !string.IsNullOrEmpty(localName) ? localName : globalName;
        var effectiveEmail = !string.IsNullOrEmpty(localEmail) ? localEmail : globalEmail;

        if (!string.IsNullOrEmpty(effectiveName))
        {
            var nameSource = !string.IsNullOrEmpty(localName) ? "[dim](local)[/]" : "[dim](global)[/]";
            AnsiConsole.MarkupLine($"  Name:  [green]{effectiveName.EscapeMarkup()}[/] {nameSource}");
        }
        else
        {
            AnsiConsole.MarkupLine("  Name:  [red]not set[/]");
        }

        if (!string.IsNullOrEmpty(effectiveEmail))
        {
            var emailSource = !string.IsNullOrEmpty(localEmail) ? "[dim](local)[/]" : "[dim](global)[/]";
            AnsiConsole.MarkupLine($"  Email: [green]{effectiveEmail.EscapeMarkup()}[/] {emailSource}");
        }
        else
        {
            AnsiConsole.MarkupLine("  Email: [red]not set[/]");
        }

        AnsiConsole.WriteLine();

        var needsConfiguration = string.IsNullOrEmpty(effectiveName) || string.IsNullOrEmpty(effectiveEmail);
        var wantsToChange = needsConfiguration || AnsiConsole.Confirm("Do you want to change this?", false);

        if (!wantsToChange)
        {
            AnsiConsole.WriteLine();
            return;
        }

        var newName = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter name:")
                .DefaultValue(effectiveName ?? "")
                .AllowEmpty()
        );

        var newEmail = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter email:")
                .DefaultValue(effectiveEmail ?? "")
                .AllowEmpty()
        );

        if (string.IsNullOrWhiteSpace(newName) || string.IsNullOrWhiteSpace(newEmail))
        {
            AnsiConsole.MarkupLine("[yellow]Name and email are required. Skipping author configuration.[/]");
            AnsiConsole.WriteLine();
            return;
        }

        var scope = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Apply name/email globally or for this repository only?")
                .AddChoices("Global (recommended)", "This repository only")
        );

        var isGlobal = scope == "Global (recommended)";

        SetGitConfig("user.name", newName, isGlobal);
        SetGitConfig("user.email", newEmail, isGlobal);

        appliedSettings.Add(("user.name", newName, isGlobal));
        appliedSettings.Add(("user.email", newEmail, isGlobal));

        AnsiConsole.WriteLine();
    }

    private static bool ConfigureRecommendedSettings(List<(string Setting, string Value, bool IsGlobal)> appliedSettings)
    {
        AnsiConsole.MarkupLine("The following settings are recommended for all developers:");
        AnsiConsole.WriteLine();

        var table = new Table();
        table.AddColumn("Setting");
        table.AddColumn("Current");
        table.AddColumn("Recommended");
        table.AddColumn("Description");
        table.Border(TableBorder.Rounded);

        var settingsToApply = new List<(string Setting, string Value)>();
        var settingsAlreadySet = new List<(string Setting, string Value, bool IsGlobal)>();

        foreach (var (setting, recommendedValue, description) in RecommendedSettings)
        {
            var globalValue = GetGitConfig(setting, true);
            var localValue = GetGitConfig(setting, false);
            var currentValue = globalValue ?? localValue;
            var currentDisplay = currentValue ?? "[dim]not set[/]";
            var alreadySet = currentValue == recommendedValue;

            if (alreadySet)
            {
                var isGlobal = globalValue == recommendedValue;
                table.AddRow(setting, $"[green]{currentDisplay}[/]", recommendedValue, $"[dim]{description}[/]");
                settingsAlreadySet.Add((setting, recommendedValue, isGlobal));
            }
            else
            {
                table.AddRow(setting, currentDisplay, $"[green]{recommendedValue}[/]", description);
                settingsToApply.Add((setting, recommendedValue));
            }
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (settingsToApply.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All recommended settings are already configured![/]");

            if (AnsiConsole.Confirm("Do you want to remove any settings?", false))
            {
                var removedSettings = RemoveSettingsOneByOne(settingsAlreadySet);
                PrintRemovalSummary(removedSettings);
            }

            AnsiConsole.WriteLine();
            return false;
        }

        var choices = new List<string>
        {
            $"Apply all {settingsToApply.Count} settings globally (recommended)",
            $"Apply all {settingsToApply.Count} settings to this repository only",
            "Ask for each setting one by one",
            "Skip"
        };

        if (settingsAlreadySet.Count > 0)
        {
            choices.Insert(choices.Count - 1, "Remove existing settings one by one");
        }

        var choice = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Apply {settingsToApply.Count} missing setting(s)?")
                .AddChoices(choices)
        );

        if (choice == "Skip")
        {
            AnsiConsole.MarkupLine("[dim]Skipping recommended settings.[/]");
            AnsiConsole.WriteLine();
            return true;
        }

        if (choice == "Remove existing settings one by one")
        {
            var removedSettings = RemoveSettingsOneByOne(settingsAlreadySet);
            PrintRemovalSummary(removedSettings);
            AnsiConsole.WriteLine();
            return false;
        }

        if (choice == "Ask for each setting one by one")
        {
            AnsiConsole.WriteLine();
            foreach (var (setting, value) in settingsToApply)
            {
                var description = RecommendedSettings.First(s => s.Setting == setting).Description;
                AnsiConsole.MarkupLine($"[yellow]{setting}[/] = [green]{value}[/]");
                AnsiConsole.MarkupLine($"[dim]{description}[/]");

                var settingChoice = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Apply this setting?")
                        .AddChoices("Global (recommended)", "This repository only", "Skip")
                );

                if (settingChoice != "Skip")
                {
                    var isGlobal = settingChoice == "Global (recommended)";
                    SetGitConfig(setting, value, isGlobal);
                    appliedSettings.Add((setting, value, isGlobal));
                }

                AnsiConsole.WriteLine();
            }
        }
        else
        {
            var isGlobal = choice.Contains("globally");

            foreach (var (setting, value) in settingsToApply)
            {
                SetGitConfig(setting, value, isGlobal);
                appliedSettings.Add((setting, value, isGlobal));
            }
        }

        AnsiConsole.WriteLine();
        return false;
    }

    private static List<(string Setting, bool IsGlobal)> RemoveSettingsOneByOne(List<(string Setting, string Value, bool IsGlobal)> settings)
    {
        var removedSettings = new List<(string Setting, bool IsGlobal)>();

        AnsiConsole.WriteLine();
        foreach (var (setting, value, isGlobal) in settings)
        {
            var description = RecommendedSettings.First(s => s.Setting == setting).Description;
            var scopeLabel = isGlobal ? "global" : "local";
            AnsiConsole.MarkupLine($"[yellow]{setting}[/] = [green]{value}[/] [dim]({scopeLabel})[/]");
            AnsiConsole.MarkupLine($"[dim]{description}[/]");

            if (AnsiConsole.Confirm("Remove this setting?", false))
            {
                UnsetGitConfig(setting, isGlobal);
                AnsiConsole.MarkupLine($"[red]Removed {setting}[/]");
                removedSettings.Add((setting, isGlobal));
            }

            AnsiConsole.WriteLine();
        }

        return removedSettings;
    }

    private static void PrintRemovalSummary(List<(string Setting, bool IsGlobal)> removedSettings)
    {
        if (removedSettings.Count == 0)
        {
            return;
        }

        AnsiConsole.MarkupLine("[yellow]Settings removed:[/]");
        var globalRemoved = removedSettings.Where(s => s.IsGlobal).ToList();
        var localRemoved = removedSettings.Where(s => !s.IsGlobal).ToList();

        if (globalRemoved.Count > 0)
        {
            AnsiConsole.MarkupLine("[dim]Global:[/]");
            foreach (var (setting, _) in globalRemoved)
            {
                AnsiConsole.MarkupLine($"  [red]✗[/] {setting}");
            }
        }

        if (localRemoved.Count > 0)
        {
            if (globalRemoved.Count > 0) AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Repository:[/]");
            foreach (var (setting, _) in localRemoved)
            {
                AnsiConsole.MarkupLine($"  [red]✗[/] {setting}");
            }
        }
    }

    private static void PrintSummary(List<(string Setting, string Value, bool IsGlobal)> appliedSettings, bool skipped)
    {
        if (appliedSettings.Count == 0)
        {
            if (skipped)
            {
                AnsiConsole.MarkupLine("[yellow]No changes were made.[/]");
            }

            return;
        }

        AnsiConsole.MarkupLine("[green]Git configuration complete![/]");
        AnsiConsole.WriteLine();

        var globalSettings = appliedSettings.Where(s => s.IsGlobal).ToList();
        var localSettings = appliedSettings.Where(s => !s.IsGlobal).ToList();

        if (globalSettings.Count > 0)
        {
            AnsiConsole.MarkupLine("[dim]Global settings applied:[/]");
            foreach (var (setting, value, _) in globalSettings)
            {
                AnsiConsole.MarkupLine($"  [green]✓[/] {setting} = {value.EscapeMarkup()}");
            }
        }

        if (localSettings.Count > 0)
        {
            if (globalSettings.Count > 0) AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[dim]Repository settings applied:[/]");
            foreach (var (setting, value, _) in localSettings)
            {
                AnsiConsole.MarkupLine($"  [green]✓[/] {setting} = {value.EscapeMarkup()}");
            }
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Tip:[/] Consider setting up commit signing for verified commits: [blue]https://docs.github.com/en/authentication/managing-commit-signature-verification[/]");
    }

    private static string GetGitConfigPath(bool global)
    {
        var scope = global ? "--global" : "--local";
        var result = ProcessHelper.StartProcess(
            $"git config {scope} --list --show-origin",
            Configuration.SourceCodeFolder,
            true,
            exitOnError: false
        );

        var defaultPath = global
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".gitconfig")
            : Path.Combine(Configuration.SourceCodeFolder, ".git", "config");

        if (string.IsNullOrWhiteSpace(result))
        {
            return defaultPath;
        }

        var firstLine = result.Split('\n').FirstOrDefault() ?? "";
        var match = Regex.Match(firstLine, @"file:(.+?)\t");
        if (match.Success)
        {
            var path = match.Groups[1].Value;
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(Path.Combine(Configuration.SourceCodeFolder, path));
            }

            return path;
        }

        return defaultPath;
    }

    private static string? GetGitConfig(string setting, bool global)
    {
        var scope = global ? "--global" : "--local";
        var result = ProcessHelper.StartProcess(
            $"git config {scope} {setting}",
            Configuration.SourceCodeFolder,
            true,
            exitOnError: false
        );
        return string.IsNullOrWhiteSpace(result) ? null : result.Trim();
    }

    private static void SetGitConfig(string setting, string value, bool global)
    {
        var scope = global ? "--global" : "--local";
        ProcessHelper.StartProcess(
            $"git config {scope} {setting} \"{value}\"",
            Configuration.SourceCodeFolder,
            true
        );
    }

    private static void UnsetGitConfig(string setting, bool global)
    {
        var scope = global ? "--global" : "--local";
        ProcessHelper.StartProcess(
            $"git config {scope} --unset {setting}",
            Configuration.SourceCodeFolder,
            true,
            exitOnError: false
        );
    }
}
