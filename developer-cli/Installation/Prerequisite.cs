using System.Diagnostics;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public abstract record Prerequisite
{
    public static readonly Prerequisite Dotnet = new CommandLineToolPrerequisite("dotnet", "dotnet", new Version(10, 0, 101));
    public static readonly Prerequisite Docker = new CommandLineToolPrerequisite("docker", "Docker", new Version(28, 5, 1));
    public static readonly Prerequisite Node = new NodePrerequisite();
    public static readonly Prerequisite AzureCli = new CommandLineToolPrerequisite("az", "Azure CLI", new Version(2, 79));
    public static readonly Prerequisite GithubCli = new CommandLineToolPrerequisite("gh", "GitHub CLI", new Version(2, 83));
    public static readonly Prerequisite TypeScriptLanguageServer = new CommandLineToolPrerequisite("typescript-language-server", "TypeScript Language Server", new Version(4, 3, 0));

    protected abstract bool IsValid();

    public static void Ensure(params Prerequisite[] prerequisites)
    {
        var failedPrerequisiteCount = prerequisites.Count(p => !p.IsValid());
        if (failedPrerequisiteCount > 0)
        {
            Environment.Exit(1);
        }
    }

    public static void Recommend(params Prerequisite[] prerequisites)
    {
        var missingPrerequisites = prerequisites.Where(p => !p.CheckExists()).ToList();
        if (missingPrerequisites.Count == 0) return;

        AnsiConsole.MarkupLine("[yellow]Optional prerequisites for enhanced Claude Code LSP support:[/]");
        foreach (var prerequisite in missingPrerequisites)
        {
            AnsiConsole.MarkupLine($"[yellow]  - {prerequisite} is not installed[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Install with:[/]");

        if (missingPrerequisites.Any(p => p == TypeScriptLanguageServer))
        {
            AnsiConsole.MarkupLine("[dim]  npm install -g typescript-language-server typescript[/]");
        }

        AnsiConsole.WriteLine();
    }

    protected abstract bool CheckExists();
}

file sealed record CommandLineToolPrerequisite(string Command, string DisplayName, Version MinVersion) : Prerequisite
{
    protected override bool IsValid()
    {
        if (!CheckExists())
        {
            AnsiConsole.MarkupLine($"[red]{DisplayName} of minimum version {MinVersion} should be installed.[/]");
            return false;
        }

        // Get the version of the command line tool
        var output = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = Configuration.IsWindows ? "cmd.exe" : "/bin/bash",
                Arguments = Configuration.IsWindows ? $"/c {Command} --version" : $"-c \"{Command} --version\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }
        );

        var versionRegex = new Regex(@"\d+\.\d+\.\d+(\.\d+)?");
        var match = versionRegex.Match(output);
        if (match.Success)
        {
            var version = Version.Parse(match.Value);
            if (version >= MinVersion) return true;
            AnsiConsole.MarkupLine(
                $"[red]Please update '[bold]{DisplayName}[/]' from version [bold]{version}[/] to [bold]{MinVersion}[/] or later.[/]"
            );

            return false;
        }

        // If the version could not be determined please change the logic here to check for the correct version
        AnsiConsole.MarkupLine(
            $"[red]Command '[bold]{Command}[/]' is installed but version could not be determined. Please update the CLI to check for correct version.[/]"
        );

        return false;
    }

    protected override bool CheckExists()
    {
        var checkOutput = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = Configuration.IsWindows ? "where" : "which",
                Arguments = Command,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            exitOnError: false
        );

        var possibleFileLocations = checkOutput.Split(Environment.NewLine);
        return !string.IsNullOrWhiteSpace(checkOutput) && possibleFileLocations.Length > 0 && File.Exists(possibleFileLocations[0]);
    }

    public override string ToString()
    {
        return DisplayName;
    }
}

file sealed record NodePrerequisite : Prerequisite
{
    protected override bool IsValid()
    {
        var requiredVersion = File.ReadAllText(Path.Combine(Configuration.ApplicationFolder, ".node-version")).Trim();
        var nodeDir = FindNodeBinDirectory(requiredVersion);

        if (nodeDir is null)
        {
            AnsiConsole.MarkupLine($"[red]NodeJS [bold]{requiredVersion}[/] not found. Install it to match .node-version.[/]");
            return false;
        }

        var separator = Configuration.IsWindows ? ";" : ":";
        Environment.SetEnvironmentVariable("PATH", $"{nodeDir}{separator}{Environment.GetEnvironmentVariable("PATH")}");
        return true;
    }

    private static string? FindNodeBinDirectory(string version)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var candidates = Configuration.IsWindows
            ? new[]
            {
                Path.Combine(home, "AppData", "Roaming", "fnm", "node-versions", $"v{version}", "installation"),
                Path.Combine(home, "AppData", "Roaming", "nvm", $"v{version}"),
                Path.Combine(home, ".volta", "tools", "image", "node", version)
            }
            : new[]
            {
                Path.Combine(home, ".local", "share", "fnm", "node-versions", $"v{version}", "installation", "bin"),
                Path.Combine(home, ".nvm", "versions", "node", $"v{version}", "bin"),
                Path.Combine(home, ".volta", "tools", "image", "node", version, "bin")
            };

        return candidates.FirstOrDefault(Directory.Exists);
    }

    protected override bool CheckExists()
    {
        return true;
    }
}
