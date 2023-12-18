using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class AliasRegistration
{
    private const string Intro =
        """
        [green]Welcome to:[/]
        To get the full benefit of PlatformPlatform, allow this tool to register an alias on your machine.
        This will allow you to run PlatformPlatform commands from anywhere on your machine by typing [green]pp[/].

        [green]The CLI can be used to:[/]
        * Check if all prerequisites are installed (e.g., Azure CLI, Docker, Bun, .NET Aspire, etc.)
        * Set up secure passwordless continuous deployments between GitHub and Azure
        * Test deploy your application to Azure from your local machine
        * Build self-contained executables for your application
        * Run static code analysis on your codebase to ensure it does not fail when running in GitHub Workflows
        * Run tests and show code coverage reports locally
        * Much more is coming soon!

        [green]Best of all, you can easily create your own commands to automate your own workflows![/]
        The CLI will automatically detect any changes and recompile itself whenever your team does a [bold][grey]git pull[/][/].
        This ensures that you always have the correct version of the CLI that works with the current version of your codebase.

        [green]Is this secure?[/]
        Like any code you copy from the internet, you should always review it before you run it.
        Just open the project in your IDE and review the code.
        But yes, it is secure, and apart from the alias, it does not make any changes to your machine.

        [green]How does it work?[/]
        The Alias is just a shortcut to the CLI tool, in your shell's config file (e.g., .zshrc or .bashrc).
        Each command is just a C# class that can be customized to automate your own workflows.
        To remove the alias, just remove the line from your shell config file.

        """;


    internal static void EnsureAliasIsRegistered()
    {
        if (IsAliasRegistered()) return;

        var figletText = new FigletText("PlatformPlatform").Color(Color.Green);
        AnsiConsole.Write(figletText);
        AnsiConsole.Write(new Markup(Intro));
        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm("This will register the alias '[green]pp[/]', so it will be available everywhere."))
        {
            AnsiConsole.WriteLine();
            RegisterAlias("pp");
        }

        // Kill the current process
        System.Environment.Exit(0);
    }

    private static bool IsAliasRegistered()
    {
        var processName = new FileInfo(System.Environment.ProcessPath!).Name;

        return Environment.IsWindows
            ? Environment.Windows.IsFolderInPath(Environment.PublishFolder)
            : Environment.MacOs.IsAliasRegisteredMacOs(processName);
    }

    private static void RegisterAlias(string aliasName)
    {
        var cliExecutable = Path.Combine(Environment.PublishFolder, new FileInfo(System.Environment.ProcessPath!).Name);

        if (Environment.IsWindows)
        {
            Environment.Windows.AddFolderToPath(Environment.PublishFolder);
        }
        else if (Environment.IsMacOs)
        {
            Environment.MacOs.RegisterAliasMacOs(aliasName, cliExecutable);
        }
    }
}