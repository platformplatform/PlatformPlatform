using System.Runtime.InteropServices;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class AliasRegistration
{
    private const string Intro =
        """
        To get the full benefit of PlatformPlatform, you can register a command-line alias for this CLI tool.
        This will allow you to run PlatformPlatform commands from anywhere on your machine.
        The alias defaults to [bold][grey]pp[/][/], but if you have multiple projects, you can customize it to something else.

        [green]The CLI can be used to:[/]
        * Set up secure passwordless continuous deployments between GitHub and Azure
        * Test deploy your application to Azure from your local machine
        * Run static code analysis on your codebase to ensure it does not fail when running in GitHub Workflows
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

    public static readonly string SolutionFolder =
        new DirectoryInfo(Environment.ProcessPath!).Parent!.Parent!.Parent!.Parent!.Parent!.FullName;

    public static readonly string PublishFolder =
        Path.Combine(SolutionFolder, "artifacts", "publish", "DeveloperCli", "release");

    internal static void EnsureAliasIsRegistered()
    {
        if (IsAliasRegistered()) return;

        var figletText = new FigletText("PlatformPlatform")
            .Color(Color.Green)
            .Centered();

        AnsiConsole.Write(new Panel(figletText)
            .Expand()
            .SquareBorder()
            .BorderColor(Color.Green)
            .Padding(1, 1, 1, 1)
            .Header("[bold green] Welcome to... [/]")
        );

        AnsiConsole.Write(new Panel(new Markup(Intro).Centered())
            .Expand()
            .SquareBorder()
            .BorderColor(Color.Green)
            .Padding(1, 1, 1, 1)
        );

        var aliasName = AnsiConsole.Prompt(
            new TextPrompt<string>("Enter the command line alias you want to use for calling this CLI tool:")
                .PromptStyle("green")
                .DefaultValue("pp")
                .Validate(result => result.Length >= 2 && result.All(char.IsLetter)
                    ? ValidationResult.Success()
                    : ValidationResult.Error("The alias must be at least two characters and only contain letters."))
                .AllowEmpty()
        );

        RegisterAlias(aliasName);

        // Kill the current process 
        Environment.Exit(0);
    }

    private static bool IsAliasRegistered()
    {
        var processName = new FileInfo(Environment.ProcessPath!).Name;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return MacOs.IsAliasRegistered(processName);
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AnsiConsole.MarkupLine("[red]Windows is not supported yet[/]");
            return false;
        }

        AnsiConsole.MarkupLine($"[red]Your OS [bold]{Environment.OSVersion.Platform}[/] is not supported.[/]");
        return false;
    }

    private static void RegisterAlias(string aliasName)
    {
        var cliExecutable = Path.Combine(PublishFolder, new FileInfo(Environment.ProcessPath!).Name);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            MacOs.RegisterAlias(aliasName, cliExecutable);
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AnsiConsole.MarkupLine("[red]Windows is not supported yet[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[red]Your OS [bold]{Environment.OSVersion.Platform}[/] is not supported.[/]");
    }

    private static class MacOs
    {
        internal static bool IsAliasRegistered(string processName)
        {
            var shellInfo = GetMacOsShellInfo();
            if (!File.Exists(shellInfo.ProfilePath))
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{shellInfo.ShellName}[/] is not supported.[/]");
                return false;
            }

            var isAliasRegistered = File.ReadAllLines(shellInfo.ProfilePath).Any(line =>
                line.StartsWith("alias ") &&
                line.Contains(SolutionFolder) &&
                line.Contains(processName)
            );
            return isAliasRegistered;
        }

        internal static void RegisterAlias(string aliasName, string cliExecutable)
        {
            var shellInfo = GetMacOsShellInfo();
            if (shellInfo.ProfileName == string.Empty)
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{shellInfo.ShellName}[/] is not supported.[/]");
                return;
            }

            var lines = File.ReadAllLines(shellInfo.ProfilePath);
            if (lines.Any(line => line.Contains($"alias {aliasName}=")))
            {
                AnsiConsole.MarkupLine(
                    $"Alias [red]{aliasName}[/] already exist in [red]{shellInfo.ProfileName}[/].");
                return;
            }

            AnsiConsole.MarkupLine($"Registering alias [green]{aliasName}[/] in [green]{shellInfo.ProfileName}[/].");
            File.AppendAllText(shellInfo.ProfilePath, $"alias {aliasName}='{cliExecutable}'{Environment.NewLine}");
            AnsiConsole.MarkupLine($"Please restart your terminal or run [green]source ~/{shellInfo.ProfileName}[/]");
        }

        private static (string ShellName, string ProfileName, string ProfilePath) GetMacOsShellInfo()
        {
            var shellName = Environment.GetEnvironmentVariable("SHELL")!;
            var profileName = string.Empty;

            if (shellName.Contains("zsh"))
            {
                profileName = ".zshrc";
            }
            else if (shellName.Contains("bash"))
            {
                profileName = ".bashrc";
            }

            var profilePath = profileName == string.Empty
                ? string.Empty
                : Path.Combine(Environment.GetEnvironmentVariable("HOME")!, profileName);

            return (shellName, profileName, profilePath);
        }
    }
}