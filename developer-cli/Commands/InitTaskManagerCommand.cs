using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class InitTaskManagerCommand : Command
{
    public InitTaskManagerCommand() : base(
        "init-task-manager",
        "Initialize task-manager as a git submodule that's ignored by the main repository"
    )
    {
        Handler = CommandHandler.Create(Execute);
    }

    private static void Execute()
    {
        var repositoryRoot = Configuration.SourceCodeFolder;
        var gitPath = Path.Combine(repositoryRoot, ".git");

        if (!Directory.Exists(gitPath))
        {
            AnsiConsole.MarkupLine($"[red]Error: Not a git repository: {repositoryRoot}[/]");
            Environment.Exit(1);
        }

        var taskManagerPath = Path.Combine(repositoryRoot, "task-manager");

        if (Directory.Exists(taskManagerPath))
        {
            AnsiConsole.MarkupLine("[green]Task-manager already initialized[/]");
            return;
        }

        AnsiConsole.MarkupLine("[blue]Creating task-manager directory...[/]");
        CreateTaskManagerDirectory(taskManagerPath);
        InitializeGitRepository(taskManagerPath);
        EnsureGitExclude(repositoryRoot);
        AnsiConsole.MarkupLine("[green]Task-manager initialized successfully[/]");
    }

    private static void CreateTaskManagerDirectory(string taskManagerPath)
    {
        Directory.CreateDirectory(taskManagerPath);
    }

    private static void InitializeGitRepository(string taskManagerPath)
    {
        ProcessHelper.StartProcess("git init -b main", taskManagerPath);
    }

    private static void EnsureGitExclude(string repositoryRoot)
    {
        var gitInfoPath = Path.Combine(repositoryRoot, ".git", "info");
        if (!Directory.Exists(gitInfoPath))
        {
            Directory.CreateDirectory(gitInfoPath);
        }

        var excludePath = Path.Combine(gitInfoPath, "exclude");
        var excludeContent = File.Exists(excludePath) ? File.ReadAllText(excludePath) : string.Empty;

        if (!excludeContent.Contains("/task-manager/"))
        {
            File.AppendAllText(excludePath, "\n/task-manager/\n");
        }
    }
}
