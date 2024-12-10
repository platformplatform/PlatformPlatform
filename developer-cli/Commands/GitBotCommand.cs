using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class GitBotCommand : Command
{
    private static readonly DirectoryInfo WorkingDirectory =
        new(Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application"));

    private static readonly FileInfo SolutionFile = GetApplicationSolutionFileInfo()!;
    private static readonly string DefaultNamespace = SolutionFile.Name[..SolutionFile.Name.IndexOf('.')];

    public GitBotCommand() : base("gitbot", "Merge the next commit from PlatformPlatform into a pull-request branch")
    {
        var writeOption = new Option<bool>(
            ["--write", "-w"],
            () => false,
            "Merge the commit, otherwise do a dry-run"
        );

        var yesOption = new Option<bool>(
            ["--yes", "-y"],
            () => false,
            "Skip the confirmation prompt"
        );

        AddOption(writeOption);
        AddOption(yesOption);

        Handler = CommandHandler.Create<bool, bool>(Execute);
    }

    private static int Execute(bool write, bool yes)
    {
        if (yes) AnsiConsole.MarkupLine("[yellow]Skipping confirmation prompts[/]");

        if (!write)
            AnsiConsole.MarkupLine(
                "[yellow]Dry-run: This command will not modify your source code[/] (Use --write to modify)");

        var startPrompt = $"""
                           [green]This command will merge the next commit from PlatformPlatform into a pull-request branch[/]
                           [gray]Namespace:[/] {DefaultNamespace}
                           [gray]Working Directory:[/] {WorkingDirectory.FullName}
                           [gray]Dry-run:[/] {!write}

                           [bold]Would you like to continue?[/]
                           """;

        if (!yes && !AnsiConsole.Confirm(startPrompt)) return 0;

        if (!CheckGitStatus()) return 0;

        FetchUpstream();
        var newCommits = GetNewCommits();

        if (newCommits.Length == 0)
        {
            AnsiConsole.MarkupLine("[green]No new commits found[/]");
            return 0;
        }

        var cherryPickedCommits = GetCherryPickedCommits();

        AnsiConsole.MarkupLine("[green]New commits found:[/]");
        AnsiConsole.MarkupLine($"[green]Found {newCommits.Length} commits[/]");
        Commit? nextCommit = null;
        foreach (var commit in newCommits)
        {
            if (Array.Exists(cherryPickedCommits, c => c.Message == commit.Message))
            {
                AnsiConsole.MarkupLine($"[gray]Skipping:[/] {commit.Hash} - {commit.Message.EscapeMarkup()}");
                continue;
            }

            if (nextCommit == null)
            {
                nextCommit = commit;
                AnsiConsole.MarkupLine($"[green]Merging:[/] {commit.Hash} - {commit.Message.EscapeMarkup()}");
                continue;
            }

            AnsiConsole.MarkupLine($"[gray]Next:[/] {commit.Hash} - {commit.Message.EscapeMarkup()}");
        }

        if (nextCommit == null)
        {
            AnsiConsole.MarkupLine("[green]No new commits found[/]");
            return 0;
        }

        if (!CreateBranchAndMerge(nextCommit, DefaultNamespace, write, yes)) return 1;

        return 0;
    }

    private static bool CheckGitStatus()
    {
        if (!CheckLocalMain()) return false;

        if (!CheckLocalUpToDate()) return false;

        if (!CheckLocalBranchClean()) return false;

        if (!CheckRemoteUpstream()) return false;

        return true;
    }

    private static bool CreateBranchAndMerge(Commit commit, string projectNamespace, bool write, bool yes)
    {
        var branchName = GetBranchName(commit.Message);

        if (BranchExists(branchName))
        {
            AnsiConsole.MarkupLine($"[red]Error: Branch '{branchName}' already exists[/]");
            return false;
        }

        var fullCommitMessage = GetCommitMessage(commit.Hash);
        var rule = new Rule("[green]Creating a pull-request branch[/]");
        AnsiConsole.Write(rule);
        var prompt = $"""
                      [green]Commit hash:[/]
                      {commit.Hash}

                      [green]Branch name:[/]
                      {branchName}

                      [green]Commit message:[/]
                      {fullCommitMessage.EscapeMarkup()}


                      [bold]Would you like to continue?[/]
                      """;

        if (!yes && !AnsiConsole.Confirm(prompt)) return true;

        if (!CreateBranch(branchName, write)) return false;

        if (!MergeCommit(commit, true, write)) return false;

        if (!CleanupAndTestCSharpCode(projectNamespace, write)) return false;

        if (!CleanupAndTestTypeScriptCode(write)) return false;

        if (!CheckChangesToCommit())
        {
            AnsiConsole.MarkupLine("[green]No additional changes to commit[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Changes to commit after code cleanup[/]");
            if (!CreateChangeCommit("Code cleanup", write))
            {
                AnsiConsole.MarkupLine("[red]Error: Failed to create a commit with changes after code cleanup[/]");
                return false;
            }
        }

        if (!yes && !AnsiConsole.Confirm("[bold]Would you like to create a pull-request?[/]")) return true;

        if (!CreatePullRequest(commit.Message, fullCommitMessage, write)) return false;

        return true;
    }

    private static bool CleanupAndTestTypeScriptCode(bool write)
    {
        if (!RunNpmInstall(write)) return false;

        if (!RunNpmFormat(write)) return false;

        if (!RunNpmLint(write)) return false;

        if (!RunNpmBuild(write)) return false;

        if (!RunNpmCheck(write)) return false;

        return true;
    }

    private static bool CleanupAndTestCSharpCode(string projectNamespace, bool write)
    {
        if (!SearchAndReplaceInFiles("using PlatformPlatform.", $"using {projectNamespace}.", SearchReplaceType.C_SHARP,
                write)) return false;

        if (!SearchAndReplaceInFiles("namespace PlatformPlatform.", $"namespace {projectNamespace}.",
                SearchReplaceType.C_SHARP, write)) return false;

        if (!SearchAndReplaceInFiles(">PlatformPlatform.", $">{projectNamespace}.", SearchReplaceType.CS_PROJECT,
                write)) return false;

        if (!SearchAndReplaceInFiles(">PlatformPlatform.", $">{projectNamespace}.", SearchReplaceType.ES_PROJECT,
                write)) return false;

        if (!CodeCleanupSolution(write)) return false;

        if (!TestSolution(write)) return false;

        return true;
    }

    private static bool CheckLocalMain()
    {
        var result = ProcessHelper.StartProcess("git branch", redirectOutput: true);
        var lines = result.Split(Environment.NewLine);

        foreach (var line in lines)
            if (line.Contains("* main"))
            {
                AnsiConsole.MarkupLine("[gray]Using branch: 'main'[/]");
                return true;
            }

        AnsiConsole.MarkupLine("[red]Please switch to the 'main' branch[/]");
        return false;
    }

    private static bool CheckLocalUpToDate()
    {
        var result = ProcessHelper.StartProcess("git status", redirectOutput: true);
        var lines = result.Split(Environment.NewLine);

        foreach (var line in lines)
            if (line.Contains("Your branch is up to date with 'origin/main'."))
            {
                AnsiConsole.MarkupLine("[gray]Branch is up to date with 'origin/main'[/]");
                return true;
            }

        AnsiConsole.MarkupLine("[red]Branch is not up to date with 'origin/main'[/]");
        return false;
    }

    private static bool CheckLocalBranchClean()
    {
        var result = ProcessHelper.StartProcess("git status --porcelain", redirectOutput: true);
        if (string.IsNullOrEmpty(result))
        {
            AnsiConsole.MarkupLine("[gray]Branch is clean[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[red]Branch has uncommitted changes[/]");
        return false;
    }

    private static bool CheckRemoteUpstream()
    {
        var result = ProcessHelper.StartProcess("git remote -v", redirectOutput: true);
        var lines = result.Split(Environment.NewLine);

        foreach (var line in lines)
            if (line.Contains("upstream") && line.Contains("git@github.com:platformplatform/PlatformPlatform.git"))
            {
                AnsiConsole.MarkupLine("[blue]Using: GitHub platformplatform/PlatformPlatform 'upstream'[/]");
                return true;
            }

        AnsiConsole.MarkupLine("[red]GitHub platformplatform/PlatformPlatform 'upstream' not found[/]");
        AnsiConsole.MarkupLine("Add the remote with the following command:");
        AnsiConsole.MarkupLine("git remote add upstream git@github.com:platformplatform/PlatformPlatform.git");

        return false;
    }

    private static void FetchUpstream()
    {
        ProcessHelper.StartProcess("git fetch upstream");
    }

    private static Commit[] GetNewCommits()
    {
        return ProcessHelper.StartProcess("git log upstream/main --not --remotes=origin --oneline --merges",
                redirectOutput: true)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split(' ', 2))
            .Select(s => new Commit(s[0], s[1]))
            .Reverse()
            .ToArray();
    }

    private static Commit[] GetCherryPickedCommits()
    {
        return ProcessHelper.StartProcess("git cherry -v upstream/main", redirectOutput: true)
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Split(' ', 3))
            .Select(s => new Commit(s[1], s[2]))
            .Reverse()
            .ToArray();
    }

    private static string GetCommitMessage(string hash)
    {
        return ProcessHelper.StartProcess($"git log -1 --pretty=%B {hash}", redirectOutput: true);
    }

    private static bool BranchExists(string branchName)
    {
        var result = ProcessHelper.StartProcess("git branch", redirectOutput: true);
        var lines = result.Split(Environment.NewLine);

        foreach (var line in lines)
            if (line.Contains(branchName))
                return true;

        return false;
    }

    private static bool MergeCommit(Commit commit, bool useTheirs, bool write)
    {
        var command = useTheirs
            ? $"git cherry-pick -m 1 --strategy=recursive -X theirs {commit.Hash}"
            : $"git cherry-pick -m 1 {commit.Hash}";
        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {command}[/]");
            return true;
        }

        var result = ProcessHelper.StartProcess(command);
        if (result.Contains("error: could not apply"))
        {
            AnsiConsole.MarkupLine($"[red]Error: {result}[/]");
            return false;
        }

        return true;
    }

    private static bool CreateBranch(string branchName, bool write)
    {
        var command = $"git checkout -b {branchName}";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {command}[/]");
            return true;
        }

        var result = ProcessHelper.StartProcess(command);
        if (result.Contains("fatal: A branch named"))
        {
            AnsiConsole.MarkupLine($"[red]Error: {result}[/]");
            return false;
        }

        return true;
    }

    private static string GetBranchName(string message)
    {
        var branchName = Regex.Replace(message, "[^a-zA-Z0-9]", "-");
        branchName = branchName.ToLowerInvariant().Split("---")[0];
        return branchName.Length > 244 ? branchName[..244] : branchName;
    }

    private static bool SearchAndReplaceInFiles(string search, string replace, SearchReplaceType searchReplaceType,
        bool write)
    {
        if (search == replace) return true;

        var searchPattern = "*.cs";
        var type = "C#";

        switch (searchReplaceType)
        {
            case SearchReplaceType.TYPE_SCRIPT:
                searchPattern = "*.ts";
                type = "TypeScript";
                break;
            case SearchReplaceType.CS_PROJECT:
                searchPattern = "*.csproj";
                type = "C# project";
                break;
            case SearchReplaceType.ES_PROJECT:
                searchPattern = "*.esproj";
                type = "ES project";
                break;
        }

        try
        {
            var files = WorkingDirectory.GetFiles(searchPattern, SearchOption.AllDirectories);
            AnsiConsole.MarkupLine(
                $"[green]Searching for '{search}' using replacement '{replace}' in {files.Length} {type} files[/]");
            var found = 0;
            foreach (var fullname in files.Select(file => file.FullName))
            {
                var content = File.ReadAllText(fullname);
                if (!content.Contains(search)) continue;
                found++;
                if (!write)
                {
                    AnsiConsole.MarkupLine($"[yellow]Dry-run: Replacing '{search}' with '{replace}' in:[/] {fullname}");
                    continue;
                }

                AnsiConsole.MarkupLine($"[green]Replacing '{search}' with '{replace}' in:[/] {fullname}");
                content = content.Replace(search, replace);
                File.WriteAllText(fullname, content);
            }

            if (found == 0)
            {
                AnsiConsole.MarkupLine($"[yellow]No files found with '{search}'[/]");
                return true;
            }

            if (!write)
            {
                AnsiConsole.MarkupLine($"[green]Found '{search}' in {found} {type} files[/]");
                return true;
            }

            AnsiConsole.MarkupLine($"[green]Found and replaced '{search}' with '{replace}' in {found} {type} files[/]");

            return true;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            return false;
        }
    }

    private static bool CheckChangesToCommit()
    {
        var result = ProcessHelper.StartProcess("git status --porcelain", redirectOutput: true);
        return !string.IsNullOrEmpty(result);
    }

    private static bool CreateChangeCommit(string message, bool write)
    {
        const string gitAddCommand = "git add .";
        var createCommitCommand = $"git commit -m \"{message}\"";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {gitAddCommand}[/]");
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {createCommitCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Adding changes[/]");
        var addResult = ProcessHelper.StartProcess(gitAddCommand);
        Console.WriteLine($"Result: {addResult}");

        AnsiConsole.MarkupLine("[green]Creating a commit[/]");
        var result = ProcessHelper.StartProcess(createCommitCommand);
        Console.WriteLine($"Result: {result}");

        return true;
    }

    private static bool CreatePullRequest(string title, string body, bool write)
    {
        var args = new[]
        {
            "pr",
            "create",
            $"--title \"[PlatformPlatform] {title}\"",
            $"--body \"{body}\"",
            "--assignee", "@me",
            "--base", "main",
            "--web"
        };
        var command = $"gh {string.Join(" ", args)}";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {command.EscapeMarkup()}[/]");
            return false;
        }

        AnsiConsole.MarkupLine("[green]Creating a pull-request[/]");
        var result = ProcessHelper.StartProcess(command);

        Console.WriteLine($"Result: {result}");

        return true;
    }

    private static FileInfo? GetApplicationSolutionFileInfo()
    {
        // Find the solution file in the application folder / working directory
        var solutionFiles = WorkingDirectory.GetFiles("*.sln", SearchOption.TopDirectoryOnly);
        if (solutionFiles.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]Error: Solution file not found[/]");
            return null;
        }

        return solutionFiles[0];
    }

    private static bool CodeCleanupSolution(bool write)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var restoreCommand = "dotnet tool restore";
        var cleanupCommand = $"dotnet jb cleanupcode {SolutionFile.Name} --profile=\".NET only\"";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {restoreCommand}[/]");
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {cleanupCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Restoring tools[/]");
        ProcessHelper.StartProcess(restoreCommand, WorkingDirectory.FullName);

        AnsiConsole.MarkupLine("[green]Running code cleanup[/]");
        ProcessHelper.StartProcess(cleanupCommand, WorkingDirectory.FullName);

        AnsiConsole.MarkupLine("[green]Code cleanup completed. Check Git to see any changes![/]");

        return true;
    }

    private static bool TestSolution(bool write)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var testCommand = $"dotnet test {SolutionFile.Name}";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {testCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Running tests[/]");
        ProcessHelper.StartProcess(testCommand, WorkingDirectory.FullName);

        return true;
    }

    private static bool RunNpmInstall(bool write)
    {
        var npmInstallCommand = "npm install";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {npmInstallCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Running 'npm install'[/]");
        ProcessHelper.StartProcess(npmInstallCommand, WorkingDirectory.FullName);

        return true;
    }

    private static bool RunNpmBuild(bool write)
    {
        var npmBuildCommand = "npm run build";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {npmBuildCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Running 'npm run build'[/]");
        ProcessHelper.StartProcess(npmBuildCommand, WorkingDirectory.FullName);

        return true;
    }

    private static bool RunNpmFormat(bool write)
    {
        var npmFormatCommand = "npm run format";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {npmFormatCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Running 'npm run format'[/]");
        ProcessHelper.StartProcess(npmFormatCommand, WorkingDirectory.FullName);

        return true;
    }

    private static bool RunNpmLint(bool write)
    {
        var npmLintCommand = "npm run lint";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {npmLintCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Running 'npm run lint'[/]");
        ProcessHelper.StartProcess(npmLintCommand, WorkingDirectory.FullName);

        return true;
    }

    private static bool RunNpmCheck(bool write)
    {
        var npmCheckCommand = "npm run check";

        if (!write)
        {
            AnsiConsole.MarkupLine($"[yellow]Dry-run: {npmCheckCommand}[/]");
            return true;
        }

        AnsiConsole.MarkupLine("[green]Running 'npm run check'[/]");
        ProcessHelper.StartProcess(npmCheckCommand, WorkingDirectory.FullName);

        return true;
    }

    private enum SearchReplaceType
    {
        C_SHARP,
        TYPE_SCRIPT,
        CS_PROJECT,
        ES_PROJECT
    }
}

internal record Commit(string Hash, string Message);
