using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class Configuration
{
    public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public static readonly bool IsMacOs = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

    private static readonly string UserFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public static readonly string AliasName = Assembly.GetExecutingAssembly().GetName().Name!;

    public static readonly string PublishFolder = IsWindows
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PlatformPlatform")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".PlatformPlatform");

    public static readonly string SourceCodeFolder = IsDebugMode
        // In debug mode, the ProcessPath is in /developer-cli/artifacts/bin/DeveloperCli/debug/pp.exe
        ? new DirectoryInfo(Environment.ProcessPath!).Parent!.Parent!.Parent!.Parent!.Parent!.Parent!.FullName
        : new DirectoryInfo(GetConfigurationSetting().CliSourceCodeFolder!).Parent!.FullName;

    public static readonly string ApplicationFolder = new(Path.Combine(SourceCodeFolder, "application"));

    public static readonly string CliFolder = new(Path.Combine(SourceCodeFolder, "developer-cli"));

    public static bool IsDebugMode => Environment.ProcessPath!.Contains("debug");

    private static string ConfigFile => Path.Combine(PublishFolder, $"{AliasName}.json");

    public static readonly string FrontendDevServerFlagFile = Path.Combine(PublishFolder, $"{AliasName}-frontend-dev-server-running");

    public static bool VerboseLogging { get; set; }

    public static bool AutoConfirm { get; set; }

    public static ConfigurationSetting GetConfigurationSetting()
    {
        if (!File.Exists(ConfigFile) && IsDebugMode)
        {
            return new ConfigurationSetting { CliSourceCodeFolder = CliFolder };
        }

        try
        {
            var readAllText = File.ReadAllText(ConfigFile);
            var configurationSetting = JsonSerializer.Deserialize<ConfigurationSetting>(readAllText)!;

            if (configurationSetting.IsValid) return configurationSetting;
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Error: {e.Message}[/]");
        }

        if (IsDebugMode)
        {
            Directory.Delete(PublishFolder, true);
            AnsiConsole.MarkupLine($"[red]Invalid configuration. The {PublishFolder} has been deleted. Please try again.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Invalid configuration. Please run `dotnet run` from the `/developer-cli` folder of PlatformPlatform.[/]");
        }

        Environment.Exit(1);
        return null;
    }

    public static void SaveConfigurationSetting(ConfigurationSetting configurationSetting)
    {
        if (!configurationSetting.IsValid)
        {
            throw new ArgumentException("Invalid configuration setting", nameof(configurationSetting));
        }

        var jsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true };
        var configuration = JsonSerializer.Serialize(configurationSetting, jsonSerializerOptions);
        File.WriteAllText(ConfigFile, configuration);
    }

    public static class Windows
    {
        private const char PathDelimiter = ';';
        private const string PathName = "PATH";
        public static readonly string LocalhostPfxWindows = $"{UserFolder}/.aspnet/https/localhost.pfx";

        internal static bool IsFolderInPath(string folder)
        {
            var paths = Environment.GetEnvironmentVariable(PathName)!.Split(PathDelimiter);
            return paths.Contains(folder);
        }

        public static void AddFolderToPath(string folder)
        {
            if (IsFolderInPath(folder)) return;
            var existingPath = Environment.GetEnvironmentVariable(PathName)!;
            var newPath = existingPath.EndsWith(PathDelimiter)
                ? $"{existingPath}{folder}{PathDelimiter}"
                : $"{existingPath}{PathDelimiter}{folder}{PathDelimiter}";

            Environment.SetEnvironmentVariable(PathName, newPath, EnvironmentVariableTarget.User);
        }

        public static void RemoveFolderFromPath(string folder)
        {
            // Get existing PATH on Windows
            var existingPath = Environment.GetEnvironmentVariable(PathName);

            // Remove the from the PATH environment variable and replace any double ;; left behind
            var newPath = existingPath!.Replace(folder, string.Empty).Replace(";;", ";");

            Environment.SetEnvironmentVariable(PathName, newPath, EnvironmentVariableTarget.User);
        }
    }

    public static class MacOs
    {
        private static string CliPath => Path.Combine(PublishFolder, new FileInfo(Environment.ProcessPath!).Name);

        private static string AliasLineRepresentation => $"alias {AliasName}='{CliPath}'";

        internal static bool IsAliasRegisteredMacOs()
        {
            if (string .IsNullOrEmpty(GetShellInfo().ProfilePath))
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{GetShellInfo().ShellName}[/] is not supported.[/]");
                return false;
            }

            return Array.Exists(File.ReadAllLines(GetShellInfo().ProfilePath), line => line == AliasLineRepresentation);
        }

        internal static void RegisterAliasMacOs()
        {
            if (!File.Exists(GetShellInfo().ProfilePath))
            {
                AnsiConsole.MarkupLine($"[red]Your shell [bold]{GetShellInfo().ShellName}[/] is not supported.[/]");
                return;
            }

            File.AppendAllLines(GetShellInfo().ProfilePath, [AliasLineRepresentation]);
        }

        public static void DeleteAlias()
        {
            var lineRepresentation = AliasLineRepresentation;
            var profilePath = GetShellInfo().ProfilePath;
            var tempFilePath = profilePath + ".tmp";
            var linesToKeep = File.ReadLines(profilePath).Where(l => !l.Contains(lineRepresentation)).ToArray();
            File.WriteAllLines(tempFilePath, linesToKeep);
            File.Replace(tempFilePath, profilePath, null);
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
    public string? CliSourceCodeFolder { get; set; }

    public string? Hash { get; set; }

    [JsonIgnore]
    public bool IsValid
    {
        get
        {
            if (CliSourceCodeFolder is null) return false;

            return !string.IsNullOrEmpty(Hash);
        }
    }
}
