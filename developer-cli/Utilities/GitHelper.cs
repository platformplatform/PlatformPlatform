using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class GitHelper
{
    private const string IntegrationBranch = "main";

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
        return status.Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Substring(3)) // Skip the status flags (first 3 characters)
            .ToDictionary(file => file.Replace(Configuration.SourceCodeFolder, ""), GetFileHash);

        string GetFileHash(string file)
        {
            return ProcessHelper.StartProcess($"git hash-object {file}", Configuration.SourceCodeFolder, true).Trim();
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
