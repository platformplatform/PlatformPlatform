using System.CommandLine;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using SharedKernel.Configuration;
using Spectre.Console;

namespace DeveloperCli.Commands;

/// <summary>
///     Command to stop the Aspire AppHost and its Docker containers. Defaults to the current
///     worktree; pass <c>--all</c> for every PlatformPlatform worktree, <c>--port</c> for a
///     specific base port, or <c>--select</c> to pick interactively from running stacks.
/// </summary>
public sealed class StopCommand : Command
{
    public StopCommand() : base("stop", "Stops the Aspire AppHost and its Docker containers")
    {
        var allOption = new Option<bool>("--all", "-a") { Description = $"Stop Aspire and Docker containers across every {Configuration.AliasName} worktree" };
        var portOption = new Option<int?>("--port", "-p") { Description = "Stop the worktree running on this base port (e.g. 9000, 9100, 9200)" };
        var selectOption = new Option<bool>("--select", "-s") { Description = "List running stacks and pick which to stop" };

        Options.Add(allOption);
        Options.Add(portOption);
        Options.Add(selectOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(allOption),
                parseResult.GetValue(portOption),
                parseResult.GetValue(selectOption)
            )
        );
    }

    private static void Execute(bool all, int? port, bool select)
    {
        var modesPicked = (all ? 1 : 0) + (port.HasValue ? 1 : 0) + (select ? 1 : 0);
        if (modesPicked > 1)
        {
            AnsiConsole.MarkupLine("[red]Error: --all, --port, and --select are mutually exclusive.[/]");
            Environment.Exit(1);
        }

        Prerequisite.Ensure(Prerequisite.Docker);

        if (all)
        {
            StopAllWorktrees();
        }
        else if (port.HasValue)
        {
            StopByBasePort(port.Value);
        }
        else if (select)
        {
            StopBySelection();
        }
        else
        {
            StopWorktree(Configuration.SourceCodeFolder);
        }
    }

    private static void StopAllWorktrees()
    {
        AnsiConsole.MarkupLine("[blue]Stopping every worktree's Aspire and Docker containers...[/]");

        var stoppedAny = false;
        foreach (var worktreePath in GetAllWorktreePaths())
        {
            if (!PortAllocation.PortFileExists(worktreePath)) continue;

            StopWorktree(worktreePath);
            stoppedAny = true;
        }

        if (!stoppedAny)
        {
            AnsiConsole.MarkupLine("[yellow]No worktrees with .workspace/port.txt found.[/]");
        }
    }

    private static void StopByBasePort(int basePort)
    {
        var worktreePath = GetAllWorktreePaths()
            .FirstOrDefault(w => PortAllocation.PortFileExists(w) && PortAllocation.LoadFrom(w).BasePort == basePort);

        if (worktreePath is not null)
        {
            StopWorktree(worktreePath);
            return;
        }

        // No worktree currently maps to this port (e.g. the worktree was deleted but containers leaked).
        // Try a Docker-only cleanup using the port-derived volume names.
        AnsiConsole.MarkupLine($"[yellow]No worktree found with base port {basePort}. Attempting Docker-only cleanup.[/]");
        StopDockerContainers(new PortAllocation(basePort));
    }

    private static void StopBySelection()
    {
        var stacks = DiscoverStoppableStacks();
        if (stacks.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No running stacks found.[/]");
            return;
        }

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<StoppableStack>()
                .Title("Select stacks to stop")
                .NotRequired()
                .PageSize(15)
                .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                .UseConverter(s => s.DisplayLabel)
                .AddChoices(stacks)
        );

        if (selected.Count == 0)
        {
            AnsiConsole.MarkupLine("[dim]Nothing selected.[/]");
            return;
        }

        foreach (var stack in selected)
        {
            StopStack(stack);
        }
    }

    private static void StopWorktree(string worktreePath)
    {
        if (!PortAllocation.PortFileExists(worktreePath))
        {
            AnsiConsole.MarkupLine("[yellow]No .workspace/port.txt found in this worktree. Nothing to stop.[/]");
            return;
        }

        var portAllocation = PortAllocation.LoadFrom(worktreePath);

        RunCommand.StopAspire(worktreePath, portAllocation);
        StopDockerContainers(portAllocation);
    }

    private static void StopStack(StoppableStack stack)
    {
        if (stack is { IsExternal: false, BasePort: { } basePort })
        {
            // PlatformPlatform worktree: use the existing path-and-port-aware stop logic.
            RunCommand.StopAspire(stack.SourceFolder, new PortAllocation(basePort));
        }
        else if (stack.AspirePid is not null)
        {
            // External Aspire (a downstream project's AppHost). Kill its process tree directly --
            // we do not touch its Docker containers because we do not know the project's volume scheme.
            AnsiConsole.MarkupLine($"[blue]Stopping external Aspire AppHost: {stack.SourceFolder}[/]");
            RunCommand.KillProcessTree(stack.AspirePid);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            AnsiConsole.MarkupLine("[green]External Aspire AppHost stopped.[/]");
        }

        foreach (var name in stack.Containers)
        {
            AnsiConsole.MarkupLine($"[green]Stopping container {name}.[/]");
            ProcessHelper.StartProcess($"docker rm --force {name}", redirectOutput: true, exitOnError: false);
        }
    }

    private static StoppableStack[] DiscoverStoppableStacks()
    {
        var stacks = new List<StoppableStack>();
        var ppWorktreePaths = GetAllWorktreePaths().ToArray();

        // 1. PlatformPlatform worktrees with running Aspire and/or leftover Docker containers.
        foreach (var worktreePath in ppWorktreePaths)
        {
            if (!PortAllocation.PortFileExists(worktreePath)) continue;

            var portAllocation = PortAllocation.LoadFrom(worktreePath);
            var aspirePid = FindAppHostPidForFolder(worktreePath);
            var containers = FindContainersForBasePort(portAllocation);

            if (aspirePid is null && containers.Length == 0) continue;

            stacks.Add(new StoppableStack(portAllocation.BasePort, worktreePath, aspirePid, containers, false));
        }

        // 2. External Aspire AppHosts (downstream projects). Cross-platform pgrep is non-trivial on
        // Windows; skip discovery there rather than partially supporting it.
        // The detached-mode launcher wraps `dotnet run` with `script` for log capture, so each
        // stack typically shows up as both a wrapper PID and a child PID -- group by source folder
        // and keep the lower PID (the wrapper). KillProcessTree walks descendants either way.
        if (!Configuration.IsWindows)
        {
            var externalEntries = GetAllRunningAppHostPids()
                .Select(pid => new
                    {
                        Pid = pid,
                        CommandLine = ProcessHelper.StartProcess($"ps -p {pid} -o args=", redirectOutput: true, exitOnError: false).Trim()
                    }
                )
                .Where(e => !ppWorktreePaths.Any(w => e.CommandLine.Contains(w, StringComparison.OrdinalIgnoreCase)))
                .Select(e => new
                    {
                        e.Pid,
                        SourceFolder = ExtractSourceFolderFromCommandLine(e.CommandLine) ?? e.CommandLine
                    }
                )
                .GroupBy(e => e.SourceFolder)
                .Select(g => g.OrderBy(e => int.Parse(e.Pid)).First());

            foreach (var entry in externalEntries)
            {
                // Downstream projects use the same .workspace/port.txt convention -- read it so the
                // user sees the port instead of a bare "External" label.
                int? externalBasePort = PortAllocation.PortFileExists(entry.SourceFolder)
                    ? PortAllocation.LoadFrom(entry.SourceFolder).BasePort
                    : null;

                stacks.Add(new StoppableStack(externalBasePort, entry.SourceFolder, entry.Pid, [], true));
            }
        }

        // 3. Orphan persistent Aspire containers (e.g. a downstream project's stack still running
        // after its AppHost died). Group by Aspire/DCP session suffix so siblings stop together.
        var coveredContainers = stacks
            .SelectMany(s => s.Containers)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var group in DiscoverOrphanContainerGroups(coveredContainers))
        {
            stacks.Add(new StoppableStack(null, group.Description, null, group.Containers, true));
        }

        return stacks.ToArray();
    }

    private static IEnumerable<(string Description, string[] Containers)> DiscoverOrphanContainerGroups(HashSet<string> coveredContainerNames)
    {
        var output = ProcessHelper.StartProcess(
            "docker ps --filter label=com.microsoft.developer.usvc-dev.persistent=true --format {{.Names}}",
            redirectOutput: true, exitOnError: false
        );
        if (string.IsNullOrWhiteSpace(output)) yield break;

        var orphanNames = output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => !coveredContainerNames.Contains(name))
            .ToArray();

        var groups = orphanNames
            .Select(name => new { Name = name, Suffix = ExtractSessionSuffix(name) })
            .Where(x => x.Suffix is not null)
            .GroupBy(x => x.Suffix!);

        foreach (var group in groups)
        {
            var names = group.Select(x => x.Name).ToArray();
            var prefix = ExtractVolumePrefix(names[0]);
            var description = prefix is not null
                ? $"{prefix}-* (no Aspire process)"
                : $"suffix -{group.Key} (no Aspire process)";
            yield return (description, names);
        }
    }

    private static string? ExtractSessionSuffix(string containerName)
    {
        var lastDash = containerName.LastIndexOf('-');
        return lastDash == -1 ? null : containerName[(lastDash + 1)..];
    }

    // Returns the project-specific volume prefix (e.g. "vilo", "platform-platform-9100") read from
    // the container's mount label. Empty for containers without a data volume (mailpit, stripe-cli).
    private static string? ExtractVolumePrefix(string containerName)
    {
        var mountsLabel = ProcessHelper.StartProcess(
            $"docker inspect {containerName} --format {{{{index .Config.Labels \"com.microsoft.developer.usvc-dev.mountsLabel\"}}}}",
            redirectOutput: true, exitOnError: false
        ).Trim();
        if (string.IsNullOrWhiteSpace(mountsLabel)) return null;

        var srcMarker = "src=";
        var srcIndex = mountsLabel.IndexOf(srcMarker, StringComparison.Ordinal);
        if (srcIndex == -1) return null;

        var src = mountsLabel[(srcIndex + srcMarker.Length)..].Trim();
        // Volume name format: "{prefix}-{resource}-data" (e.g. "vilo-postgres-data"). Trim trailing
        // resource name to surface just the project prefix.
        var dataSuffix = "-data";
        if (!src.EndsWith(dataSuffix, StringComparison.Ordinal)) return src;
        var withoutData = src[..^dataSuffix.Length];
        var lastDash = withoutData.LastIndexOf('-');
        return lastDash == -1 ? withoutData : withoutData[..lastDash];
    }

    private static string? FindAppHostPidForFolder(string sourceCodeFolder)
    {
        foreach (var pid in GetAllRunningAppHostPids())
        {
            var commandLine = ProcessHelper.StartProcess($"ps -p {pid} -o args=", redirectOutput: true, exitOnError: false).Trim();
            if (commandLine.Contains(sourceCodeFolder, StringComparison.OrdinalIgnoreCase)) return pid;
        }

        return null;
    }

    private static string[] GetAllRunningAppHostPids()
    {
        if (Configuration.IsWindows) return [];

        var output = ProcessHelper.StartProcess("pgrep -f dotnet.*AppHost", redirectOutput: true, exitOnError: false);
        return string.IsNullOrWhiteSpace(output)
            ? []
            : output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string? ExtractSourceFolderFromCommandLine(string commandLine)
    {
        var separator = commandLine.Contains('\\') ? "\\" : "/";
        var marker = $"{separator}application{separator}";
        var index = commandLine.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index == -1) return null;

        var pathStart = commandLine.LastIndexOf(' ', index) + 1;
        return commandLine[pathStart..index];
    }

    private static IEnumerable<string> GetAllWorktreePaths()
    {
        var output = ProcessHelper.StartProcess(
            "git worktree list --porcelain", Configuration.SourceCodeFolder, true, exitOnError: false
        );
        if (string.IsNullOrWhiteSpace(output)) yield break;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            const string prefix = "worktree ";
            if (line.StartsWith(prefix, StringComparison.Ordinal))
            {
                yield return line[prefix.Length..].Trim();
            }
        }
    }

    private static string[] FindContainersForBasePort(PortAllocation portAllocation)
    {
        var infix = portAllocation.VolumeNameInfix;
        string[] worktreeVolumes = [$"platform-platform{infix}-postgres-data", $"platform-platform{infix}-azure-storage-data"];

        var sessionSuffix = FindAspireSessionSuffix(worktreeVolumes);
        return sessionSuffix is null ? [] : FindContainersBySessionSuffix(sessionSuffix);
    }

    // Aspire/DCP gives every container in a session a shared random suffix (e.g. "postgres-606c6219",
    // "azure-storage-606c6219", "mail-server-606c6219"). The persistent volumes on postgres and
    // azurite are named after the worktree's base port -- we use them to find one container in the
    // session, extract the suffix, then remove every container that shares it.
    private static void StopDockerContainers(PortAllocation portAllocation)
    {
        var containers = FindContainersForBasePort(portAllocation);
        if (containers.Length == 0)
        {
            AnsiConsole.MarkupLine($"[dim]No Docker containers found for base port {portAllocation.BasePort}.[/]");
            return;
        }

        foreach (var name in containers)
        {
            AnsiConsole.MarkupLine($"[green]Stopping container {name}.[/]");
            ProcessHelper.StartProcess($"docker rm --force {name}", redirectOutput: true, exitOnError: false);
        }
    }

    private static string? FindAspireSessionSuffix(string[] worktreeVolumes)
    {
        foreach (var volume in worktreeVolumes)
        {
            var output = ProcessHelper.StartProcess(
                $"docker ps -a --filter volume={volume} --format {{{{.Names}}}}", redirectOutput: true, exitOnError: false
            );
            if (string.IsNullOrWhiteSpace(output)) continue;

            var firstName = output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .FirstOrDefault();
            if (firstName is null) continue;

            var lastDash = firstName.LastIndexOf('-');
            if (lastDash == -1) continue;

            return firstName[(lastDash + 1)..];
        }

        return null;
    }

    private static string[] FindContainersBySessionSuffix(string sessionSuffix)
    {
        var output = ProcessHelper.StartProcess(
            $"docker ps -a --filter name=-{sessionSuffix} --format {{{{.Names}}}}", redirectOutput: true, exitOnError: false
        );
        if (string.IsNullOrWhiteSpace(output)) return [];

        // docker --filter name= is a substring match -- post-filter to require the suffix at the end
        // so we don't accidentally remove an unrelated container that happens to contain the hex suffix.
        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(name => name.EndsWith($"-{sessionSuffix}", StringComparison.Ordinal))
            .ToArray();
    }

    private sealed record StoppableStack(int? BasePort, string SourceFolder, string? AspirePid, string[] Containers, bool IsExternal)
    {
        public string DisplayLabel
        {
            get
            {
                var parts = new List<string>();
                if (AspirePid is not null) parts.Add("Aspire");
                if (Containers.Length > 0) parts.Add($"{Containers.Length} container{(Containers.Length == 1 ? "" : "s")}");
                var status = parts.Count == 0 ? "(nothing)" : string.Join(" + ", parts);

                var portLabel = BasePort.HasValue ? $"port {BasePort}" : "port ?   ";
                var prefix = IsExternal
                    ? $"[yellow]External[/] [cyan]{portLabel}[/]"
                    : $"[cyan]{portLabel}[/]         ";

                return $"{prefix}  {status,-30}  [grey]{Markup.Escape(SourceFolder)}[/]";
            }
        }
    }
}
