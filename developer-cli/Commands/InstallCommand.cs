using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class InstallCommand : Command
{
    private static readonly string Intro =
        $"""
         [green]Welcome to:[/]
         To get the full benefit of PlatformPlatform, allow this tool to register an alias on your machine.
         This will allow you to run PlatformPlatform commands from anywhere on your machine by typing [green]{Configuration.AliasName}[/].

         [green]The CLI can be used to:[/]
         * Start all PlatformPlatform services locally in one command
         * Be guided through setting up secure passwordless continuous deployments between GitHub and Azure
         * Run static code analysis on your codebase to ensure it does not fail when running in GitHub Workflows
         * Run tests and show code coverage reports locally
         * Much more is coming soon!

         [green]Best of all, you can easily create your own commands to automate your own workflows![/]
         The CLI will automatically detect any changes and recompile itself whenever your team does a [bold][grey]git pull[/][/].
         It's a great way to automate workflows and share them with your team.

         [green]Is this secure?[/]
         Yes. But, like any code you copy from the internet, you should always review it before you run it.
         Just open the project in your IDE and review the code.

         [green]How does it work?[/]
         The CLI has several commands that you can run from anywhere on your machine.
         Each command is one C# class that can be customized to automate your own workflows.
         Each command check for its prerequisites (e.g., Docker, Node, .NET Aspire, Azure CLI, etc.)
         To remove the alias, just run [green]{Configuration.AliasName} uninstall[/].

         """;

    public InstallCommand() : base(
        "install",
        $"This will register the alias {Configuration.AliasName} so it will be available everywhere."
    )
    {
        Handler = CommandHandler.Create(Execute);
    }

    private void Execute()
    {
        PrerequisitesChecker.Check("dotnet");

        if (IsAliasRegistered())
        {
            AnsiConsole.MarkupLine($"[yellow]The CLI is already installed please run {Configuration.AliasName} to use it.[/]");
            return;
        }

        AnsiConsole.Write(new Markup(Intro));
        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm($"This will register the alias '[green]{Configuration.AliasName}[/]', so it will be available everywhere."))
        {
            AnsiConsole.WriteLine();
            RegisterAlias();
        }

        if (Configuration.IsWindows)
        {
            AnsiConsole.MarkupLine("Please restart your terminal to update your PATH.");
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"Please restart your terminal to update your PATH (or run [green]source ~/{Configuration.MacOs.GetShellInfo().ProfileName}[/])."
            );
        }
    }

    private static bool IsAliasRegistered()
    {
        return Configuration.IsWindows
            ? Configuration.Windows.IsFolderInPath(Configuration.PublishFolder)
            : Configuration.MacOs.IsAliasRegisteredMacOs();
    }

    private static void RegisterAlias()
    {
        if (Configuration.IsWindows)
        {
            Configuration.Windows.AddFolderToPath(Configuration.PublishFolder);
        }
        else if (Configuration.IsMacOs || Configuration.IsLinux)
        {
            Configuration.MacOs.RegisterAliasMacOs();
        }
    }
}
