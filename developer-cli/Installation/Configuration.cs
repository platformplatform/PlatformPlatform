using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class Configuration
{
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static readonly bool IsMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    private static readonly string UserFolder =
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static readonly string LocalhostPfx = IsWindows ? Windows.LocalhostPfxWindows : MacOs.LocalhostPfxMacOs;

    public static readonly string PublishFolder = IsWindows
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PlatformPlatform")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".PlatformPlatform");

    private static string ConfigFile => Path.Combine(PublishFolder, $"{AliasRegistration.AliasName}.json");

    public static bool VerboseLogging { get; set; }

    public static string GetSourceCodeFolder()
    {
        if (Environment.ProcessPath!.Contains("debug"))
        {
            // In debug mode the ProcessPath is in developer-cli/artifacts/bin/DeveloperCli/debug/pp.exe
            return new DirectoryInfo(Environment.ProcessPath).Parent!.Parent!.Parent!.Parent!.Parent!.FullName;
        }

        return GetConfigurationSetting().SourceCodeFolder!;
    }

    public static ConfigurationSetting GetConfigurationSetting()
    {
        if (!File.Exists(ConfigFile))
        {
            return new ConfigurationSetting();
        }

        try
        {
            var readAllText = File.ReadAllText(ConfigFile);
            return JsonSerializer.Deserialize<ConfigurationSetting>(readAllText)!;
        }
        catch (Exception)
        {
            AnsiConsole.MarkupLine(
                $"[red]Reading configuration. Please delete the folder {PublishFolder} and reinstall the CLI.[/]");
            Environment.Exit(1);
            throw;
        }
    }

    public static void SaveConfigurationSetting(ConfigurationSetting configurationSetting)
    {
        var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
        var configuration = JsonSerializer.Serialize(configurationSetting, jsonSerializerOptions);
        File.WriteAllText(ConfigFile, configuration);
    }

    public static class Windows
    {
        public static readonly string LocalhostPfxWindows = $"{UserFolder}/.aspnet/https/localhost.pfx";

        internal static bool IsFolderInPath(string path)
        {
            var paths = Environment.GetEnvironmentVariable("PATH")!.Split(';');
            return paths.Contains(path);
        }

        public static void AddFolderToPath(string publishFolder)
        {
            var arguments = $"/c setx PATH \"%PATH%;{publishFolder}\"";
            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });
        }
    }

    public static class MacOs
    {
        public static readonly string LocalhostPfxMacOs = $"{UserFolder}/.aspnet/https/localhost.pfx";

        internal static bool IsAliasRegisteredMacOs(string processName)
        {
            if (!File.Exists(GetShellInfo().ProfilePath))
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{GetShellInfo().ShellName}[/] is not supported.[/]");
                return false;
            }

            return Array.Exists(File.ReadAllLines(GetShellInfo().ProfilePath), line =>
                line.StartsWith("alias ") &&
                line.Contains(PublishFolder) &&
                line.Contains(processName)
            );
        }

        internal static void RegisterAliasMacOs(string aliasName, string filename)
        {
            if (!File.Exists(GetShellInfo().ProfilePath))
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{GetShellInfo().ShellName}[/] is not supported.[/]");
                return;
            }

            File.AppendAllText(GetShellInfo().ProfilePath,
                $"alias {aliasName}='{filename}'{Environment.NewLine}");
            AnsiConsole.MarkupLine(
                $"Please restart your terminal or run [green]source ~/{GetShellInfo().ProfileName}[/]");
        }

        public static (string ShellName, string ProfileName, string ProfilePath) GetShellInfo()
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

            var profilePath = profileName == string.Empty ? string.Empty : Path.Combine(UserFolder, profileName);

            return (shellName, profileName, profilePath);
        }
    }
}

public class ConfigurationSetting
{
    public string? SourceCodeFolder { get; set; }

    public string? Hash { get; set; }
}