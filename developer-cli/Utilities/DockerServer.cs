using System.Text;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

internal class DockerServer(DockerServerOptions options) : IDisposable
{
    public void Dispose()
    {
        StopServer();
    }

    private void StopServer()
    {
        AnsiConsole.Status().Start($"Stopping {options.InstanceName} server...", context =>
        {
            try
            {
                RemoveDockerContainer(options.InstanceName);
                context.Status($"Stopped {options.InstanceName} server.");
                AnsiConsole.MarkupLine($"[green]{options.InstanceName} server stopped.[/]");
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Failed stopping container for {options.InstanceName}. {e.Message}[/]");
                Environment.Exit(1);
            }
        });
    }

    public void StartServer()
    {
        AnsiConsole.Status().Start($"Initializing {options.InstanceName} server...", context =>
        {
            EnsureDockerImageExists(context);

            context.Status($"Starting {options.InstanceName} server...");

            try
            {
                var dockerRunArguments = GetDockerRunArguments();
                RunDockerContainer(options.ImageName, dockerRunArguments, options.InstanceName);
                AnsiConsole.MarkupLine($"[green]Started {options.InstanceName} server.[/]");
            }
            catch (Exception e)
            {
                AnsiConsole.MarkupLine($"[red]Failed to start {options.InstanceName} server. {e.Message}[/]");
                Environment.Exit(1);
            }
        });
    }

    private void EnsureDockerImageExists(StatusContext context)
    {
        if (DockerImageExists(options.ImageName)) return;

        context.Status($"Pulling Docker image {options.ImageName}...");
        PullDockerImage(options.ImageName);

        context.Status($"Docker image {options.ImageName} pulled");
    }

    private string GetDockerRunArguments()
    {
        var runOptions = new StringBuilder();
        if (options.Volumes is not null)
        {
            foreach (var volume in options.Volumes)
            {
                runOptions.Append($"-v {volume.Key}:{volume.Value} ");
            }
        }

        if (options.Ports is not null)
        {
            foreach (var port in options.Ports)
            {
                runOptions.Append($"-p {port.Key}:{port.Value} ");
            }
        }

        if (options.Environment is not null)
        {
            foreach (var env in options.Environment)
            {
                runOptions.Append($"-e {env.Key}={env.Value} ");
            }
        }

        return runOptions.ToString().Trim();
    }

    private void RunDockerContainer(string imageName, string runOptions, string instanceName)
    {
        var output = ProcessHelper.StartProcess(
            "docker",
            $"run -d {runOptions} --name {instanceName} {imageName}",
            redirectOutput: true
        );

        if (!output.Contains("Error")) return;

        AnsiConsole.MarkupLine($"[red]Failed to start {options.InstanceName} server. {output}[/]");
        Environment.Exit(1);
    }

    private void RemoveDockerContainer(string containerName)
    {
        ProcessHelper.StartProcess("docker", $"rm --force {containerName}", Directory.GetCurrentDirectory());
    }

    private bool DockerImageExists(string imageName)
    {
        var output = ProcessHelper.StartProcess("docker", $"image inspect {imageName}", redirectOutput: true);
        return output.Contains("Digest");
    }

    private void PullDockerImage(string imageName)
    {
        try
        {
            AnsiConsole.MarkupLine($"[green]Downloading {imageName} Docker image.[/]");
            ProcessHelper.StartProcess("docker", $"pull {imageName}");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[red]Failed to pull the latest {options.ImageName} docker image. {e.Message}[/]");
            Environment.Exit(1);
        }
    }
}

public class DockerServerOptions
{
    public required string ImageName { get; init; }

    public required string InstanceName { get; init; }

    public Dictionary<string, string>? Volumes { get; init; }

    public Dictionary<string, string>? Ports { get; init; }

    public Dictionary<string, string>? Environment { get; init; }
}