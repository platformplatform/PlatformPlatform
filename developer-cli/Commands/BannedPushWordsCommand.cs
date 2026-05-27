using System.CommandLine;
using System.Text.RegularExpressions;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public sealed class BannedPushWordsCommand : Command
{
    public BannedPushWordsCommand() : base("banned-push-words", "Configure words banned from diffs and which remotes to enforce them on")
    {
        var wordsArgument = new Argument<string[]>("words")
        {
            Description = "Words to ban (replaces the current list)",
            Arity = ArgumentArity.ZeroOrMore
        };
        var clearOption = new Option<bool>("--clear") { Description = "Remove all banned words and enforced remotes" };

        Arguments.Add(wordsArgument);
        Options.Add(clearOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(wordsArgument) ?? [],
                parseResult.GetValue(clearOption)
            )
        );
    }

    private static void Execute(string[] words, bool clear)
    {
        var wordsFilePath = Path.Combine(Configuration.PublishFolder, $"{Configuration.AliasName}.banned-push-words");
        var remotesFilePath = Path.Combine(Configuration.PublishFolder, $"{Configuration.AliasName}.banned-push-words-remotes");

        if (clear)
        {
            if (File.Exists(wordsFilePath)) File.Delete(wordsFilePath);
            if (File.Exists(remotesFilePath)) File.Delete(remotesFilePath);
            AnsiConsole.MarkupLine("[green]Cleared all banned push words and enforced remotes.[/]");
            return;
        }

        if (words.Length == 0)
        {
            ListCurrentConfiguration(wordsFilePath, remotesFilePath);
            return;
        }

        Directory.CreateDirectory(Configuration.PublishFolder);

        SetBannedWords(wordsFilePath, words);
        PromptEnforcedRemotes(remotesFilePath);
    }

    private static void ListCurrentConfiguration(string wordsFilePath, string remotesFilePath)
    {
        var hasWords = File.Exists(wordsFilePath) && new FileInfo(wordsFilePath).Length > 0;
        var hasRemotes = File.Exists(remotesFilePath) && new FileInfo(remotesFilePath).Length > 0;

        if (!hasWords && !hasRemotes)
        {
            AnsiConsole.MarkupLine("[yellow]No push guards configured.[/]");
            AnsiConsole.MarkupLine($"[grey]Set up with: [blue]{Configuration.AliasName} banned-push-words Word1 Word2[/][/]");
            return;
        }

        if (hasWords)
        {
            var existingWords = File.ReadAllLines(wordsFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            AnsiConsole.MarkupLine("[blue]Banned push words:[/]");
            PrintWordsWithPermutations(existingWords);
            AnsiConsole.WriteLine();
        }

        if (hasRemotes)
        {
            var enforcedUrls = File.ReadAllLines(remotesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
            var remotes = GetPushRemotes();
            AnsiConsole.MarkupLine("[blue]Enforced on remotes:[/]");
            foreach (var url in enforcedUrls)
            {
                var name = remotes.FirstOrDefault(r => NormalizeGitUrl(r.Url).Equals(url, StringComparison.OrdinalIgnoreCase))?.Name;
                AnsiConsole.MarkupLine(name is not null ? $"  - [blue]{Markup.Escape(name)}[/]  →  {Markup.Escape(url)}" : $"  - {Markup.Escape(url)}");
            }
        }
        else if (hasWords)
        {
            AnsiConsole.MarkupLine("[grey]Enforced on: all remotes (no specific remotes selected)[/]");
        }
    }

    private static void SetBannedWords(string filePath, string[] words)
    {
        File.WriteAllLines(filePath, words);

        AnsiConsole.MarkupLine($"[green]Set {words.Length} banned push word(s):[/]");
        PrintWordsWithPermutations(words);
        AnsiConsole.WriteLine();
    }

    private static void PromptEnforcedRemotes(string remotesFilePath)
    {
        var remotes = GetPushRemotes();
        if (remotes.Length == 0) return;

        if (!AnsiConsole.Profile.Capabilities.Interactive) return;

        var enforcedUrls = File.Exists(remotesFilePath)
            ? File.ReadAllLines(remotesFilePath).Where(l => !string.IsNullOrWhiteSpace(l)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (enforcedUrls.Count > 0)
        {
            AnsiConsole.MarkupLine("[blue]Currently enforced on:[/]");
            foreach (var url in enforcedUrls)
            {
                var name = remotes.FirstOrDefault(r => NormalizeGitUrl(r.Url).Equals(url, StringComparison.OrdinalIgnoreCase))?.Name;
                AnsiConsole.MarkupLine(name is not null ? $"  - [blue]{Markup.Escape(name)}[/]  →  {Markup.Escape(url)}" : $"  - {Markup.Escape(url)}");
            }

            AnsiConsole.WriteLine();
        }

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<PushRemote>()
                .Title("Select remotes to [blue]enforce[/] banned words on:")
                .NotRequired()
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm. None = enforce on all.)[/]")
                .UseConverter(r => $"{Markup.Escape(r.Name)}  →  {Markup.Escape(r.Url)}")
                .AddChoices(remotes)
        );

        var normalizedUrls = selected.Select(r => NormalizeGitUrl(r.Url)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

        if (normalizedUrls.Length > 0)
        {
            File.WriteAllLines(remotesFilePath, normalizedUrls);
            AnsiConsole.MarkupLine($"[green]Enforcing banned words on {normalizedUrls.Length} remote(s):[/]");
            foreach (var remote in selected)
            {
                AnsiConsole.MarkupLine($"  - [blue]{Markup.Escape(remote.Name)}[/]  →  {Markup.Escape(remote.Url)}");
            }
        }
        else
        {
            if (File.Exists(remotesFilePath)) File.Delete(remotesFilePath);
            AnsiConsole.MarkupLine("[green]Enforcing banned words on all remotes.[/]");
        }
    }

    private static PushRemote[] GetPushRemotes()
    {
        var output = ProcessHelper.StartProcess("git remote -v", Configuration.SourceCodeFolder, true, exitOnError: false).Trim();
        if (string.IsNullOrEmpty(output)) return [];

        return output.Split('\n')
            .Where(line => line.Contains("(push)"))
            .Select(line =>
                {
                    var parts = line.Split('\t', 2);
                    var name = parts[0];
                    var url = parts[1].Replace("(push)", "").Trim();
                    return new PushRemote(name, url);
                }
            )
            .ToArray();
    }

    private static string NormalizeGitUrl(string url)
    {
        url = url.Trim();
        url = Regex.Replace(url, "^https?://", "");
        url = Regex.Replace(url, "^git@([^:]+):", "$1/");
        url = Regex.Replace(url, @"\.git$", "");
        return url.TrimEnd('/').ToLowerInvariant();
    }

    private static void PrintWordsWithPermutations(string[] words)
    {
        foreach (var word in words)
        {
            var permutations = GeneratePermutations(word);
            AnsiConsole.MarkupLine($"  - [blue]{Markup.Escape(word)}[/]");
            if (permutations.Length > 0)
            {
                AnsiConsole.MarkupLine($"    [grey]also matches: {Markup.Escape(string.Join(", ", permutations))}[/]");
            }
        }
    }

    private static string[] GeneratePermutations(string word)
    {
        var parts = SplitPascalCase(word);

        if (parts.Length <= 1)
        {
            return [word.ToLowerInvariant(), word.ToUpperInvariant()];
        }

        var lowerParts = parts.Select(p => p.ToLowerInvariant()).ToArray();
        var upperParts = parts.Select(p => p.ToUpperInvariant()).ToArray();
        var camel = lowerParts[0] + string.Join("", parts.Skip(1));

        return
        [
            camel,
            string.Join("_", lowerParts),
            string.Join("-", lowerParts),
            string.Join("_", upperParts),
            string.Join(" ", parts),
            string.Concat(lowerParts)
        ];
    }

    private static string[] SplitPascalCase(string word)
    {
        var parts = Regex.Split(word, "(?<=[a-z])(?=[A-Z])");
        return parts.Where(p => p.Length > 0).ToArray();
    }

    private sealed record PushRemote(string Name, string Url);
}
