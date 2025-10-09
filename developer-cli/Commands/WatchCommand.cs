using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

/// <summary>
///     Command to manage Aspire AppHost lifecycle - start, stop, and monitor the application host.
/// </summary>
public class WatchCommand : Command
{
    private const int AspirePort = 9001;
    private const int DashboardPort = 9097;
    private const int ResourceServicePort = 9098;

    public WatchCommand() : base("watch", "Manages Aspire AppHost operations")
    {
        AddOption(new Option<bool>(["--force"], "Force start a fresh Aspire AppHost instance, stopping any existing one"));
        AddOption(new Option<bool>(["--stop"], "Stop any running Aspire AppHost instance without starting a new one"));
        AddOption(new Option<bool>(["--attach", "-a"], "Keep the CLI process attached to the Aspire process"));
        AddOption(new Option<bool>(["--detach", "-d"], "Run the Aspire process in detached mode (background)"));
        AddOption(new Option<string?>(["--public-url"], "Set the PUBLIC_URL environment variable for the app (e.g., https://example.ngrok-free.app)"));

        Handler = CommandHandler.Create<bool, bool, bool, bool, string?>(Execute);
    }

    private static void Execute(bool force, bool stop, bool attach, bool detach, string? publicUrl)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node, Prerequisite.Docker);

        var isRunning = IsAspireRunning();

        if (stop)
        {
            StopAspire();
            return;
        }

        // Validate that either --attach or --detach is specified (but not both)
        if (attach == detach)
        {
            AnsiConsole.MarkupLine("[red]You must specify either --attach (-a) or --detach (-d) mode.[/]");
            Environment.Exit(1);
        }

        if (isRunning)
        {
            if (!force)
            {
                AnsiConsole.MarkupLine($"[yellow]Aspire AppHost is already running on port {AspirePort}. Use --force to force a fresh start or --stop to stop it.[/]");
                Environment.Exit(1);
            }

            StopAspire();
        }

        StartAspireAppHost(attach, publicUrl);
    }

    private static bool IsAspireRunning()
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

        // Also check if there are any dotnet watch processes running AppHost
        if (Configuration.IsWindows)
        {
            // Check if any dotnet.exe processes are running with AppHost in the command line
            var watchProcesses = ProcessHelper.StartProcess("""powershell -Command "Get-Process dotnet -ErrorAction SilentlyContinue | Where-Object {$_.CommandLine -like '*watch*AppHost*'} | Select-Object Id" """, redirectOutput: true, exitOnError: false);
            return !string.IsNullOrWhiteSpace(watchProcesses) && watchProcesses.Contains("Id");
        }
        else
        {
            var watchProcesses = ProcessHelper.StartProcess("pgrep -f dotnet.*watch.*AppHost", redirectOutput: true, exitOnError: false);
            return !string.IsNullOrWhiteSpace(watchProcesses);
        }
    }

    private static void StopAspire()
    {
        AnsiConsole.MarkupLine("[blue]Stopping Aspire AppHost and all related services...[/]");

        if (Configuration.IsWindows)
        {
            // Kill all dotnet and rsbuild-node processes on ports 9000-9999
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

                    if (!int.TryParse(address[(portIndex + 1)..], out var port) || port < 9000 || port > 9999) continue;

                    var pid = parts[^1];
                    if (processedPids.Contains(pid)) continue;
                    processedPids.Add(pid);

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
            // First, find all process command names running on SCS ports (9100, 9200, 9300, etc.)
            var scsCommandNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var port = 9100; port <= 9900; port += 100)
            {
                var pids = ProcessHelper.StartProcess($"lsof -i :{port} -sTCP:LISTEN -t", redirectOutput: true, exitOnError: false);
                if (!string.IsNullOrWhiteSpace(pids))
                {
                    foreach (var pid in pids.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        var commandName = ProcessHelper.StartProcess($"ps -p {pid} -o comm=", redirectOutput: true, exitOnError: false).Trim();
                        if (!string.IsNullOrWhiteSpace(commandName))
                        {
                            scsCommandNames.Add(commandName);
                        }
                    }
                }
            }

            // Now kill all processes on ports 9000-9999 that match SCS commands or known Aspire processes
            var pidsOutput = ProcessHelper.StartProcess("lsof -i :9000-9999 -sTCP:LISTEN -t", redirectOutput: true, exitOnError: false);
            if (!string.IsNullOrWhiteSpace(pidsOutput))
            {
                foreach (var pid in pidsOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var commandName = ProcessHelper.StartProcess($"ps -p {pid} -o comm=", redirectOutput: true, exitOnError: false).Trim();
                    if (string.IsNullOrWhiteSpace(commandName)) continue;

                    var shouldKill = scsCommandNames.Contains(commandName) ||
                                     commandName.Contains("AppHost", StringComparison.OrdinalIgnoreCase) ||
                                     commandName.Contains("dcp", StringComparison.OrdinalIgnoreCase) ||
                                     commandName.Contains("PlatformP", StringComparison.OrdinalIgnoreCase) ||
                                     commandName.Contains("rsbuild-node", StringComparison.OrdinalIgnoreCase);

                    if (shouldKill)
                    {
                        ProcessHelper.StartProcess($"kill -9 {pid}", redirectOutput: true, exitOnError: false);
                    }
                }
            }

            // Kill Aspire-specific processes
            ProcessHelper.StartProcess("pkill -9 -if aspire", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("pkill -9 -f dcp", redirectOutput: true, exitOnError: false);
        }

        // Wait a moment for processes to terminate
        Thread.Sleep(TimeSpan.FromSeconds(2));

        AnsiConsole.MarkupLine("[green]Aspire AppHost stopped successfully.[/]");
    }

    private static void StartAspireAppHost(bool attach, string? publicUrl)
    {
        AnsiConsole.MarkupLine($"[blue]Starting Aspire AppHost in {(attach ? "attached" : "detached")} mode...[/]");

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
        var command = $"dotnet watch --non-interactive --project {appHostProjectPath}";

        if (!attach && Configuration.IsWindows)
        {
            // For Windows in detached mode, use "start" command to truly detach
            var detachedCommand = $"cmd /c start \"Aspire AppHost\" /min {command}";
            if (publicUrl is not null)
            {
                ProcessHelper.StartProcess($"{detachedCommand} --environment PUBLIC_URL={publicUrl}", Configuration.ApplicationFolder, waitForExit: false);
            }
            else
            {
                ProcessHelper.StartProcess(detachedCommand, Configuration.ApplicationFolder, waitForExit: false);
            }

            // Give it a moment to start
            Thread.Sleep(2000);
            AnsiConsole.MarkupLine("[green]Aspire AppHost started in detached mode.[/]");
        }
        else
        {
            // Attached mode or non-Windows
            if (publicUrl is not null)
            {
                ProcessHelper.StartProcess(command, Configuration.ApplicationFolder, waitForExit: attach, environmentVariables: ("PUBLIC_URL", publicUrl));
            }
            else
            {
                ProcessHelper.StartProcess(command, Configuration.ApplicationFolder, waitForExit: attach);
            }
        }
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
        var ngrokCommand = $"ngrok http --url={subdomain}.ngrok-free.app https://localhost:9000";

        if (Configuration.IsWindows)
        {
            ProcessHelper.StartProcess($"start /B {ngrokCommand}", waitForExit: false);
        }
        else
        {
            // Use shell to handle backgrounding properly
            ProcessHelper.StartProcess($"sh -c \"{ngrokCommand} > /dev/null 2>&1 &\"", waitForExit: false);
        }

        AnsiConsole.MarkupLine("[green]Ngrok tunnel started successfully.[/]");
    }
}
