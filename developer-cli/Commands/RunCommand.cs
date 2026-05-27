using System.CommandLine;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using SharedKernel.Configuration;
using Spectre.Console;

namespace DeveloperCli.Commands;

/// <summary>
///     Command to start the Aspire AppHost. Use <c>restart</c> to start a fresh instance or <c>stop</c> to stop it.
/// </summary>
public class RunCommand : Command
{
    public RunCommand() : base("run", "Runs Aspire AppHost (use --watch for hot reload)")
    {
        var watchOption = new Option<bool>("--watch", "-w") { Description = "Enable watch mode for hot reload" };
        var attachOption = new Option<bool>("--attach", "-a") { Description = "Keep the CLI process attached to the Aspire process (detached is the default)" };
        var publicUrlOption = new Option<string?>("--public-url") { Description = "Set the PUBLIC_URL environment variable for the app (e.g., https://example.ngrok-free.app)" };

        Options.Add(watchOption);
        Options.Add(attachOption);
        Options.Add(publicUrlOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(watchOption),
                parseResult.GetValue(attachOption),
                parseResult.GetValue(publicUrlOption)
            )
        );
    }

    // The CLI binary is published outside the repo, so PortAllocation.Load (which walks up from
    // AppContext.BaseDirectory) cannot find the repo. Use the CLI's known SourceCodeFolder instead.
    internal static PortAllocation Ports => PortAllocation.LoadFrom(Configuration.SourceCodeFolder);

    internal static int AspirePort => Ports.Aspire;

    internal static int DashboardPort => Ports.OtelEndpoint;

    internal static int ResourceServicePort => Ports.ResourceService;

    private static void Execute(bool watch, bool attach, string? publicUrl)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node, Prerequisite.Docker);

        // Refuse if Aspire is already on the currently configured port.
        // Skipped in a fresh worktree (no port.txt) where the check would false-positive on another worktree's stack.
        if (PortAllocation.PortFileExists(Configuration.SourceCodeFolder) && IsAspireRunning())
        {
            var alias = Configuration.AliasName;
            AnsiConsole.MarkupLine($"[yellow]Aspire AppHost is already running on port {AspirePort}. Run '{alias} stop' to stop it or '{alias} restart' to start a fresh instance.[/]");
            Environment.Exit(1);
        }

        CheckForPortConflicts();

        StartAspireAppHost(watch, attach, publicUrl);
    }

    internal static bool IsAspireRunning()
    {
        // Check the main Aspire port
        if (Configuration.IsWindows)
        {
            // Windows: Check all Aspire ports
            var aspirePortsToCheck = new[] { AspirePort, DashboardPort, ResourceServicePort };
            foreach (var port in aspirePortsToCheck)
            {
                var portCheckCommand = $"""powershell -Command "Get-NetTCPConnection -LocalPort {port} -State Listen -ErrorAction SilentlyContinue" """;
                var result = ProcessHelper.StartProcess(portCheckCommand, redirectOutput: true, exitOnError: false);

                if (!string.IsNullOrWhiteSpace(result))
                {
                    return true;
                }
            }
        }
        else
        {
            // macOS/Linux: Original logic - only check main port
            var portCheckCommand = $"lsof -i :{AspirePort} -sTCP:LISTEN -t";
            var result = ProcessHelper.StartProcess(portCheckCommand, redirectOutput: true, exitOnError: false);
            if (!string.IsNullOrWhiteSpace(result))
            {
                return true;
            }
        }

        // Also check if there are any dotnet processes running AppHost for THIS project (both run and watch modes)
        if (Configuration.IsWindows)
        {
            var escapedPath = Configuration.SourceCodeFolder.Replace("\\", "\\\\");
            var appHostProcesses = ProcessHelper.StartProcess($$"""powershell -Command "Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {$_.CommandLine -like '*AppHost*' -and $_.CommandLine -like '*{{escapedPath}}*'} | Select-Object Id" """, redirectOutput: true, exitOnError: false);
            return !string.IsNullOrWhiteSpace(appHostProcesses) && appHostProcesses.Contains("Id");
        }

        var pidsOutput = ProcessHelper.StartProcess("pgrep -f dotnet.*AppHost", redirectOutput: true, exitOnError: false);
        if (string.IsNullOrWhiteSpace(pidsOutput)) return false;

        foreach (var pid in pidsOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var commandLine = ProcessHelper.StartProcess($"ps -p {pid} -o args=", redirectOutput: true, exitOnError: false).Trim();
            if (commandLine.Contains(Configuration.SourceCodeFolder, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    internal static void CheckForPortConflicts()
    {
        // Check if any Aspire port is held by a process from a different project
        var ports = new[] { AspirePort, DashboardPort, ResourceServicePort };
        var conflictSource = ports
            .SelectMany(GetListeningProcessCommandLines)
            .Select(FindProjectRoot)
            .FirstOrDefault(root => root is not null);

        if (conflictSource is null) return;

        AnsiConsole.MarkupLine($"[red]Aspire ports are in use by another project: {conflictSource}[/]");
        AnsiConsole.MarkupLine("[red]Stop that instance first, then try again.[/]");
        Environment.Exit(1);
    }

    private static string[] GetListeningProcessCommandLines(int port)
    {
        if (Configuration.IsWindows)
        {
            var output = ProcessHelper.StartProcess($"""powershell -Command "Get-NetTCPConnection -LocalPort {port} -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty OwningProcess" """, redirectOutput: true, exitOnError: false).Trim();
            if (string.IsNullOrWhiteSpace(output)) return [];

            var commandLine = ProcessHelper.StartProcess($"""powershell -Command "(Get-Process -Id {output} -ErrorAction SilentlyContinue).CommandLine" """, redirectOutput: true, exitOnError: false).Trim();
            return string.IsNullOrWhiteSpace(commandLine) ? [] : [commandLine];
        }

        var processIds = ProcessHelper.StartProcess($"lsof -i :{port} -sTCP:LISTEN -t", redirectOutput: true, exitOnError: false).Trim();
        if (string.IsNullOrWhiteSpace(processIds)) return [];

        return processIds
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => ProcessHelper.StartProcess($"ps -p {id} -o args=", redirectOutput: true, exitOnError: false).Trim())
            .Where(args => !string.IsNullOrWhiteSpace(args))
            .ToArray();
    }

    private static string? FindProjectRoot(string commandLine)
    {
        // Command lines contain paths like .../SomeProject/application/AppHost/...
        var separator = commandLine.Contains('\\') ? "\\" : "/";
        var marker = $"{separator}application{separator}";
        var index = commandLine.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index == -1) return null;

        var pathStart = commandLine.LastIndexOf(' ', index) + 1;
        return commandLine[pathStart..index];
    }

    internal static void StopAspire()
    {
        StopAspire(Configuration.SourceCodeFolder, Ports);
    }

    internal static void StopAspire(string sourceCodeFolder, PortAllocation portAllocation)
    {
        AnsiConsole.MarkupLine($"[blue]Stopping Aspire AppHost on base port {portAllocation.BasePort}...[/]");

        if (Configuration.IsWindows)
        {
            // Kill dotnet and rsbuild-node processes listening on any port in the explicit allocation.
            var allPorts = portAllocation.AllPorts;
            var netstatOutput = ProcessHelper.StartProcess("""cmd /c "netstat -ano | findstr LISTENING" """, redirectOutput: true, exitOnError: false);
            if (!string.IsNullOrWhiteSpace(netstatOutput))
            {
                var processedPids = new HashSet<string>();

                foreach (var line in netstatOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 5) continue;

                    var address = parts[1];
                    var portIndex = address.LastIndexOf(':');
                    if (portIndex == -1) continue;

                    if (!int.TryParse(address[(portIndex + 1)..], out var port) || !allPorts.Contains(port)) continue;

                    var pid = parts[^1];
                    if (!processedPids.Add(pid)) continue;

                    var processName = ProcessHelper.StartProcess($"""wmic process where ProcessId={pid} get Name /format:list""", redirectOutput: true, exitOnError: false);

                    if (processName.Contains("dotnet", StringComparison.OrdinalIgnoreCase) ||
                        processName.Contains("rsbuild-node", StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessHelper.StartProcess($"taskkill /F /PID {pid}", redirectOutput: true, exitOnError: false);
                    }
                }
            }

            // Kill specific Aspire-related processes
            var processesToKill = new[] { "Aspire.Dashboard", "dcp", "dcpproc" };
            foreach (var processName in processesToKill)
            {
                ProcessHelper.StartProcess($"taskkill /F /IM {processName}.exe", redirectOutput: true, exitOnError: false);
            }
        }
        else
        {
            // Find AppHost processes for this worktree, then kill each one and all its children
            // (children include Aspire infrastructure: dcp, dcpproc, Aspire.Dashboard, etc.)
            foreach (var processId in FindAppHostProcesses(sourceCodeFolder))
            {
                KillProcessTree(processId);
            }
        }

        // Wait a moment for processes to terminate
        Thread.Sleep(TimeSpan.FromSeconds(2));

        AnsiConsole.MarkupLine("[green]Aspire AppHost stopped.[/]");
    }

    private static string[] FindAppHostProcesses(string sourceCodeFolder)
    {
        var output = ProcessHelper.StartProcess("pgrep -f dotnet.*AppHost", redirectOutput: true, exitOnError: false);
        if (string.IsNullOrWhiteSpace(output)) return [];

        return output
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(processId =>
                {
                    var commandLine = ProcessHelper.StartProcess($"ps -p {processId} -o args=", redirectOutput: true, exitOnError: false).Trim();
                    return commandLine.Contains(sourceCodeFolder, StringComparison.OrdinalIgnoreCase);
                }
            )
            .ToArray();
    }

    internal static void KillProcessTree(string processId)
    {
        if (Configuration.IsWindows)
        {
            ProcessHelper.StartProcess($"taskkill /T /F /PID {processId}", redirectOutput: true, exitOnError: false);
            return;
        }

        var children = ProcessHelper.StartProcess($"pgrep -P {processId}", redirectOutput: true, exitOnError: false);
        if (!string.IsNullOrWhiteSpace(children))
        {
            foreach (var childId in children.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                KillProcessTree(childId);
            }
        }

        ProcessHelper.StartProcess($"kill -9 {processId}", redirectOutput: true, exitOnError: false);
    }

    internal static void StartAspireAppHost(bool watch, bool attach, string? publicUrl)
    {
        var mode = watch ? "watch" : "run";
        AnsiConsole.MarkupLine($"[blue]Starting Aspire AppHost in {mode} mode ({(attach ? "attached" : "detached")})...[/]");

        if (publicUrl is not null)
        {
            AnsiConsole.MarkupLine($"[blue]Using PUBLIC_URL: {publicUrl}[/]");

            // Check if this is an ngrok URL and start ngrok if needed
            if (publicUrl.Contains(".ngrok-free.app", StringComparison.OrdinalIgnoreCase) ||
                publicUrl.Contains(".ngrok.io", StringComparison.OrdinalIgnoreCase))
            {
                StartNgrokIfNeeded(publicUrl);
            }
        }

        var appHostProjectPath = Path.Combine(Configuration.ApplicationFolder, "AppHost", "AppHost.csproj");
        var command = watch
            ? $"dotnet watch --non-interactive --project {appHostProjectPath}"
            : $"dotnet run --project {appHostProjectPath}";

        // AppHost reads .workspace/port.txt itself and overrides the Aspire dashboard env vars
        // before CreateBuilder. PUBLIC_URL is the only env var the CLI needs to forward.
        var envVars = publicUrl is not null
            ? new[] { ("PUBLIC_URL", publicUrl) }
            : Array.Empty<(string Name, string Value)>();

        if (attach)
        {
            ProcessHelper.StartProcess(command, Configuration.ApplicationFolder, waitForExit: true, environmentVariables: envVars);
            return;
        }

        var logPath = Path.Combine(Configuration.WorkspaceFolder, "developer-cli", "aspire-apphost.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
        if (File.Exists(logPath)) File.Delete(logPath);

        var detachedCommand = Configuration.IsWindows
            ? $"cmd /c start \"\" /min cmd /c \"{command} > \"{logPath}\" 2>&1\""
            : Configuration.IsMacOs
                ? $"sh -c \"script -q -t 0 '{logPath}' {command} > /dev/null 2>&1 &\""
                : $"sh -c \"script -q -f -c '{command}' '{logPath}' > /dev/null 2>&1 &\"";

        ProcessHelper.StartProcess(detachedCommand, Configuration.ApplicationFolder, waitForExit: false, environmentVariables: envVars);

        TailLogUntilReady(logPath);
    }

    private static void TailLogUntilReady(string logPath)
    {
        const string readyMarker = "Distributed application started.";
        const string misleadingShutdownHint = " Press Ctrl+C to shut down.";
        const string dashboardLoginMarker = "Login to the dashboard at ";
        var deadline = DateTime.UtcNow.AddSeconds(60);
        var offset = 0L;
        var sawFirstLine = false;

        AnsiConsole.MarkupLine("[dim]Waiting for AppHost output...[/]");

        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(logPath))
            {
                using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                stream.Seek(offset, SeekOrigin.Begin);
                using var reader = new StreamReader(stream);

                while (reader.ReadLine() is { } line)
                {
                    sawFirstLine = true;
                    var displayLine = line.Replace(misleadingShutdownHint, "").TrimEnd();
                    AnsiConsole.WriteLine(displayLine);

                    var dashboardIndex = line.IndexOf(dashboardLoginMarker, StringComparison.Ordinal);
                    if (dashboardIndex >= 0)
                    {
                        var dashboardUrl = line[(dashboardIndex + dashboardLoginMarker.Length)..].Trim();
                        var dashboardUrlPath = Path.Combine(Configuration.WorkspaceFolder, "aspire-dashboard-url.txt");
                        File.WriteAllText(dashboardUrlPath, dashboardUrl);
                    }

                    if (line.Contains(readyMarker))
                    {
                        AnsiConsole.MarkupLine("[green]Aspire AppHost is ready.[/]");
                        AnsiConsole.MarkupLine($"[dim]Stop with:[/] [yellow]{Configuration.AliasName} stop[/]");
                        AnsiConsole.MarkupLine($"[dim]Logs:[/] {logPath}");
                        return;
                    }
                }

                offset = stream.Position;
            }

            Thread.Sleep(sawFirstLine ? 100 : 300);
        }

        AnsiConsole.MarkupLine($"[yellow]Aspire did not report ready within 60s. Check {logPath}[/]");
    }

    private static void StartNgrokIfNeeded(string publicUrl)
    {
        // First check if ngrok is installed
        var ngrokVersion = ProcessHelper.StartProcess("ngrok version", redirectOutput: true, exitOnError: false);
        if (!ngrokVersion.Contains("ngrok version", StringComparison.OrdinalIgnoreCase))
        {
            AnsiConsole.MarkupLine("[yellow]Ngrok is not installed. Please install ngrok from https://ngrok.com/download[/]");
            AnsiConsole.MarkupLine("[yellow]Continuing without ngrok tunnel...[/]");
            return;
        }

        // Extract the subdomain from the URL
        var uri = new Uri(publicUrl);
        var subdomain = uri.Host.Split('.')[0];

        // Check if ngrok is already running
        bool isNgrokRunning;

        if (Configuration.IsWindows)
        {
            var ngrokProcesses = ProcessHelper.StartProcess("""tasklist /FI "IMAGENAME eq ngrok.exe" """, redirectOutput: true, exitOnError: false);
            isNgrokRunning = ngrokProcesses.Contains("ngrok.exe");
        }
        else
        {
            var ngrokProcesses = ProcessHelper.StartProcess("pgrep -f ngrok", redirectOutput: true, exitOnError: false);
            isNgrokRunning = !string.IsNullOrEmpty(ngrokProcesses);
        }

        if (isNgrokRunning)
        {
            AnsiConsole.MarkupLine("[yellow]Ngrok is already running.[/]");
            return;
        }

        AnsiConsole.MarkupLine("[blue]Starting ngrok tunnel...[/]");

        // Start ngrok in detached mode
        var ngrokCommand = $"ngrok http --url={subdomain}.ngrok-free.app https://app.dev.localhost:{Ports.AppGateway}";

        // Use shell to handle backgrounding properly on macOS/Linux
        ProcessHelper.StartProcess(
            Configuration.IsWindows ? $"start /B {ngrokCommand}" : $"sh -c \"{ngrokCommand} > /dev/null 2>&1 &\"",
            waitForExit: false
        );

        AnsiConsole.MarkupLine("[green]Ngrok tunnel started successfully.[/]");
    }
}
