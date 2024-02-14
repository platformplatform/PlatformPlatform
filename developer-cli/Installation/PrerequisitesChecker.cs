using System.Diagnostics;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Commands;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public record Prerequisite(
    PrerequisiteType Type,
    string Name,
    string? DisplayName = null,
    Version? Version = null,
    string? Regex = null
);

public enum PrerequisiteType
{
    CommandLineTool,
    DotnetWorkload,
    EnvironmentVariable
}

public static class PrerequisitesChecker
{
    private static readonly List<Prerequisite> Dependencies =
    [
        new Prerequisite(PrerequisiteType.CommandLineTool, "docker", "Docker", new Version(24, 0)),
        new Prerequisite(PrerequisiteType.CommandLineTool, "node", "NodeJS", new Version(21, 0)),
        new Prerequisite(PrerequisiteType.CommandLineTool, "yarn", "Yarn", new Version(1, 22)),
        new Prerequisite(PrerequisiteType.CommandLineTool, "az", "Azure CLI", new Version(2, 55)),
        new Prerequisite(PrerequisiteType.CommandLineTool, "gh", "GitHub CLI", new Version(2, 41)),
        new Prerequisite(PrerequisiteType.DotnetWorkload, "aspire", "Aspire", Regex: """aspire\s*8\.0\.0-preview.3"""),
        new Prerequisite(PrerequisiteType.EnvironmentVariable, "SQL_SERVER_PASSWORD"),
        new Prerequisite(PrerequisiteType.EnvironmentVariable, "CERTIFICATE_PASSWORD")
    ];

    public static void Check(params string[] prerequisiteName)
    {
        var invalid = false;
        foreach (var command in prerequisiteName)
        {
            var prerequisite = Dependencies.SingleOrDefault(p => p.Name == command);
            if (prerequisite is null)
            {
                AnsiConsole.MarkupLine($"[red]Unknown prerequisite: {command}[/]");
                invalid = true;
                continue;
            }

            switch (prerequisite.Type)
            {
                case PrerequisiteType.CommandLineTool:
                    if (!IsCommandLineToolValid(prerequisite.Name, prerequisite.DisplayName!, prerequisite.Version!))
                    {
                        invalid = true;
                    }

                    break;
                case PrerequisiteType.DotnetWorkload:
                    if (!IsDotnetWorkloadValid(prerequisite.Name, prerequisite.DisplayName!, prerequisite.Regex!))
                    {
                        invalid = true;
                    }

                    break;
                case PrerequisiteType.EnvironmentVariable:
                    if (!IsEnvironmentVariableSet(prerequisite.Name))
                    {
                        invalid = true;
                    }

                    break;
            }
        }

        if (invalid)
        {
            Environment.Exit(1);
        }
    }

    private static bool IsCommandLineToolValid(string command, string displayName, Version minVersion)
    {
        // Check if the command line tool is installed
        var checkOutput = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = Configuration.IsWindows ? "where" : "which",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        var possibleFileLocations = checkOutput.Split(Environment.NewLine);

        if (string.IsNullOrWhiteSpace(checkOutput) || !possibleFileLocations.Any() ||
            !File.Exists(possibleFileLocations[0]))
        {
            AnsiConsole.MarkupLine(
                $"[red]{displayName} of minimum version {minVersion} should be installed.[/]");

            return false;
        }

        // Get the version of the command line tool
        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = Configuration.IsWindows ? "cmd.exe" : "/bin/bash",
            Arguments = Configuration.IsWindows ? $"/c {command} --version" : $"-c \"{command} --version\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        var versionRegex = new Regex(@"\d+\.\d+\.\d+(\.\d+)?");
        var match = versionRegex.Match(output);
        if (match.Success)
        {
            var version = Version.Parse(match.Value);
            if (version >= minVersion) return true;
            AnsiConsole.MarkupLine(
                $"[red]Please update '[bold]{displayName}[/]' from version [bold]{version}[/] to [bold]{minVersion}[/] or later.[/]");

            return false;
        }

        // If the version could not be determined please change the logic here to check for the correct version
        AnsiConsole.MarkupLine(
            $"[red]Command '[bold]{command}[/]' is installed but version could not be determined. Please update the CLI to check for correct version.[/]");

        return false;
    }

    private static bool IsDotnetWorkloadValid(string workloadName, string displayName, string workloadRegex)
    {
        var output = ProcessHelper.StartProcess("dotnet workload list", redirectOutput: true);

        if (!output.Contains(workloadName))
        {
            AnsiConsole.MarkupLine(
                $"[red].NET '[bold]{displayName}[/]' should be installed. Please run '[bold]dotnet workload update[/]' and then '[bold]dotnet workload install {workloadName}[/]'.[/]");

            return false;
        }

        /*
           The output is on the form:

           Installed Workload Id      Manifest Version                     Installation Source
           -----------------------------------------------------------------------------------
           aspire                     8.0.0-preview.3.24105.21/8.0.100     SDK 8.0.100

           Use `dotnet workload search` to find additional workloads to install.
         */
        var regex = new Regex(workloadRegex);
        var match = regex.Match(output);
        if (!match.Success)
        {
            // If the version could not be determined please change the logic here to check for the correct version
            AnsiConsole.MarkupLine(
                $"[red].NET '[bold]{displayName}[/]' is installed but not in the expected version. Please run '[bold]dotnet workload update[/]'.[/]");

            return false;
        }

        return true;
    }

    private static bool IsEnvironmentVariableSet(string variableName)
    {
        if (Environment.GetEnvironmentVariable(variableName) is not null) return true;


        if (Configuration.IsMacOs)
        {
            var fileContent = File.ReadAllText(Configuration.MacOs.GetShellInfo().ProfilePath);

            if (fileContent.Contains($"export {variableName}"))
            {
                AnsiConsole.MarkupLine(
                    $"[red]'{variableName}' is configured but not available. Please run '[bold]source ~/{Configuration.MacOs.GetShellInfo().ProfileName}[/] and restart the terminal'[/]");
                return false;
            }
        }

        AnsiConsole.MarkupLine(
            $"[red]'{variableName}' is not configured. Please run '[bold]{Configuration.AliasName} {ConfigureDeveloperEnvironmentCommand.CommandName}[/] and restart the terminal'[/]");

        return false;
    }
}