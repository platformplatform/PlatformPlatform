using System.CommandLine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Commands;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class PrerequisitesChecker
{
    public static async void EnsurePrerequisitesAreMet(string[] args)
    {
        CheckCommandLineTool("docker", "Docker", new Version(24, 0));
        CheckCommandLineTool("az", "Azure CLI", new Version(2, 55));
        CheckCommandLineTool("yarn", "Yarn", new Version(1, 22));
        CheckCommandLineTool("node", "NodeJS", new Version(20, 0));
        CheckDotnetWorkload("aspire", "Aspire", """aspire\s*8\.0\.0-preview.2""");

        if (args.Contains(ConfigureDeveloperEnvironment.CommandName))
        {
            // If we are configuring the environment we don't need to check if configuring the environment is needed
            return;
        }

        // If Environment variables are set but not sourced exit hard with a message
        EnsureEnvironmentVariableIsConfigured("SQL_SERVER_PASSWORD");
        EnsureEnvironmentVariableIsConfigured("CERTIFICATE_PASSWORD");

        var sqlPasswordConfigured = System.Environment.GetEnvironmentVariable("SQL_SERVER_PASSWORD") is not null;
        if (!sqlPasswordConfigured)
        {
            AnsiConsole.MarkupLine("[yellow]SQL_SERVER_PASSWORD environment variable is not set.[/]");
        }

        var hasValidDeveloperCertificate = ConfigureDeveloperEnvironment.HasValidDeveloperCertificate();

        if (!sqlPasswordConfigured || !hasValidDeveloperCertificate)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]Running 'pp {ConfigureDeveloperEnvironment.CommandName}' to configure your environment.[/]");
            AnsiConsole.WriteLine();

            var command = new ConfigureDeveloperEnvironment();
            await command.InvokeAsync(Array.Empty<string>());
        }
    }

    public static void CheckCommandLineTool(
        string command,
        string displayName,
        Version minVersion,
        bool isRequired = false
    )
    {
        // Check if the command line tool is installed
        var checkOutput = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = Environment.IsWindows ? "where" : "which",
            Arguments = command,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        var outputMessageColor = isRequired ? "red" : "yellow";

        var possibleFileLocations = checkOutput.Split(System.Environment.NewLine);

        if (string.IsNullOrWhiteSpace(checkOutput) || !possibleFileLocations.Any() ||
            !File.Exists(possibleFileLocations[0]))
        {
            AnsiConsole.MarkupLine(
                $"[{outputMessageColor}]{displayName} of minimum version {minVersion} should be installed.[/]");

            ExitIfRequired(isRequired);
            return;
        }

        // Get the version of the command line tool
        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = Environment.IsWindows ? "cmd.exe" : "/bin/bash",
            Arguments = Environment.IsWindows ? $"/c {command} --version" : $"-c \"{command} --version\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        var versionRegex = new Regex(@"\d+\.\d+\.\d+(\.\d+)?");
        var match = versionRegex.Match(output);
        if (match.Success)
        {
            var version = Version.Parse(match.Value);
            if (version >= minVersion) return;
            AnsiConsole.MarkupLine(
                $"[{outputMessageColor}]Please update '[bold]{displayName}[/]' from version [bold]{version}[/] to [bold]{minVersion}[/] or later.[/]");

            ExitIfRequired(isRequired);
            return;
        }

        // If the version could not be determined please change the logic here to check for the correct version
        AnsiConsole.MarkupLine(
            $"[{outputMessageColor}]Command '[bold]{command}[/]' is installed but version could not be determined. Please update the CLI to check for correct version.[/]");

        ExitIfRequired(isRequired);
    }

    public static void CheckDotnetWorkload(
        string workloadName,
        string displayName,
        string workloadRegex,
        bool isRequired = false
    )
    {
        var output = ProcessHelper.StartProcess("dotnet workload list", redirectOutput: true);

        var outputMessageColor = isRequired ? "red" : "yellow";

        if (!output.Contains(workloadName))
        {
            AnsiConsole.MarkupLine(
                $"[{outputMessageColor}].NET '[bold]{displayName}[/]' should be installed. Please run '[bold]dotnet workload update[/]' and then '[bold]dotnet workload install {workloadName}[/]'.[/]");

            ExitIfRequired(isRequired);
            return;
        }

        /*
           The output is on the form:

           Installed Workload Id      Manifest Version                     Installation Source
           -----------------------------------------------------------------------------------
           aspire                     8.0.0-preview.2.23619.3/8.0.100      SDK 8.0.100

           Use `dotnet workload search` to find additional workloads to install.
         */
        var regex = new Regex(workloadRegex);
        var match = regex.Match(output);
        if (!match.Success)
        {
            // If the version could not be determined please change the logic here to check for the correct version
            AnsiConsole.MarkupLine(
                $"[{outputMessageColor}].NET '[bold]{displayName}[/]' is installed but not in the expected version. Please run '[bold]dotnet workload update[/]'.[/]");

            ExitIfRequired(isRequired);
        }
    }

    private static void ExitIfRequired(bool isRequired)
    {
        if (isRequired)
        {
            System.Environment.Exit(1);
        }
    }

    private static void EnsureEnvironmentVariableIsConfigured(string variableName)
    {
        if (System.Environment.GetEnvironmentVariable(variableName) is not null) return;

        if (Environment.IsWindows) return;

        var fileContent = File.ReadAllText(Environment.MacOs.GetShellInfo().ProfilePath);
        if (!fileContent.Contains($"export {variableName}")) return;

        AnsiConsole.MarkupLine(
            $"[red]'{variableName}' is configured but not available. Please run '[bold]source ~/{Environment.MacOs.GetShellInfo().ProfileName}[/] and restart the terminal'[/]");
        System.Environment.Exit(0);
    }
}