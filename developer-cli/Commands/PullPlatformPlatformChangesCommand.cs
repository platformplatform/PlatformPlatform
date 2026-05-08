using System.CommandLine;
using System.Text;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class PullPlatformPlatformChangesCommand : Command
{
    public PullPlatformPlatformChangesCommand() : base("pull-platformplatform-changes", "Pull new updates from PlatformPlatform into a pull-request branch")
    {
        var autoConfirmOption = new Option<bool>("--auto-confirm", "-a") { Description = "Auto confirm picking all upstream pull-requests" };
        var resumeOption = new Option<bool>("--resume", "-r") { Description = "Validate current branch and resume pulling updates starting with rerunning checks" };
        var runFormatOption = new Option<bool>("--run-format", "-s") { Description = "Run JetBrains format of backend code (slow)" };
        var listOnlyOption = new Option<bool>("--list-only", "-l") { Description = "Print unported commits and exit. No mutations or prompts." };

        Options.Add(autoConfirmOption);
        Options.Add(resumeOption);
        Options.Add(runFormatOption);
        Options.Add(listOnlyOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(autoConfirmOption),
                parseResult.GetValue(resumeOption),
                parseResult.GetValue(runFormatOption),
                parseResult.GetValue(listOnlyOption)
            )
        );
    }

    private static void Execute(bool autoConfirm, bool resume, bool runCodeFormat, bool listOnly)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node, Prerequisite.GithubCli);

        if (listOnly)
        {
            ExecuteListOnly();
            return;
        }

        Configuration.AutoConfirm = autoConfirm;

        EnsureValidGitState();

        if (resume)
        {
            if (GitHelper.GetCurrentBranch() != PlatformPlatformChangesHelper.PullRequestBranchName)
            {
                AnsiConsole.MarkupLine($"[yellow]Cannot 'resume': not on the pull-request branch '{PlatformPlatformChangesHelper.PullRequestBranchName}'.[/]");
                Environment.Exit(0);
            }

            BuildTestAndFormatCode(runCodeFormat);
            ValidateGitStatus();
        }

        var newCommits = PlatformPlatformChangesHelper.GetUnportedCommits();
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

                BuildTestAndFormatCode(runCodeFormat);
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
    }

    private static void ExecuteListOnly()
    {
        GitHelper.EnsureUpstreamRemoteExists(PlatformPlatformChangesHelper.PlatformPlatformGitPath);
        ProcessHelper.StartProcess("git fetch upstream", Configuration.SourceCodeFolder, true);

        var commits = PlatformPlatformChangesHelper.GetUnportedCommits();
        foreach (var commit in commits)
        {
            Console.WriteLine($"{commit.Date} {commit.Hash} {commit.Message}");
        }
    }

    private static void EnsureValidGitState()
    {
        if (GitHelper.HasUncommittedChanges())
        {
            AnsiConsole.MarkupLine("[red]You have uncommitted changes. Please stash or commit these and try again.[/]");
            Environment.Exit(0);
        }

        var currentBranch = GitHelper.GetCurrentBranch();
        if (currentBranch != PlatformPlatformChangesHelper.PullRequestBranchName && GitHelper.LocalBranchExists(PlatformPlatformChangesHelper.PullRequestBranchName))
        {
            AnsiConsole.MarkupLine($"[yellow]A branch named {PlatformPlatformChangesHelper.PullRequestBranchName} already exists, but is not the active branch. Please merge, delete, or switch to this branch and try again.[/]");
            Environment.Exit(0);
        }
        else if (currentBranch != PlatformPlatformChangesHelper.TrunkBranchName && currentBranch != PlatformPlatformChangesHelper.PullRequestBranchName)
        {
            AnsiConsole.Confirm($"[yellow]You are neither on the {PlatformPlatformChangesHelper.TrunkBranchName} nor the {PlatformPlatformChangesHelper.PullRequestBranchName} branch. When using this command to pull changes from PlatformPlatform you must be on one of these branches.[/]");
            Environment.Exit(0);
        }

        GitHelper.EnsureUpstreamRemoteExists(PlatformPlatformChangesHelper.PlatformPlatformGitPath);
        GitHelper.EnsureBranchIsUpToDate();
        GitHelper.EnsureLocalBranchInSyncWithOrigin(PlatformPlatformChangesHelper.TrunkBranchName);
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
        if (GitHelper.GetCurrentBranch() == PlatformPlatformChangesHelper.PullRequestBranchName) return;

        if (GitHelper.LocalBranchExists(PlatformPlatformChangesHelper.PullRequestBranchName))
        {
            GitHelper.SwitchToBranch(PlatformPlatformChangesHelper.PullRequestBranchName);
        }
        else
        {
            GitHelper.CreateBranch(PlatformPlatformChangesHelper.PullRequestBranchName);
        }
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

        GitHelper.CherryPickCommit(commit.Hash);

        if (!GitHelper.HasUncommittedChanges())
        {
            return AnsiConsole.Confirm("The cherry-pick did not create any changes. This happens when changes from PlatformPlatform have already been made in this repository. Continue?");
        }

        while (GitHelper.HasUnstagedChanges())
        {
            if (!AnsiConsole.Confirm("Please resolve all conflicts and stage all changes before continuing."))
            {
                Environment.Exit(0);
            }
        }

        GitHelper.CreateCommit(PlatformPlatformChangesHelper.BuildPortedCommitMessage(commit));

        if (fullCommitMessage.Contains("Downstream") && !AnsiConsole.Confirm($"{fullCommitMessage.EscapeMarkup()}{Environment.NewLine}[red]Before we continue, please follow the instructions described for Downstream Projects, and press enter to continue?[/]"))
        {
            Environment.Exit(0);
        }

        return true;
    }

    private static void BuildTestAndFormatCode(bool runCodeFormat)
    {
        while (true)
        {
            try
            {
                new BuildCommand().Parse([]).Invoke();
                new TestCommand().Parse(["--no-build"]).Invoke();

                if (runCodeFormat)
                {
                    new FormatCommand().Parse(["--no-build"]).Invoke();
                }

                break;
            }
            catch (Exception)
            {
                if (!AnsiConsole.Confirm("[red]The check command failed. Please fix any issues and try again. If you continue, any fixes will be added to the cherry-picked change.[/]"))
                {
                    Environment.Exit(0);
                }
            }
        }

        GitHelper.AmendCommit();
    }

    private static void ValidateGitStatus()
    {
        while (GitHelper.HasUncommittedChanges())
        {
            AnsiConsole.MarkupLine("");

            var choice = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[yellow]Code format resulted in changes. How do you want to proceed?[/]")
                    .AddChoices("Add changes to cherry picked commit", "Add add new commit", "Abort")
            );

            switch (choice)
            {
                case "Add changes to cherry picked commit": GitHelper.AmendCommit(); break;
                case "Add add new commit": GitHelper.CreateCommit("Code format"); break;
                case "Abort": Environment.Exit(0); break;
            }
        }
    }

    private static Commit[] GetPullRequestCommits()
    {
        if (GitHelper.GetCurrentBranch() != PlatformPlatformChangesHelper.PullRequestBranchName)
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

        return startProcess
            .ToCommits()
            .Reverse()
            .ToArray();
    }

    private static void PreparePullRequest(Commit[] pullrequestCommits)
    {
        if (pullrequestCommits.Length == 0) return;

        if (!AnsiConsole.Confirm($"[bold]Found {pullrequestCommits.Length} commits on the {PlatformPlatformChangesHelper.PullRequestBranchName} branch. Would you like to create a pull-request?[/]")) return;

        var body = new StringBuilder(
            """
            ### Summary & Motivation

            Apply updates from PlatformPlatform, including the same changes to this repository:

            """
        );

        var platformPlatformCommits = pullrequestCommits.Where(c => c.Message.StartsWith(PlatformPlatformChangesHelper.PullRequestPrefix)).ToArray();
        foreach (var commit in platformPlatformCommits)
        {
            var pullRequestNumber = GitHelper.GetPullRequestNumber(commit, $@"{PlatformPlatformChangesHelper.PullRequestPrefix}(\d+) - ");
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
        AnsiConsole.MarkupLine(pullRequestDescription.EscapeMarkup());
        AnsiConsole.Confirm("Copy the above text as a description and use it for the pull request description. Continue?");

        GitHelper.PushBranch(PlatformPlatformChangesHelper.PullRequestBranchName);

        var githubUri = GithubHelper.GetGithubUri("origin");
        var githubInfo = GithubHelper.GetGithubInfo(githubUri);

        ProcessHelper.OpenBrowser($"https://github.com/{githubInfo.Path}/compare/platformplatform-updates?expand=1");
    }
}
