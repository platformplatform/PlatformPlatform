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
            if (!IsAspireRunning())
            {
                AnsiConsole.MarkupLine("[yellow]No Aspire AppHost instance is running.[/]");
                Environment.Exit(1);
            }

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
        var portCheckCommand = Configuration.IsWindows
            ? $"netstat -ano | findstr :{AspirePort} | findstr LISTENING"
            : $"lsof -i :{AspirePort} -sTCP:LISTEN -t";

        var result = ProcessHelper.StartProcess(portCheckCommand, redirectOutput: true, exitOnError: false);
        if (!string.IsNullOrWhiteSpace(result))
        {
            return true;
        }

        // Also check if there are any dotnet watch processes running AppHost
        if (Configuration.IsWindows)
        {
            // Check if any dotnet.exe processes are running with AppHost in the command line
            var watchProcesses = ProcessHelper.StartProcess("wmic process where \"name='dotnet.exe' and commandline like '%watch%AppHost%'\" get processid", redirectOutput: true, exitOnError: false);
            return !string.IsNullOrWhiteSpace(watchProcesses) && watchProcesses.Contains("ProcessId");
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

        var applicationFolder = Configuration.ApplicationFolder;

        if (Configuration.IsWindows)
        {
            // Use taskkill with filters to kill processes
            // This approach is simpler and more reliable than WMIC

            // Kill all dotnet.exe processes that have AppHost in their command line
            ProcessHelper.StartProcess("taskkill /F /IM dotnet.exe /FI \"WINDOWTITLE eq *AppHost*\"", redirectOutput: true, exitOnError: false);

            // Kill all processes that contain our application folder in the command line
            // Note: Windows doesn't have a direct equivalent to pkill -f, so we use WMIC for this specific case
            ProcessHelper.StartProcess($"wmic process where \"commandline like '%{applicationFolder}%'\" delete", redirectOutput: true, exitOnError: false);

            // Kill specific Aspire and watch processes
            ProcessHelper.StartProcess("taskkill /F /IM dotnet.exe /FI \"WINDOWTITLE eq *watch*\"", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("taskkill /F /IM dotnet.exe /FI \"WINDOWTITLE eq *Aspire*\"", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("taskkill /F /IM dotnet.exe /FI \"WINDOWTITLE eq *Dashboard*\"", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("taskkill /F /IM dcp.exe", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("taskkill /F /IM dcpproc.exe", redirectOutput: true, exitOnError: false);
        }
        else
        {
            // Kill all dotnet watch processes that are running AppHost
            // This handles both absolute and relative paths
            ProcessHelper.StartProcess("pkill -9 -f dotnet.*watch.*AppHost", redirectOutput: true, exitOnError: false);

            // Kill all processes that contain our application folder path
            // This catches any process started from our directory
            ProcessHelper.StartProcess($"pkill -9 -f {applicationFolder}", redirectOutput: true, exitOnError: false);

            // Kill Aspire-specific processes (Dashboard, DCP, etc.)
            ProcessHelper.StartProcess("pkill -9 -if aspire", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("pkill -9 -f dcp", redirectOutput: true, exitOnError: false);

            // Kill processes by project names in case they're running from different locations
            ProcessHelper.StartProcess("pkill -9 -f AppHost", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("pkill -9 -f AccountManagement", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("pkill -9 -f BackOffice", redirectOutput: true, exitOnError: false);
            ProcessHelper.StartProcess("pkill -9 -f AppGateway", redirectOutput: true, exitOnError: false);
        }

        Thread.Sleep(TimeSpan.FromSeconds(2));

        // Final verification - check if core Aspire ports are free
        var stillRunning = false;
        var checkCommand = Configuration.IsWindows
            ? $"netstat -ano | findstr :{AspirePort} | findstr LISTENING"
            : $"lsof -i :{AspirePort} -sTCP:LISTEN -t";
        var result = ProcessHelper.StartProcess(checkCommand, redirectOutput: true, exitOnError: false);
        if (!string.IsNullOrWhiteSpace(result))
        {
            stillRunning = true;
        }

        if (stillRunning)
        {
            AnsiConsole.MarkupLine("[yellow]Warning: Some processes may still be running. You may need to manually kill them.[/]");
            AnsiConsole.MarkupLine("[yellow]Check running processes with: ps aux | grep dotnet[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[green]Aspire AppHost stopped successfully.[/]");
        }
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

        if (publicUrl is not null)
        {
            ProcessHelper.StartProcess(command, Configuration.ApplicationFolder, waitForExit: attach, environmentVariables: ("PUBLIC_URL", publicUrl));
        }
        else
        {
            ProcessHelper.StartProcess(command, Configuration.ApplicationFolder, waitForExit: attach);
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
        var isNgrokRunning = false;

        if (Configuration.IsWindows)
        {
            var ngrokProcesses = ProcessHelper.StartProcess("tasklist /FI \"IMAGENAME eq ngrok.exe\"", redirectOutput: true, exitOnError: false);
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
