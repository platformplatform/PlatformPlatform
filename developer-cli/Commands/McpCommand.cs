using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

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
    [Description("Execute developer CLI commands: build, test, format, or inspect code")]
    public static async Task<string> ExecuteCommand(
        [Description("Command to execute: 'build', 'test', 'format', or 'inspect'")]
        string command,
        [Description("Backend")] bool backend = false,
        [Description("Frontend")] bool frontend = false,
        [Description("Self-contained system, e.g., 'account' (optional)")]
        string? selfContainedSystem = null,
        [Description("Skip build (for test, format, inspect only)")]
        bool noBuild = false,
        [Description("Filter tests by name (test command only)")]
        string? filter = null,
        [Description("Developer CLI")] bool cli = false)
    {
        var validCommands = new[] { "build", "test", "format", "inspect" };
        if (!validCommands.Contains(command))
        {
            return $"Invalid command: '{command}'. Valid commands: {string.Join(", ", validCommands)}";
        }

        var args = new List<string> { command, "--quiet" };

        // Add target flags - if none specified, command will run all targets
        if (backend) args.Add("--backend");
        if (frontend) args.Add("--frontend");
        if (cli) args.Add("--cli");

        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        if (noBuild && command != "build")
        {
            args.Add("--no-build");
        }

        if (filter is not null && command == "test")
        {
            args.Add("--filter");
            args.Add(filter);
        }

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Start .NET Aspire AppHost and run database migrations at https://localhost:9000. Runs in the background so you can continue working while it starts.")]
    public static string Run()
    {
        // Call run command in detached mode - don't wait for process exit
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

    [McpServerTool]
    [Description("Run end-to-end tests")]
    public static async Task<string> EndToEnd(
        [Description("Search terms")] string[]? searchTerms = null,
        [Description("Browser")] string browser = "all",
        [Description("Smoke only")] bool smoke = false,
        [Description("Wait for Aspire to start (retries server check up to 50 seconds)")]
        bool waitForAspire = false)
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

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Sync AI rules from .claude to .windsurf and .cursor. CRITICAL: Always make AI rule changes in .claude folder first, then sync")]
    public static async Task<string> SyncAiRules()
    {
        return await ExecuteCliCommandAsync(["sync-ai-rules"]);
    }

    private static async Task<string> ExecuteCliCommandAsync(string[] args)
    {
        var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");
        var allArgs = new List<string> { "run", "--project", developerCliPath, "--" };
        allArgs.AddRange(args);

        var command = $"dotnet {string.Join(" ", allArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))}";
        var result = await ProcessHelper.ExecuteQuietlyAsync(command, Configuration.SourceCodeFolder);

        return result.CombinedOutput;
    }
}
