using System.Diagnostics;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

internal class DockerServer(string imageName, string instanceName, int? port, string? volume)
{
    public void StartServer()
    {
        if (!DockerImageExists())
        {
            AnsiConsole.MarkupLine($"[green]Pulling {imageName} Docker Image.[/]");
            ProcessHelper.StartProcess(new ProcessStartInfo { FileName = "docker", Arguments = $"pull {imageName}" });
        }

        AnsiConsole.MarkupLine($"[green]Starting {instanceName} server.[/]");
        var portArguments = port.HasValue ? $"-p {port}:{port}" : "";
        var volumeArguments = volume is not null ? $"-v {instanceName}:{volume}" : "";
        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"run -d {portArguments} {volumeArguments} --name {instanceName} {imageName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (output.Contains("Error"))
        {
            throw new InvalidOperationException($"Failed to start {instanceName} server. {output}");
        }
    }

    public void StopServer()
    {
        AnsiConsole.MarkupLine($"[green]Stopping {instanceName} server.[/]");

        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"rm --force {instanceName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (output.Contains("Error"))
        {
            AnsiConsole.MarkupLine($"[red]Failed to stop {instanceName} server. {output}[/]");
        }
    }

    private bool DockerImageExists()
    {
        AnsiConsole.MarkupLine("[green]Checking for existing Docker image.[/]");

        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"image inspect {imageName}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        return output.Contains("Digest");
    }
}