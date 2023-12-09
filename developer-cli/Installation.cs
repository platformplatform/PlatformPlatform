using System.Runtime.InteropServices;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli;

public static class Installation
{
    public static readonly string SolutionFolder =
        new DirectoryInfo(Environment.ProcessPath!).Parent!.Parent!.Parent!.Parent!.Parent!.FullName;

    public static readonly string PublishFolder =
        Path.Combine(SolutionFolder, "artifacts", "publish", "DeveloperCli", "release");

    internal static void EnsureAliasIsRegistered()
    {
        if (IsAliasRegistered()) return;

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