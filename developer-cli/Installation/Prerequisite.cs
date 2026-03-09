using System.Diagnostics;
using System.Text.RegularExpressions;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Installation;

public abstract record Prerequisite
{
    public static readonly Prerequisite Dotnet = new CommandLineToolPrerequisite("dotnet", "dotnet", new Version(10, 0, 103));
    public static readonly Prerequisite Docker = new CommandLineToolPrerequisite("docker", "Docker", new Version(27, 0, 0));
    public static readonly Prerequisite Node = new NodePrerequisite();
    public static readonly Prerequisite AzureCli = new CommandLineToolPrerequisite("az", "Azure CLI", new Version(2, 79));
    public static readonly Prerequisite GithubCli = new CommandLineToolPrerequisite("gh", "GitHub CLI", new Version(2, 83));
    public static readonly Prerequisite TypeScriptLanguageServer = new CommandLineToolPrerequisite("typescript-language-server", "TypeScript Language Server", new Version(4, 3, 0));
    public static readonly Prerequisite CSharpLanguageServer = new CommandLineToolPrerequisite("csharp-ls", "C# Language Server", new Version(0, 22, 0));

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

        AnsiConsole.MarkupLine("[yellow]Recommended: Install language servers for enhanced Claude Code support:[/]");

        if (missingPrerequisites.Any(p => p == TypeScriptLanguageServer))
        {
            AnsiConsole.MarkupLine("[yellow]  npm install -g typescript-language-server typescript[/]");
        }

        if (missingPrerequisites.Any(p => p == CSharpLanguageServer))
        {
            AnsiConsole.MarkupLine("[yellow]  dotnet tool install -g csharp-ls[/]");
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
        var requiredVersionText = File.ReadAllText(Path.Combine(Configuration.ApplicationFolder, ".node-version")).Trim();
        var requiredVersion = Version.Parse(requiredVersionText);

        if (IsCompatibleVersion(requiredVersion)) return true;

        if (IsFnmInstalled())
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var nodeDir = Configuration.IsWindows
                ? Path.Combine(home, "AppData", "Roaming", "fnm", "node-versions", $"v{requiredVersionText}", "installation")
                : Path.Combine(home, ".local", "share", "fnm", "node-versions", $"v{requiredVersionText}", "installation", "bin");

            if (!Directory.Exists(nodeDir))
            {
                AnsiConsole.MarkupLine($"[yellow]NodeJS [bold]{requiredVersionText}[/] not found. Installing with fnm...[/]");
                ProcessHelper.StartProcess(new ProcessStartInfo
                    {
                        FileName = Configuration.IsWindows ? "cmd.exe" : "/bin/bash",
                        Arguments = Configuration.IsWindows ? $"/c fnm install {requiredVersionText}" : $"-c \"fnm install {requiredVersionText}\"",
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    }
                );
            }

            if (Directory.Exists(nodeDir))
            {
                var separator = Configuration.IsWindows ? ";" : ":";
                Environment.SetEnvironmentVariable("PATH", $"{nodeDir}{separator}{Environment.GetEnvironmentVariable("PATH")}");
            }

            if (IsCompatibleVersion(requiredVersion)) return true;
        }

        AnsiConsole.MarkupLine($"[red]NodeJS [bold]{requiredVersion.Major}.x[/] (>= {requiredVersionText}) not found. Install it to match .node-version.[/]");
        return false;
    }

    private static bool IsCompatibleVersion(Version requiredVersion)
    {
        var output = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = Configuration.IsWindows ? "cmd.exe" : "/bin/bash",
                Arguments = Configuration.IsWindows ? "/c node --version" : "-c \"node --version\"",
                WorkingDirectory = Configuration.ApplicationFolder,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            exitOnError: false
        ).Trim();

        var versionRegex = new Regex(@"\d+\.\d+\.\d+");
        var match = versionRegex.Match(output);
        if (!match.Success) return false;

        var installedVersion = Version.Parse(match.Value);
        return installedVersion.Major == requiredVersion.Major && installedVersion >= requiredVersion;
    }

    private static bool IsFnmInstalled()
    {
        var output = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = Configuration.IsWindows ? "cmd.exe" : "/bin/bash",
                Arguments = Configuration.IsWindows ? "/c fnm --version" : "-c \"fnm --version\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            },
            exitOnError: false
        ).Trim();

        return !string.IsNullOrEmpty(output) && !output.Contains("not found");
    }

    protected override bool CheckExists()
    {
        return true;
    }
}
