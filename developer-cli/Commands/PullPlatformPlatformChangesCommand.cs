using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class PullPlatformPlatformChangesCommand : Command
{
    private const string PullRequestBranchName = "platformplatform-updates";
    private const string TrunkBranchName = "main";
    private const string DefaultRemote = "origin";
    private const string UpstreamRemote = "upstream";
    private const string PlatformplatformGitPath = "https://github.com/platformplatform/PlatformPlatform.git";
    private const string PlatformplatformPullRequestPrefix = "PlatformPlatform PR ";

    public PullPlatformPlatformChangesCommand() : base("pull-platformplatform-changes", "Pull new updates from PlatformPlatform into a pull-request branch")
    {
        AddOption(new Option<bool>(["--verbose-logging"], "Show git command and output"));
        AddOption(new Option<bool>(["--auto-confirm", "-a"], "Auto confirm picking all upstream pull-requests"));
        AddOption(new Option<bool>(["--resume", "-r"], "Validate current branch and resume pulling updates starting with rerunning checks"));
        AddOption(new Option<bool>(["--run-code-cleanup", "-s"], "Run JetBrains code cleanup of backend (slow)"));

        Handler = CommandHandler.Create<bool, bool, bool, bool>(Execute);
    }

    private static int Execute(bool verboseLogging, bool autoConfirm, bool resume, bool runCodeCleanup)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node, Prerequisite.GithubCli);

        Configuration.VerboseLogging = verboseLogging;
        Configuration.AutoConfirm = autoConfirm;

        EnsureValidGitState();

        if (resume)
        {
            if (GetActiveBranchName() != PullRequestBranchName)
            {
                AnsiConsole.MarkupLine($"[yellow]Cannot 'resume': not on the pull-request branch '{PullRequestBranchName}'.[/]");
                Environment.Exit(0);
            }

            BuildTestAndCleanupCode(runCodeCleanup);
            ValidateGitStatus();
        }

        var newCommits = GetNewCommitsFromPlatformPlatform();
        if (newCommits.Length > 0)
        {
            if (!resume)
            {
                ConfirmationPrompt(newCommits);
            }

            EnsurePullRequestBranchExistsAndIsActive();

            foreach (var commit in newCommits)
            {
                if (!StartCherryPick(commit)) break;

                BuildTestAndCleanupCode(runCodeCleanup);
                ValidateGitStatus();
            }
        }

        var pullRequestCommits = GetPullRequestCommits();
        PreparePullRequest(pullRequestCommits);

        if (pullRequestCommits.Length == 0 && newCommits.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Everything looks up to date.[/]");
            Environment.Exit(0);
        }

        return 0;
    }

    private static void EnsureValidGitState()
    {
        if (HasPendingChanges())
        {
            AnsiConsole.MarkupLine("[red]You have uncommitted changes. Please stash or commit these and try again.[/]");
            Environment.Exit(0);
        }

        if (!HasOriginRemote())
        {
            AnsiConsole.MarkupLine($"[red]This repository does not have a remote called {DefaultRemote}, which is required for this command to work.[/]");
            Environment.Exit(0);
        }

        var activeBranchName = GetActiveBranchName();
        if (activeBranchName != PullRequestBranchName && DoesBranchExist(PullRequestBranchName))
        {
            AnsiConsole.MarkupLine($"[yellow]A branch named {PullRequestBranchName} already exists, but is not the active branch. Please merge, delete, or switch to this branch and try again.[/]");
            Environment.Exit(0);
        }
        else if (activeBranchName != TrunkBranchName && activeBranchName != PullRequestBranchName)
        {
            AnsiConsole.Confirm($"[yellow]You are neither on the {TrunkBranchName} nor the {PullRequestBranchName} branch. When using this command to pull changes from PlatformPlatform you must be on one of these branches.[/]");
            Environment.Exit(0);
        }

        EnsurePlatformPlatformUpstreamRemoteExists();
        EnsureBranchIsUpToDate();
    }

    private static Commit[] GetNewCommitsFromPlatformPlatform()
    {
        var lastMergedPullRequestHashFromPlatformPlatform = GetLatestCommitFromPlatformPlatform();

        // Find all pull request merge commits on the PlatformPlatform main branch since the last merge
        return ProcessHelper.StartProcess(
                $"""git --no-pager log {lastMergedPullRequestHashFromPlatformPlatform.Hash}..{UpstreamRemote}/main --min-parents=2 --oneline --format="%cd %h %s" --date=short""",
                Configuration.SourceCodeFolder,
                true
            )
            .ToCommits()
            .Reverse()
            .ToArray();

        static Commit GetLatestCommitFromPlatformPlatform()
        {
            var activeBranchName = GetActiveBranchName();
            // When pull requests from PlatformPlatform are cherry-picked, they are prefixed with "PlatformPlatform PR ### - Original pull request title"
            var latestPlatformPlatformCommit = ProcessHelper.StartProcess(
                    $"""git --no-pager log {activeBranchName} --grep="^{PlatformplatformPullRequestPrefix}" --format="%cd %h %s" --date=short""",
                    Configuration.SourceCodeFolder,
                    true
                )
                .ToCommits()
                .FirstOrDefault();

            if (latestPlatformPlatformCommit is not null)
            {
                // Now find the same commit in the PlatformPlatform upstream repository
                var pullRequestNumber = GetPullRequestNumber(latestPlatformPlatformCommit, $@"{PlatformplatformPullRequestPrefix}(\d+) - ");

                return ProcessHelper.StartProcess(
                        $"""git --no-pager log {UpstreamRemote}/main --grep=" (#{pullRequestNumber})$" --format="%cd %h %s" --date=short""",
                        Configuration.SourceCodeFolder,
                        true
                    )
                    .ToCommits()
                    .First();
            }

            // This is the first time pulling updates from PlatformPlatform, find the original commit
            var originalMergeCommitHash = ProcessHelper.StartProcess(
                $"git merge-base main {UpstreamRemote}/main",
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

    private static void ConfirmationPrompt(Commit[] newCommits)
    {
        var platformPlatformPullRequests = new StringBuilder();
        foreach (var commit in newCommits)
        {
            platformPlatformPullRequests.AppendLine($"{commit.Date} - {commit.Message.EscapeMarkup()}");
        }

        if (!AnsiConsole.Confirm(
                $"""
                 [green]Found the following {newCommits.Length} pull requests (commits) from PlatformPlatform that are not merged into this repository:[/]

                 {platformPlatformPullRequests}

                 If you continue, each pull request from PlatformPlatform will be cherry-picked into this repository one by one.

                 Tests, code linting, and code analysis will be run for each cherry-pick before proceeding to the next commit.

                 The titles will be prefixed with "PlatformPlatform PR # - Original title" indicating that this commit is coming from PlatformPlatform.

                 These titles are also used to track which changes from PlatformPlatform are already in this repository.

                 Do you want to continue?
                 """
            ))
        {
            Environment.Exit(0);
        }
    }

    private static void EnsurePullRequestBranchExistsAndIsActive()
    {
        if (GetActiveBranchName() == PullRequestBranchName) return;
        var gitBranchCommand = DoesBranchExist(PullRequestBranchName) ? $"git switch {PullRequestBranchName}" : $"git switch -c {PullRequestBranchName}";
        ProcessHelper.StartProcess(gitBranchCommand, Configuration.SourceCodeFolder);
    }

    private static bool StartCherryPick(Commit commit)
    {
        var fullCommitMessage = commit.GetFullCommitMessage();

        var pullRequestInfo = $"""
                               [green]

                               ----------------------------
                               [/]
                               [green]Commit hash:[/] {commit.Hash}
                               [green]Date:[/]{commit.Date}
                               [green]Commit message:[/]
                               {fullCommitMessage.EscapeMarkup()}

                               """;

        if (fullCommitMessage.Contains("- [x] Breaking Change"))
        {
            AnsiConsole.Confirm($"{pullRequestInfo}{Environment.NewLine}[red]This pull-requests contains breaking changes. Do you want to continue?[/]");
        }

        if (Configuration.AutoConfirm)
        {
            AnsiConsole.MarkupLine(pullRequestInfo);
        }
        else if (!AnsiConsole.Confirm($"{pullRequestInfo}{Environment.NewLine}Do you want to continue?"))
        {
            return false;
        }

        var cherryPickResult = ProcessHelper.StartProcess(
            $"git cherry-pick -m 1 --no-commit --strategy=recursive -X theirs {commit.Hash}",
            Configuration.SourceCodeFolder,
            true,
            exitOnError: false
        );

        if (cherryPickResult.Contains("error: "))
        {
            var prompt = $"""
                          The following error occured when cherry-picking commit:

                          [red]{cherryPickResult}[/]
                          Please fix the problem using your git tool, and complete the cherry-pick before continuing.
                          """;

            if (!AnsiConsole.Confirm(prompt))
            {
                Environment.Exit(0);
            }
        }

        if (!HasPendingChanges())
        {
            return AnsiConsole.Confirm("The cherry-pick did not create any changes. This happens when changes from PlatformPlatform have already been made in this repository. Continue?");
        }

        while (HasUnstagedChanges())
        {
            if (!AnsiConsole.Confirm("Please resolve all conflicts and stage all changes before continuing."))
            {
                Environment.Exit(0);
            }
        }

        // Use a regex to match the number after "PR "
        var pattern = @" \(#(\d+)\)$";
        var pullRequestNumber = GetPullRequestNumber(commit, pattern);
        var updatedMessage = $"PlatformPlatform PR {pullRequestNumber} - {fullCommitMessage
            .Replace($" (#{pullRequestNumber})", "")
            .Replace("\"", "\\\"").TrimEnd()}";
        ProcessHelper.StartProcess($"""git commit -m "{updatedMessage}" """, Configuration.SourceCodeFolder);

        if (fullCommitMessage.Contains("Downstream") && !AnsiConsole.Confirm($"{fullCommitMessage.EscapeMarkup()}{Environment.NewLine}[red]Before we continue, please follow the instructions described for Downstream Projects, and press enter to continue?[/]"))
        {
            Environment.Exit(0);
        }

        return true;
    }

    private static void BuildTestAndCleanupCode(bool runCodeCleanup)
    {
        BuildSolution();

        RunTests();

        CleanupBackendCode();

        CheckFrontendCode();

        void BuildSolution()
        {
            AnsiConsole.MarkupLine("[green]Building backend and frontend backend.[/]");
            while (true)
            {
                try
                {
                    ProcessHelper.StartProcess("dotnet tool restore", Configuration.ApplicationFolder, throwOnError: true);
                    ProcessHelper.StartProcess("dotnet build", Configuration.ApplicationFolder, throwOnError: true);
                    ProcessHelper.StartProcess("npm install", Configuration.ApplicationFolder, throwOnError: true);
                    ProcessHelper.StartProcess("npm run build", Configuration.ApplicationFolder, throwOnError: true);

                    AmendCommit();
                    break;
                }
                catch (ProcessExecutionException)
                {
                    if (!AnsiConsole.Confirm("[red]The build failed. Please fix the build and try again. If you continue, any fixes will be added to the cherry-picked change.[/]"))
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        void RunTests()
        {
            while (true)
            {
                try
                {
                    AnsiConsole.MarkupLine("[green]Running tests.[/]");
                    ProcessHelper.StartProcess("dotnet test", Configuration.ApplicationFolder, throwOnError: true);
                    break;
                }
                catch (ProcessExecutionException)
                {
                    if (!AnsiConsole.Confirm("Tests failed. Please fix the failing tests and try again. If you continue, any fixes will be added to the cherry-picked change."))
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        void CleanupBackendCode()
        {
            if (!runCodeCleanup) return;

            while (true)
            {
                try
                {
                    AnsiConsole.MarkupLine("[green]Running cleanup.[/]");

                    var solutionFile = Directory.GetFiles(Configuration.ApplicationFolder, "*.slnx", SearchOption.TopDirectoryOnly).Single();
                    ProcessHelper.StartProcess(
                        $"""dotnet jb cleanupcode {solutionFile} --profile=".NET only" """,
                        Configuration.ApplicationFolder,
                        throwOnError: true
                    );

                    break;
                }
                catch (ProcessExecutionException)
                {
                    if (!AnsiConsole.Confirm("JetBrains code cleanup failed. Please fix the errors and try again. If you continue, any fixes will be added to the cherry-picked change."))
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }

        void CheckFrontendCode()
        {
            while (true)
            {
                try
                {
                    AnsiConsole.MarkupLine("[green]Validating frontend code.[/]");
                    ProcessHelper.StartProcess("npm run lint", Configuration.ApplicationFolder, throwOnError: true);
                    ProcessHelper.StartProcess("npm run check", Configuration.ApplicationFolder, throwOnError: true);
                    break;
                }
                catch (ProcessExecutionException)
                {
                    if (!AnsiConsole.Confirm("Checking frontend codd failed. Please fix the errors and try again. If you continue, any fixes will be added to the cherry-picked change."))
                    {
                        Environment.Exit(0);
                    }
                }
            }
        }
    }

    private static void ValidateGitStatus()
    {
        while (HasPendingChanges())
        {
            AnsiConsole.MarkupLine("");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Code cleanup resulted in changes. How do you want to proceed?[/]")
                    .AddChoices("Add changes to cherry picked commit", "Add add new commit", "Abort")
            );

            switch (choice)
            {
                case "Add changes to cherry picked commit": AmendCommit(); break;
                case "Add add new commit": CreateChangeCommit("Code cleanup"); break;
                case "Abort": Environment.Exit(0); break;
            }
        }

        void CreateChangeCommit(string message)
        {
            AnsiConsole.MarkupLine("[green]Adding changes.[/]");
            ProcessHelper.StartProcess("git add .", Configuration.SourceCodeFolder);
            ProcessHelper.StartProcess($"git commit -m \"{message}\"", Configuration.SourceCodeFolder);
        }
    }

    private static void PreparePullRequest(Commit[] pullrequestCommits)
    {
        if (pullrequestCommits.Length == 0) return;

        if (!AnsiConsole.Confirm($"[bold]Found {pullrequestCommits.Length} commits on the {PullRequestBranchName} branch. Would you like to create a pull-request?[/]")) return;

        var body = new StringBuilder(
            """
            ### Summary & Motivation

            Apply updates from PlatformPlatform, including the same changes to this repository:

            """
        );

        var platformPlatformCommits = pullrequestCommits.Where(c => c.Message.StartsWith(PlatformplatformPullRequestPrefix)).ToArray();
        foreach (var commit in platformPlatformCommits)
        {
            var pullRequestNumber = GetPullRequestNumber(commit, $@"{PlatformplatformPullRequestPrefix}(\d+) - ");
            var link = $"https://github.com/platformplatform/PlatformPlatform/pull/{pullRequestNumber}";
            var prTitleParts = commit.Message.Split(" - ", 2);
            var pullRequestName = prTitleParts[0].Trim();
            var title = prTitleParts[1].Trim();
            body.AppendLine($"[[{pullRequestName}]]({link}) - {title}");
        }

        var rawCommits = pullrequestCommits.Except(platformPlatformCommits).ToArray();
        if (rawCommits.Any())
        {
            body.AppendLine(
                """

                Other changes to ensure reflect changes in PlatformPlatform:

                """
            );

            foreach (var commit in rawCommits)
            {
                body.AppendLine($"* {commit.Message}");
            }
        }


        body.AppendLine(
            """

            ------------------------------------------------------------------------------

            """
        );

        var pullRequestDescription = body.ToString();
        AnsiConsole.MarkupLine(pullRequestDescription);
        AnsiConsole.Confirm("Copy the above text as a description and use it for the pull request description. Continue?");

        var pushResult = ProcessHelper.StartProcess($"git push {DefaultRemote} {PullRequestBranchName}", Configuration.SourceCodeFolder, true, exitOnError: false);
        if (pushResult.Contains("error: "))
        {
            AnsiConsole.MarkupLine("[red]An error occured when pushing commits. Is your local branch out of sync with origin?[/]");
            Environment.Exit(0);
        }

        var githubUri = GithubHelper.GetGithubUri(DefaultRemote);
        var githubInfo = GithubHelper.GetGithubInfo(githubUri);

        ProcessHelper.OpenBrowser($"https://github.com/{githubInfo.Path}/compare/platformplatform-updates?expand=1");
    }

    private static Commit[] GetPullRequestCommits()
    {
        if (GetActiveBranchName() != PullRequestBranchName)
        {
            return [];
        }

        var lastCommitHash = ProcessHelper.StartProcess(
            "git --no-pager log -1 --pretty=format:\"%H\"",
            Configuration.SourceCodeFolder,
            true
        ).Trim();

        var lastMainHash = ProcessHelper.StartProcess("git merge-base HEAD main", Configuration.SourceCodeFolder, true).Trim();

        var startProcess = ProcessHelper.StartProcess(
            $"""git --no-pager log {lastMainHash}..{lastCommitHash} --oneline --format="%cd %h %s" --date=short""",
            Configuration.SourceCodeFolder,
            true
        );

        var pullRequestCommits = startProcess
            .ToCommits()
            .Reverse()
            .ToArray();
        return pullRequestCommits;
    }

    private static bool HasPendingChanges()
    {
        var pendingChanges = ProcessHelper.StartProcess("git status --porcelain", Configuration.SourceCodeFolder, true).Trim();
        return !string.IsNullOrEmpty(pendingChanges);
    }

    private static bool HasUnstagedChanges()
    {
        var unstagedChanges = ProcessHelper.StartProcess("git diff --name-only", Configuration.SourceCodeFolder, true).Trim();
        return !string.IsNullOrEmpty(unstagedChanges);
    }

    private static bool DoesBranchExist(string branchName)
    {
        var branches = ProcessHelper.StartProcess("git branch", Configuration.SourceCodeFolder, true).Split(Environment.NewLine);
        return branches.Select(b => b.Trim('*').Trim()).Contains(branchName);
    }

    private static string GetActiveBranchName()
    {
        return ProcessHelper.StartProcess("git rev-parse --abbrev-ref HEAD", Configuration.SourceCodeFolder, true).Trim();
    }

    private static bool HasOriginRemote()
    {
        var remotes = ProcessHelper.StartProcess("git remote -v", Configuration.SourceCodeFolder, true).Split(Environment.NewLine);
        return remotes.Any(r => r.Contains(DefaultRemote));
    }

    private static void AmendCommit()
    {
        AnsiConsole.MarkupLine("[green]Amending changes.[/]");
        ProcessHelper.StartProcess("git add .", Configuration.SourceCodeFolder);
        ProcessHelper.StartProcess("git commit --amend --no-edit", Configuration.SourceCodeFolder);
    }

    private static void EnsurePlatformPlatformUpstreamRemoteExists()
    {
        var remotes = ProcessHelper.StartProcess("git remote -v", Configuration.SourceCodeFolder, true).Split(Environment.NewLine);

        if (remotes.Any(r => r.Contains(UpstreamRemote) && r.Contains("github.com/platformplatform/PlatformPlatform.git")))
        {
            return;
        }

        if (!AnsiConsole.Confirm($"To pull changes from PlatformPlatform you need to add '{PlatformplatformGitPath}' as an upstream git remote. Add this now."))
        {
            Environment.Exit(0);
        }

        ProcessHelper.StartProcess($"git remote add {UpstreamRemote} {PlatformplatformGitPath}", Configuration.SourceCodeFolder);
    }

    private static void EnsureBranchIsUpToDate()
    {
        var activeBranchName = GetActiveBranchName();
        ProcessHelper.StartProcess($"git fetch {DefaultRemote}", Configuration.SourceCodeFolder);
        ProcessHelper.StartProcess($"git fetch {UpstreamRemote}", Configuration.SourceCodeFolder);

        var remoteBranches = ProcessHelper.StartProcess("git branch -r", Configuration.SourceCodeFolder, true)
            .Split(Environment.NewLine)
            .Select(b => b.Trim());
        if (!remoteBranches.Contains($"{DefaultRemote}/{PullRequestBranchName}")) return;

        var latestOriginCommit = ProcessHelper.StartProcess(
                $"""git --no-pager log {DefaultRemote}/{activeBranchName} --oneline --format="%cd %h %s" --date=short -n 1 """,
                Configuration.SourceCodeFolder,
                true
            )
            .ToCommits()
            .First();

        var localCommits = ProcessHelper.StartProcess(
                $"""git --no-pager log {activeBranchName} --oneline --format="%cd %h %s" --date=short""", Configuration.SourceCodeFolder,
                true
            )
            .ToCommits();

        if (localCommits.Contains(latestOriginCommit)) return;

        AnsiConsole.MarkupLine($"[red]Branch is not up to date with '{DefaultRemote}/{activeBranchName}'.[/]");
        Environment.Exit(0);
    }

    private static int GetPullRequestNumber(Commit commit, string pattern)
    {
        var match = Regex.Match(commit.Message, pattern);

        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        throw new InvalidOperationException("Pull request number not found.");
    }
}
