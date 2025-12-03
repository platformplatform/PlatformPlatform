using System.CommandLine;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class InitTaskManagerCommand : Command
{
    public InitTaskManagerCommand() : base(
        "init-task-manager",
        "Initialize task-manager directory in .workspace as a separate git repository"
    )
    {
        SetAction(_ => Execute());
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

        var workspaceDirectory = Path.Combine(repositoryRoot, ".workspace");
        Directory.CreateDirectory(workspaceDirectory);

        var taskManagerPath = Path.Combine(workspaceDirectory, "task-manager");

        if (Directory.Exists(taskManagerPath))
        {
            AnsiConsole.MarkupLine("[green]Task-manager already initialized[/]");
            return;
        }

        AnsiConsole.MarkupLine("[blue]Creating task-manager directory...[/]");
        CreateTaskManagerDirectory(taskManagerPath);
        InitializeGitRepository(taskManagerPath);
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
}
