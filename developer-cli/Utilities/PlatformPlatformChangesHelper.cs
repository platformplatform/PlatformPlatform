using DeveloperCli.Installation;

namespace DeveloperCli.Utilities;

public static class PlatformPlatformChangesHelper
{
    public const string PullRequestBranchName = "platformplatform-updates";
    public const string PlatformPlatformGitPath = "https://github.com/platformplatform/PlatformPlatform.git";
    public const string PullRequestPrefix = "PlatformPlatform PR ";
    public const string TrunkBranchName = "main";

    private const string UpstreamPullRequestPattern = @" \(#(\d+)\)$";

    public static Commit[] GetUnportedCommits()
    {
        var lastPortedCommit = GetLatestPortedCommitFromUpstream();

        return ProcessHelper.StartProcess(
                $"""git --no-pager log {lastPortedCommit.Hash}..upstream/main --min-parents=2 --oneline --format="%cd %h %s" --date=short""",
                Configuration.SourceCodeFolder,
                true
            )
            .ToCommits()
            .Reverse()
            .ToArray();
    }

    public static int GetUpstreamPullRequestNumber(Commit upstreamCommit)
    {
        return GitHelper.GetPullRequestNumber(upstreamCommit, UpstreamPullRequestPattern);
    }

    public static string BuildPortedCommitMessage(Commit upstreamCommit)
    {
        var fullCommitMessage = upstreamCommit.GetFullCommitMessage();
        var pullRequestNumber = GetUpstreamPullRequestNumber(upstreamCommit);

        return $"{PullRequestPrefix}{pullRequestNumber} - {fullCommitMessage
            .Replace($" (#{pullRequestNumber})", "")
            .Replace("\"", "\\\"")
            .TrimEnd()}";
    }

    private static Commit GetLatestPortedCommitFromUpstream()
    {
        var currentBranch = GitHelper.GetCurrentBranch();
        var latestPortedCommit = ProcessHelper.StartProcess(
                $"""git --no-pager log {currentBranch} --grep="^{PullRequestPrefix}" --format="%cd %h %s" --date=short""",
                Configuration.SourceCodeFolder,
                true
            )
            .ToCommits()
            .FirstOrDefault();

        if (latestPortedCommit is not null)
        {
            var pullRequestNumber = GitHelper.GetPullRequestNumber(latestPortedCommit, $@"{PullRequestPrefix}(\d+) - ");

            return ProcessHelper.StartProcess(
                    $"""git --no-pager log upstream/main --grep=" (#{pullRequestNumber})$" --format="%cd %h %s" --date=short""",
                    Configuration.SourceCodeFolder,
                    true
                )
                .ToCommits()
                .First();
        }

        var originalMergeCommitHash = ProcessHelper.StartProcess(
            "git merge-base main upstream/main",
            Configuration.SourceCodeFolder,
            true
        ).Trim();

        return ProcessHelper.StartProcess(
                $"""git --no-pager log {originalMergeCommitHash} -1 --oneline --format="%cd %h %s" --date=short""",
                Configuration.SourceCodeFolder,
                true
            )
            .ToCommits()
            .Single();
    }
}
