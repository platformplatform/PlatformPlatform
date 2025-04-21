using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class GitHelper
{
    private const string IntegrationBranch = "main";
    private const string DefaultRemote = "origin";
    private const string UpstreamRemote = "upstream";

    public static Commit[] ToCommits(this string consoleOutput)
    {
        return consoleOutput
            .Split("\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split(' ', 3))
            .Select(s => new Commit(s[0], s[1], s[2]))
            .ToArray();
    }

    public static bool HasUncommittedChanges()
    {
        var status = ProcessHelper.StartProcess("git status --porcelain", Configuration.SourceCodeFolder, true);
        return !string.IsNullOrWhiteSpace(status);
    }

    public static void DiscardAllChanges()
    {
        ProcessHelper.StartProcess("git reset --hard", Configuration.SourceCodeFolder);
    }

    public static void CreateBranch(string branchName)
    {
        ProcessHelper.StartProcess($"git switch --create {branchName}", Configuration.SourceCodeFolder);
    }

    public static void CheckoutIntegrationBranch()
    {
        ProcessHelper.StartProcess($"git checkout {IntegrationBranch}", Configuration.SourceCodeFolder);
    }

    public static void CheckoutRemoteBranch(string branchName)
    {
        ProcessHelper.StartProcess($"git checkout -b {branchName} origin/{branchName}", Configuration.SourceCodeFolder);
    }

    public static void SwitchToBranch(string branchName)
    {
        ProcessHelper.StartProcess($"git switch {branchName}", Configuration.SourceCodeFolder);
    }

    public static void PullLatest()
    {
        var currentBranch = GetCurrentBranch();

        if (RemoteBranchExists(currentBranch))
        {
            if (HasDivergentBranches(currentBranch))
            {
                AnsiConsole.MarkupLine("[yellow]Branch has diverged from origin. Local and remote branches have different commits.[/]");
                AnsiConsole.MarkupLine("[yellow]You may need to manually reconcile changes with 'git pull --rebase' or 'git pull --no-ff'.[/]");
            }
            else
            {
                ProcessHelper.StartProcess("git pull --ff-only", Configuration.SourceCodeFolder, exitOnError: false);
            }
        }
    }

    public static bool LocalBranchExists(string branchName)
    {
        var result = ProcessHelper.StartProcess($"git branch --list {branchName}", Configuration.SourceCodeFolder, true);
        return !string.IsNullOrEmpty(result);
    }

    public static bool RemoteBranchExists(string branchName)
    {
        FetchFromOrigin();
        var result = ProcessHelper.StartProcess($"git ls-remote --heads origin {branchName}", Configuration.SourceCodeFolder, true, exitOnError: false);
        return !string.IsNullOrEmpty(result);
    }

    public static bool HasDivergentBranches(string branchName)
    {
        ProcessHelper.StartProcess("git fetch", Configuration.SourceCodeFolder, exitOnError: false);

        var behindCount = ProcessHelper.StartProcess($"git rev-list --count HEAD..origin/{branchName}",
            Configuration.SourceCodeFolder, true, exitOnError: false
        );

        var aheadCount = ProcessHelper.StartProcess($"git rev-list --count origin/{branchName}..HEAD",
            Configuration.SourceCodeFolder, true, exitOnError: false
        );

        return !string.IsNullOrWhiteSpace(behindCount) && int.TryParse(behindCount.Trim(), out var behind) && behind > 0 &&
               !string.IsNullOrWhiteSpace(aheadCount) && int.TryParse(aheadCount.Trim(), out var ahead) && ahead > 0;
    }

    public static void FetchFromOrigin()
    {
        ProcessHelper.StartProcess("git fetch origin", Configuration.SourceCodeFolder);
    }

    public static void StashChanges(string message = "Stashed by Developer CLI")
    {
        ProcessHelper.StartProcess($"git stash push -m \"{message}\"", Configuration.SourceCodeFolder);
    }

    public static void PopStashedChanges()
    {
        ProcessHelper.StartProcess("git stash pop", Configuration.SourceCodeFolder, exitOnError: false);
    }

    public static bool IsBranchUpToDateWithOrigin(string branchName)
    {
        FetchFromOrigin();

        if (!RemoteBranchExists(branchName)) return true;

        var result = ProcessHelper.StartProcess($"git rev-list HEAD..origin/{branchName} --count", Configuration.SourceCodeFolder, true, exitOnError: false);
        return int.TryParse(result.Trim(), out var behindCount) && behindCount == 0;
    }

    public static string GetCurrentBranch()
    {
        var result = ProcessHelper.StartProcess("git rev-parse --abbrev-ref HEAD", Configuration.SourceCodeFolder, true);
        return result.Trim();
    }

    public static Dictionary<string, string> GetChangedFiles()
    {
        var status = ProcessHelper.StartProcess("git status --porcelain", Configuration.SourceCodeFolder, true);
        var changedFiles = status
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(f => !f.EndsWith('/') && !f.EndsWith('\\'))
            .Select(line => line[3..].Trim());

        return changedFiles.ToDictionary(file => file.Replace(Configuration.SourceCodeFolder, ""), GetFileHash);

        string GetFileHash(string file)
        {
            return ProcessHelper.StartProcess($"git hash-object {file}", Configuration.SourceCodeFolder, true).Trim();
        }
    }

    public static void AmendCommit()
    {
        ProcessHelper.StartProcess("git add .", Configuration.SourceCodeFolder);
        ProcessHelper.StartProcess("git commit --amend --no-edit", Configuration.SourceCodeFolder);
    }

    public static bool HasUnstagedChanges()
    {
        var unstagedChanges = ProcessHelper.StartProcess("git diff --name-only", Configuration.SourceCodeFolder, true).Trim();
        return !string.IsNullOrEmpty(unstagedChanges);
    }

    public static void EnsureUpstreamRemoteExists(string upstreamUrl)
    {
        var remotes = ProcessHelper.StartProcess("git remote -v", Configuration.SourceCodeFolder, true).Split(Environment.NewLine);

        if (remotes.Any(r => r.Contains(UpstreamRemote) && r.Contains(upstreamUrl)))
        {
            return;
        }

        if (!AnsiConsole.Confirm($"To pull changes you need to add '{upstreamUrl}' as an upstream git remote. Add this now?"))
        {
            Environment.Exit(0);
        }

        ProcessHelper.StartProcess($"git remote add {UpstreamRemote} {upstreamUrl}", Configuration.SourceCodeFolder);
    }

    public static void EnsureBranchIsUpToDate()
    {
        var currentBranch = GetCurrentBranch();
        FetchFromOrigin();
        ProcessHelper.StartProcess($"git fetch {UpstreamRemote}", Configuration.SourceCodeFolder);

        var remoteBranches = ProcessHelper.StartProcess("git branch -r", Configuration.SourceCodeFolder, true)
            .Split(Environment.NewLine)
            .Select(b => b.Trim());

        if (!remoteBranches.Contains($"{DefaultRemote}/{currentBranch}")) return;

        var latestOriginCommit = ProcessHelper.StartProcess(
                $"""git --no-pager log {DefaultRemote}/{currentBranch} --oneline --format="%cd %h %s" --date=short -n 1 """,
                Configuration.SourceCodeFolder,
                true
            )
            .ToCommits()
            .First();

        var localCommits = ProcessHelper.StartProcess(
                $"""git --no-pager log {currentBranch} --oneline --format="%cd %h %s" --date=short""",
                Configuration.SourceCodeFolder,
                true
            )
            .ToCommits();

        if (localCommits.Contains(latestOriginCommit)) return;

        AnsiConsole.MarkupLine($"[red]Branch is not up to date with '{DefaultRemote}/{currentBranch}'.[/]");
        Environment.Exit(0);
    }

    public static void PushBranch(string branchName)
    {
        var pushResult = ProcessHelper.StartProcess($"git push {DefaultRemote} {branchName}", Configuration.SourceCodeFolder, true, exitOnError: false);
        if (pushResult.Contains("error: "))
        {
            AnsiConsole.MarkupLine("[red]An error occurred when pushing commits. Is your local branch out of sync with origin?[/]");
            Environment.Exit(0);
        }
    }

    public static int GetPullRequestNumber(Commit commit, string pattern)
    {
        var match = Regex.Match(commit.Message, pattern);

        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        throw new InvalidOperationException("Pull request number not found.");
    }

    public static void CreateCommit(string message)
    {
        ProcessHelper.StartProcess("git add .", Configuration.SourceCodeFolder);
        ProcessHelper.StartProcess($"git commit -m \"{message}\"", Configuration.SourceCodeFolder);
    }

    public static void CherryPickCommit(string commitHash, bool noCommit = true)
    {
        var result = ProcessHelper.StartProcess(
            $"git cherry-pick -m 1 {(noCommit ? "--no-commit" : "")} --strategy=recursive -X theirs {commitHash}",
            Configuration.SourceCodeFolder,
            true,
            exitOnError: false
        );

        if (result.Contains("error: "))
        {
            AnsiConsole.MarkupLine($"""
                The following error occurred when cherry-picking commit:

                [red]{result}[/]
                Please fix the problem using your git tool, and complete the cherry-pick before continuing.
                """);
        }
    }
}

public record Commit
{
    public Commit(string date, string hash, string message)
    {
        ValidateHash(hash);
        Date = date;
        Hash = hash;
        Message = message;
    }

    public string Date { get; }

    public string Hash { get; }

    public string Message { get; }

    private static void ValidateHash(string hash)
    {
        if (!Regex.IsMatch(hash, "^[0-9a-fA-F]{6,}$"))
        {
            throw new ArgumentException("Invalid hash. Hash must be a hexadecimal string.", nameof(hash));
        }
    }

    public string GetFullCommitMessage()
    {
        return ProcessHelper.StartProcess($"git log -1 --pretty=%B {Hash}", Configuration.SourceCodeFolder, true);
    }
}
