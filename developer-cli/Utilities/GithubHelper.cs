using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class GithubHelper
{
    public static string GetGithubUri(string? remote = null)
    {
        // Get all Git remotes
        var output = ProcessHelper.StartProcess("git remote -v", Configuration.SourceCodeFolder, true);

        // Sort the output lines so that the "origin" is at the top
        output = string.Join('\n', output.Split('\n')
            .Where(line => remote == null || line.StartsWith(remote))
            .OrderBy(line => line.Contains("origin") ? 0 : 1)
        );

        var regex = new Regex(@"(?<githubUri>(https://github\.com/.*/.*\.git)|(git@github\.com:.*/.*\.git)) \(push\)");
        var matches = regex.Matches(output);

        var gitRemoteMatches = matches.Select(m => m.Groups["githubUri"].Value).ToArray();

        var githubUri = string.Empty;
        switch (gitRemoteMatches.Length)
        {
            case 0:
                AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. Please ensure you are within a Git repository with a GitHub.com as remote origin.[/]");
                Environment.Exit(0);
                break;
            case 1:
                githubUri = gitRemoteMatches.Single();
                break;
            case > 1:
                githubUri = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Select the GitHub remote")
                    .AddChoices(gitRemoteMatches)
                );
                ProcessHelper.StartProcess($"gh repo set-default {githubUri}");
                break;
        }

        return githubUri;
    }

    public static GithubInfo GetGithubInfo(string gitUri)
    {
        string remote;
        if (gitUri.StartsWith("https://github.com/"))
        {
            remote = gitUri.Replace("https://github.com/", "").Replace(".git", "");
        }
        else if (gitUri.StartsWith("git@github.com:"))
        {
            remote = gitUri.Replace("git@github.com:", "").Replace(".git", "");
        }
        else
        {
            throw new ArgumentException($"Invalid Git URI: {gitUri}. Only https:// and git@ formatted is supported.", nameof(gitUri));
        }

        var parts = remote.Split("/");
        return new GithubInfo(parts[0], parts[1]);
    }
}

public class GithubInfo(string organizationName, string repositoryName)
{
    public string OrganizationName { get; } = organizationName;

    public string RepositoryName { get; } = repositoryName;

    public string Path => $"{OrganizationName}/{RepositoryName}";

    public string Url => $"https://github.com/{Path}";
}
