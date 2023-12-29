using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

internal class DockerServer : IDisposable
{
    private readonly DockerServerOptions _options;
    private readonly string _serverName;

    public DockerServer(DockerServerOptions options)
    {
        _options = options;
        _serverName = $"{_options.InstanceName} server";

        try
        {
            StartServer();
        }
        catch
        {
            StopServer();
        }
    }

    public void Dispose()
    {
        StopServer();
    }

    private void StopServer()
    {
        AnsiConsole.Status().Start($"Stopping {_serverName}...", context =>
        {
            if (RemoveDockerContainer(_options.InstanceName))
            {
                context.Status($"Stopped {_serverName}");
                AnsiConsole.MarkupLine($"[green]{_serverName} stopped[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to stop Docker container for {_options.InstanceName}[/]");
            }
        });
    }

    private void StartServer()
    {
        AnsiConsole.Status().Start($"Initializing {_serverName}...", context =>
        {
            if (!EnsureDockerImageExists(context)) throw new Exception("Failed to pull Docker image");

            context.Status($"Starting {_serverName}...");

            if (RunDockerContainer(_options.ImageName, GetDockerRunArguments(), _options.InstanceName,
                    _options.WorkingDirectory))
            {
                AnsiConsole.MarkupLine($"[green]Started {_serverName}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Failed to start {_serverName}[/]");
                throw new Exception($"Failed to start ${_serverName}");
            }
        });
    }

    private bool EnsureDockerImageExists(StatusContext? context = null)
    {
        if (!IsDockerImageFound(_options.ImageName))
        {
            context?.Status($"Pulling Docker image {_options.ImageName}...");
            if (!PullDockerImage(_options.ImageName))
            {
                AnsiConsole.MarkupLine("[red]Failed to pull Docker image[/]");
                return false;
            }

            context?.Status($"Docker image {_options.ImageName} pulled");
        }
        else
        {
            context?.Status($"Docker image {_options.ImageName} found");
        }

        return true;
    }

    private string GetDockerRunArguments()
    {
        var runOptions = "";
        if (_options.Volumes != null)
            foreach (var volume in _options.Volumes)
                runOptions += $"-v {volume.Key}:{volume.Value} ";
        if (_options.Ports != null)
            foreach (var port in _options.Ports)
                runOptions += $"-p {port.Key}:{port.Value} ";
        if (_options.Environment != null)
            foreach (var env in _options.Environment)
                runOptions += $"-e {env.Key}={env.Value} ";
        return runOptions;
    }

    private bool RunDockerContainer(string imageName, string runOptions, string instanceName, string? workingDirectory)
    {
        return ProcessHelpers.StartProcess("docker",
            $"run -d {runOptions} --name {instanceName} {imageName}",
            workingDirectory ?? Directory.GetCurrentDirectory(), false) == 0;
    }

    private bool RemoveDockerContainer(string containerName)
    {
        return ProcessHelpers.StartProcess("docker", $"rm --force {containerName}", Directory.GetCurrentDirectory(),
            false) == 0;
    }

    private bool IsDockerImageFound(string imageName)
    {
        return ProcessHelpers.StartProcess("docker", $"image inspect {imageName}", _options.WorkingDirectory,
            false) == 0;
    }

    private bool PullDockerImage(string imageName)
    {
        AnsiConsole.MarkupLine($"[green]Downloading {imageName} Docker image[/]");
        if (ProcessHelpers.StartProcess("docker", $"pull {imageName}", _options.WorkingDirectory, false) != 0)
        {
            AnsiConsole.MarkupLine($"[red]Failed to pull the latest {_options.ImageName} docker image[/]");
            return false;
        }

        return true;
    }
}

public class DockerServerOptions
{
    public required string ImageName { get; init; }
    public required string InstanceName { get; init; }
    public Dictionary<string, string>? Volumes { get; init; }
    public Dictionary<string, string>? Ports { get; init; }
    public Dictionary<string, string>? Environment { get; init; }
    public required string WorkingDirectory { get; init; }
}