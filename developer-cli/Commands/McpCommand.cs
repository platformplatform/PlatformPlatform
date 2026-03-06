using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace DeveloperCli.Commands;

public class McpCommand : Command
{
    public McpCommand() : base("mcp", "Start MCP server for AI integration")
    {
        SetAction(async _ => await ExecuteAsync());
    }

    private static async Task ExecuteAsync()
    {
        // MCP server mode - all output to stderr to keep stdout clean
        await Console.Error.WriteLineAsync("[MCP] Starting MCP server...");
        await Console.Error.WriteLineAsync("[MCP] Listening on stdio for MCP communication");

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddConsole(consoleLogOptions => { consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace; });
        builder.Services
            .AddMcpServer()
            .WithStdioServerTransport()
            .WithToolsFromAssembly();

        await builder.Build().RunAsync();
    }
}

[McpServerToolType]
public static class DeveloperCliMcpTools
{
    [McpServerTool]
    [Description("Build the solution including backend (.NET), frontend (React/TypeScript), and developer CLI projects")]
    public static async Task<string> Build(
        [Description("Backend (.NET)")] bool backend = false,
        [Description("Frontend (React/TypeScript)")]
        bool frontend = false,
        [Description("Developer CLI")] bool cli = false,
        [Description("Self-contained system, e.g., 'account' (optional)")]
        string? selfContainedSystem = null)
    {
        var args = new List<string> { "build", "--quiet" };

        if (backend) args.Add("--backend");
        if (frontend) args.Add("--frontend");
        if (cli) args.Add("--cli");

        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Run backend (.NET) xUnit tests with optional name filtering")]
    public static async Task<string> Test(
        [Description("Backend (.NET)")] bool backend = false,
        [Description("Skip build before running tests")]
        bool noBuild = false,
        [Description("Filter tests by name")] string? filter = null,
        [Description("Self-contained system, e.g., 'account' (optional)")]
        string? selfContainedSystem = null)
    {
        var args = new List<string> { "test", "--quiet" };

        if (backend) args.Add("--backend");

        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        if (noBuild) args.Add("--no-build");

        if (filter is not null)
        {
            args.Add("--filter");
            args.Add(filter);
        }

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Format and auto-fix code style for backend (.NET), frontend (React/TypeScript), and developer CLI projects")]
    public static async Task<string> Format(
        [Description("Backend (.NET)")] bool backend = false,
        [Description("Frontend (React/TypeScript)")]
        bool frontend = false,
        [Description("Developer CLI")] bool cli = false,
        [Description("Skip build before formatting")]
        bool noBuild = false,
        [Description("Self-contained system, e.g., 'account' (optional)")]
        string? selfContainedSystem = null)
    {
        var args = new List<string> { "format", "--quiet" };

        if (backend) args.Add("--backend");
        if (frontend) args.Add("--frontend");
        if (cli) args.Add("--cli");

        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        if (noBuild) args.Add("--no-build");

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Inspect and lint code for backend (.NET), frontend (React/TypeScript), and developer CLI projects to find issues")]
    public static async Task<string> Inspect(
        [Description("Backend (.NET)")] bool backend = false,
        [Description("Frontend (React/TypeScript)")]
        bool frontend = false,
        [Description("Developer CLI")] bool cli = false,
        [Description("Skip build before inspecting")]
        bool noBuild = false,
        [Description("Self-contained system, e.g., 'account' (optional)")]
        string? selfContainedSystem = null)
    {
        var args = new List<string> { "inspect", "--quiet" };

        if (backend) args.Add("--backend");
        if (frontend) args.Add("--frontend");
        if (cli) args.Add("--cli");

        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        if (noBuild) args.Add("--no-build");

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Start .NET Aspire AppHost and run database migrations at https://localhost:9000. Runs in the background so you can continue working while it starts.")]
    public static string Run()
    {
        try
        {
            var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");
            var args = new List<string> { "run", "--project", developerCliPath, "run", "--detach", "--force" };

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = string.Join(" ", args),
                WorkingDirectory = Configuration.SourceCodeFolder,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process();
            process.StartInfo = processStartInfo;
            var output = new List<string>();
            var errors = new List<string>();

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null) output.Add(e.Data);
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null) errors.Add(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            // Wait briefly to capture startup messages, then return (don't wait for full exit)
            Thread.Sleep(TimeSpan.FromSeconds(3));

            if (process.HasExited && process.ExitCode != 0)
            {
                return $"Failed to start Aspire.\n\n{string.Join("\n", output)}\n{string.Join("\n", errors)}";
            }

            return "Aspire started successfully in detached mode at https://localhost:9000";
        }
        catch (Exception exception)
        {
            return $"Error starting Aspire: {exception.Message}";
        }
    }

    [McpServerTool]
    [Description("Run end-to-end tests")]
    public static async Task<string> EndToEnd(
        [Description("Search terms")] string[]? searchTerms = null,
        [Description("Browser")] string browser = "all",
        [Description("Smoke only")] bool smoke = false,
        [Description("Wait for Aspire to start (retries server check up to 2 minutes)")]
        bool waitForAspire = false,
        [Description("Maximum retry count for flaky tests, zero for no retries")]
        int? retries = null,
        [Description("Stop after the first failure")]
        bool stopOnFirstFailure = false,
        [Description("Number of times to repeat each test")]
        int? repeatEach = null,
        [Description("Only re-run the failures")]
        bool lastFailed = false,
        [Description("Number of worker processes to use for running tests")]
        int? workers = null)
    {
        var args = new List<string> { "e2e", "--quiet" };
        if (searchTerms is { Length: > 0 }) args.AddRange(searchTerms);
        if (browser != "all")
        {
            args.Add("--browser");
            args.Add(browser);
        }

        if (smoke) args.Add("--smoke");
        if (waitForAspire) args.Add("--wait-for-aspire");
        if (retries.HasValue) args.Add($"--retries={retries.Value}");
        if (stopOnFirstFailure) args.Add("--stop-on-first-failure");
        if (repeatEach.HasValue) args.Add($"--repeat-each={repeatEach.Value}");
        if (lastFailed) args.Add("--last-failed");
        if (workers.HasValue) args.Add($"--workers={workers.Value}");

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Send an interrupt signal to a team agent. The signal is picked up by the agent's PostToolUse hook on their next tool call. For idle/sleeping agents, also use SendMessage with 'Check your interrupt signal' to wake them.")]
    public static string SendInterruptSignal(
        [Description("Team name (e.g., 'feature-team')")]
        string teamName,
        [Description("Target agent name (e.g., 'backend', 'frontend')")]
        string agentName,
        [Description("Interrupt message to deliver to the agent")]
        string message)
    {
        var homeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var signalsDirectory = Path.Combine(homeDirectory, ".claude", "teams", teamName, "signals");

        if (!Directory.Exists(signalsDirectory))
        {
            Directory.CreateDirectory(signalsDirectory);
        }

        var signalFilePath = Path.Combine(signalsDirectory, $"{agentName}.signal");
        File.AppendAllText(signalFilePath, message + "\n");

        return $"Interrupt sent to {agentName} in {teamName}";
    }

    [McpServerTool]
    [Description("Sync AI rules from .claude to .windsurf and .cursor. CRITICAL: Always make AI rule changes in .claude folder first, then sync")]
    public static async Task<string> SyncAiRules()
    {
        return await ExecuteCliCommandAsync(["sync-ai-rules"]);
    }

    private static async Task<string> ExecuteCliCommandAsync(string[] args)
    {
        try
        {
            var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");
            var allArgs = new List<string> { "run", "--project", developerCliPath, "--" };
            allArgs.AddRange(args);

            var command = $"dotnet {string.Join(" ", allArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))}";
            var result = await ProcessHelper.ExecuteQuietlyAsync(command, Configuration.SourceCodeFolder);

            return result.CombinedOutput;
        }
        catch (Exception exception)
        {
            return $"Error executing command: {exception.Message}";
        }
    }
}
