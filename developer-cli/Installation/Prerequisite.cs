using System.Diagnostics;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public abstract record Prerequisite
{
    public static readonly Prerequisite Dotnet = new CommandLineToolPrerequisite("dotnet", "dotnet", new Version(10, 0, 101));
    public static readonly Prerequisite Docker = new CommandLineToolPrerequisite("docker", "Docker", new Version(28, 5, 1));
    public static readonly Prerequisite Node = new CommandLineToolPrerequisite("node", "NodeJS", new Version(24, 10, 0));
    public static readonly Prerequisite AzureCli = new CommandLineToolPrerequisite("az", "Azure CLI", new Version(2, 79));
    public static readonly Prerequisite GithubCli = new CommandLineToolPrerequisite("gh", "GitHub CLI", new Version(2, 83));

    protected abstract bool IsValid();

    public static void Ensure(params Prerequisite[] prerequisites)
    {
        var failedPrerequisiteCount = prerequisites.Count(p => !p.IsValid());
        if (failedPrerequisiteCount > 0)
        {
            Environment.Exit(1);
        }
    }
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
}
