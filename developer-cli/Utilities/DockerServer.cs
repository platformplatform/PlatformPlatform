using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

internal class DockerServer(string imageName, string instanceName, int? port, string? volume)
{
    public void StartServer()
    {
        if (!DockerImageExists())
        {
            AnsiConsole.MarkupLine($"[green]Pulling {imageName} Docker Image.[/]");
            ProcessHelper.StartProcess("docker", $"pull {imageName}");
        }

        AnsiConsole.MarkupLine($"[green]Starting {instanceName} server.[/]");
        var portArguments = port.HasValue ? $"-p {port}:{port}" : "";
        var volumeArguments = volume is not null ? $"-v {instanceName}:{volume}" : "";
        var output = ProcessHelper.StartProcess(
            "docker",
            $"run -d {portArguments} {volumeArguments} --name {instanceName} {imageName}",
            redirectOutput: true
        );

        if (output.Contains("Error"))
        {
            throw new InvalidOperationException($"Failed to start {instanceName} server. {output}");
        }
    }

    public void StopServer()
    {
        AnsiConsole.MarkupLine($"[green]Stopping {instanceName} server.[/]");
        var output = ProcessHelper.StartProcess("docker", $"rm --force {instanceName}", redirectOutput: true);
        if (output.Contains("Error"))
        {
            AnsiConsole.MarkupLine($"[red]Failed to stop {instanceName} server. {output}[/]");
        }
    }

    private bool DockerImageExists()
    {
        AnsiConsole.MarkupLine("[green]Checking for existing Docker image.[/]");
        var output = ProcessHelper.StartProcess("docker", $"image inspect {imageName}", redirectOutput: true);
        return output.Contains("Digest");
    }
}