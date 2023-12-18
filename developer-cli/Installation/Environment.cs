using System.Diagnostics;
using System.Runtime.InteropServices;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class Environment
{
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static readonly bool IsMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    private static readonly string UserFolder =
        System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);

    public static readonly string SolutionFolder =
        new DirectoryInfo(System.Environment.ProcessPath!).Parent!.Parent!.Parent!.Parent!.Parent!.FullName;

    public static readonly string PublishFolder =
        Path.Combine(SolutionFolder, "artifacts", "publish", "DeveloperCli", "release");

    public static class Windows
    {
        public static readonly string LocalhostPfx = $"{UserFolder}/.aspnet/https/localhost.pfx";

        internal static bool IsFolderInPath(string path)
        {
            var paths = System.Environment.GetEnvironmentVariable("PATH")!.Split(';');
            return paths.Contains(path);
        }

        public static void AddFolderToPath(string publishFolder)
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c setx PATH \"%PATH%;{publishFolder}\"",
                RedirectStandardOutput = true,
                CreateNoWindow = true
            })!;
            process.WaitForExit();
        }
    }

    public static class MacOs
    {
        public static readonly (string ShellName, string ProfileName, string ProfilePath) ShellInfo = GetShellInfo();

        public static readonly string LocalhostPfx = $"{UserFolder}/.aspnet/https/localhost.pfx";

        internal static bool IsAliasRegisteredMacOs(string processName)
        {
            if (!File.Exists(ShellInfo.ProfilePath))
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{ShellInfo.ShellName}[/] is not supported.[/]");
                return false;
            }

            return Array.Exists(File.ReadAllLines(ShellInfo.ProfilePath), line =>
                line.StartsWith("alias ") &&
                line.Contains(SolutionFolder) &&
                line.Contains(processName)
            );
        }

        internal static void RegisterAliasMacOs(string aliasName, string filename)
        {
            if (!File.Exists(ShellInfo.ProfilePath))
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{ShellInfo.ShellName}[/] is not supported.[/]");
                return;
            }

            File.AppendAllText(ShellInfo.ProfilePath, $"alias {aliasName}='{filename}'{System.Environment.NewLine}");
            AnsiConsole.MarkupLine($"Please restart your terminal or run [green]source ~/{ShellInfo.ProfileName}[/]");
        }

        private static (string ShellName, string ProfileName, string ProfilePath) GetShellInfo()
        {
            var shellName = System.Environment.GetEnvironmentVariable("SHELL")!;
            var profileName = string.Empty;

            if (shellName.Contains("zsh"))
            {
                profileName = ".zshrc";
            }
            else if (shellName.Contains("bash"))
            {
                profileName = ".bashrc";
            }

            var profilePath = profileName == string.Empty ? string.Empty : Path.Combine(UserFolder, profileName);

            return (shellName, profileName, profilePath);
        }
    }
}