using System.CommandLine;
using System.Reflection;
using PlatformPlatform.DeveloperCli.Commands;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class AliasRegistration
{
    public static readonly string AliasName = Assembly.GetExecutingAssembly().GetName().Name!;

    private static readonly string Intro =
        $"""
         [green]Welcome to:[/]
         To get the full benefit of PlatformPlatform, allow this tool to register an alias on your machine.
         This will allow you to run PlatformPlatform commands from anywhere on your machine by typing [green]{AliasName}[/].

         [green]The CLI can be used to:[/]
         * Start all PlatformPlatform services locally in one command
         * Be guided through setting up secure passwordless continuous deployments between GitHub and Azure
         * Test deploy your application to Azure from your local machine
         * Run static code analysis on your codebase to ensure it does not fail when running in GitHub Workflows
         * Run tests and show code coverage reports locally
         * Much more is coming soon!

         [green]Best of all, you can easily create your own commands to automate your own workflows![/]
         The CLI will automatically detect any changes and recompile itself whenever your team does a [bold][grey]git pull[/][/].
         It's a great way to automate workflows and share them with your team.

         [green]Is this secure?[/]
         Like any code you copy from the internet, you should always review it before you run it.
         Just open the project in your IDE and review the code.

         [green]How does it work?[/]
         The CLI has several commands that you can run from anywhere on your machine.
         Each command is one C# class that can be customized to automate your own workflows.
         Each command check for its prerequisites (e.g., Docker, Node, Yarn, .NET Aspire, Azure CLI, etc.)
         To remove the alias, just run [green]{AliasName} uninstall[/].

         """;

    internal static void EnsureAliasIsRegistered()
    {
        if (IsAliasRegistered()) return;

        AnsiConsole.Write(new Markup(Intro));
        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm(
                $"This will register the alias '[green]{AliasName}[/]', so it will be available everywhere."))
        {
            AnsiConsole.WriteLine();
            RegisterAlias();
        }

        if (AnsiConsole.Confirm(
                "PlatformPlatform requires a self-signed certificate and a SQL Server password. Do you want to create them now?"))
        {
            AnsiConsole.WriteLine();
            var command = new ConfigureDeveloperEnvironmentCommand();
            command.Invoke(Array.Empty<string>());
        }

        if (Configuration.IsWindows)
        {
            AnsiConsole.MarkupLine(
                "Please restart your terminal to update your PATH and environment variables.");
        }
        else
        {
            AnsiConsole.MarkupLine(
                $"Please restart your terminal to update your PATH and environment variables (or run [green]source ~/{Configuration.MacOs.GetShellInfo().ProfileName}[/]).");
        }

        // Kill the current process
        Environment.Exit(0);
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
        else if (Configuration.IsMacOs)
        {
            Configuration.MacOs.RegisterAliasMacOs();
        }
    }
}