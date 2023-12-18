using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class MacOs
{
    public static readonly (string ShellName, string ProfileName, string ProfilePath, string UserFolder) ShellInfo =
        GetShellInfo();

    public static readonly string LocalhostPfx = $"{ShellInfo.UserFolder}/.aspnet/https/localhost.pfx";

    internal static bool IsAliasRegisteredMacOs(string processName)
    {
        if (!File.Exists(ShellInfo.ProfilePath))
        {
            AnsiConsole.MarkupLine($"[red]Your shell [bold]{ShellInfo.ShellName}[/] is not supported.[/]");
            return false;
        }

        var isAliasRegistered = Array.Exists(File.ReadAllLines(ShellInfo.ProfilePath), line =>
            line.StartsWith("alias ") &&
            line.Contains(AliasRegistration.SolutionFolder) &&
            line.Contains(processName)
        );
        return isAliasRegistered;
    }

    internal static void RegisterAliasMacOs(string aliasName, string cliExecutable)
    {
        if (ShellInfo.ProfileName == string.Empty)
        {
            AnsiConsole.MarkupLine($"[red]Your shell [bold]{ShellInfo.ShellName}[/] is not supported.[/]");
            return;
        }

        File.AppendAllText(ShellInfo.ProfilePath, $"alias {aliasName}='{cliExecutable}'{Environment.NewLine}");
        AnsiConsole.MarkupLine($"Please restart your terminal or run [green]source ~/{ShellInfo.ProfileName}[/]");
    }

    private static (string ShellName, string ProfileName, string ProfilePath, string UserFolder) GetShellInfo()
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

        var userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return (shellName, profileName, profilePath, userFolder);
    }
}