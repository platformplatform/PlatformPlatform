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
        this.SetHandler(ExecuteAsync);
    }

    private static async Task ExecuteAsync()
    {
        // MCP server mode - all output to stderr to keep stdout clean
        await Console.Error.WriteLineAsync("[MCP] Starting MCP server...");
        await Console.Error.WriteLineAsync("[MCP] Listening on stdio for MCP communication");

        var builder = Host.CreateApplicationBuilder();
        builder.Logging.AddConsole(consoleLogOptions => { consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace; }
        );
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
    [Description("Build code. Builds both backend and frontend by default. Use backend/frontend flags to build only one.")]
    public static string Build(
        [Description("Build backend only (optional)")]
        bool backend = false,
        [Description("Build frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null)
    {
        return ExecuteStandardMcpCommand("build", backend, frontend, selfContainedSystem);
    }

    [McpServerTool]
    [Description("Run tests. Tests entire codebase by default.")]
    public static string Test(
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null,
        [Description("Skip build (optional)")] bool noBuild = false,
        [Description("Test backend only (optional)")]
        bool backend = false,
        [Description("Test frontend only (optional)")]
        bool frontend = false)
    {
        return ExecuteStandardMcpCommand("test", backend, frontend, selfContainedSystem, args =>
            {
                if (noBuild) args.Add("--no-build");
            }
        );
    }

    [McpServerTool]
    [Description("Format code. Formats both backend and frontend by default. Use backend/frontend flags to format only one.")]
    public static string Format(
        [Description("Format backend only (optional)")]
        bool backend = false,
        [Description("Format frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null)
    {
        return ExecuteStandardMcpCommand("format", backend, frontend, selfContainedSystem);
    }

    [McpServerTool]
    [Description("Run code inspections. Inspects both backend and frontend by default. Use backend/frontend flags to inspect only one.")]
    public static string Inspect(
        [Description("Inspect backend only (optional)")]
        bool backend = false,
        [Description("Inspect frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null,
        [Description("Skip build (optional)")] bool noBuild = false)
    {
        return ExecuteStandardMcpCommand("inspect", backend, frontend, selfContainedSystem, args =>
            {
                if (noBuild) args.Add("--no-build");
            }
        );
    }

    [McpServerTool]
    [Description("Run all checks (build + test + format + inspect). Checks both backend and frontend by default. Use backend/frontend flags to check only one.")]
    public static string Check(
        [Description("Check backend only (optional)")]
        bool backend = false,
        [Description("Check frontend only (optional)")]
        bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null)
    {
        return ExecuteStandardMcpCommand("check", backend, frontend, selfContainedSystem);
    }

    [McpServerTool]
    [Description("Start .NET Aspire at https://localhost:9000")]
    public static string Watch()
    {
        // Call watch command in detached mode - don't wait for process exit
        var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");
        var args = new List<string> { "run", "--project", developerCliPath, "watch", "--detach", "--force" };

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
            if (e.Data != null) output.Add(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null) errors.Add(e.Data);
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
    [Description("Run E2E tests")]
    public static string E2E(
        [Description("Search terms")] string[] searchTerms = null!,
        [Description("Browser")] string browser = "all",
        [Description("Smoke only")] bool smoke = false)
    {
        var args = new List<string> { "e2e", "--quiet" };
        if (searchTerms is { Length: > 0 }) args.AddRange(searchTerms);
        if (browser != "all")
        {
            args.Add("--browser");
            args.Add(browser);
        }

        if (smoke) args.Add("--smoke");

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? $"E2E tests completed.\n\n{result.Output}" : $"E2E tests failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Initialize task-manager directory in .workspace as separate git repository")]
    public static string InitTaskManager()
    {
        var result = ExecuteCliCommand(["init-task-manager"]);
        return result.Success ? "Task-manager initialized successfully" : $"Failed to initialize task-manager.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("Sync AI rules from .claude to .windsurf and .cursor. CRITICAL: Always make AI rule changes in .claude folder first, then sync")]
    public static string SyncAiRules()
    {
        var result = ExecuteCliCommand(["sync-ai-rules"]);
        return result.Success ? "AI rules synced successfully to .windsurf and .cursor" : $"Failed to sync AI rules.\n\n{result.Output}";
    }

    private static (bool Success, string Output) ExecuteCliCommand(string[] args)
    {
        var outputLines = new List<string>();
        var errorLines = new List<string>();

        var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");
        var allArgs = new List<string> { "run", "--project", developerCliPath, "--" };
        allArgs.AddRange(args);

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = string.Join(" ", allArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
            WorkingDirectory = Configuration.SourceCodeFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process();
        process.StartInfo = processStartInfo;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) outputLines.Add(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) errorLines.Add(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var allOutput = string.Join("\n", outputLines);
        if (errorLines.Count > 0) allOutput += "\n\nErrors:\n" + string.Join("\n", errorLines);

        return (process.ExitCode == 0, allOutput);
    }

    private static (bool Success, string Output, string TempFilePath) ExecuteCliCommandQuietly(string[] args)
    {
        var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");
        var allArgs = new List<string> { "run", "--project", developerCliPath, "--" };
        allArgs.AddRange(args);

        var command = $"dotnet {string.Join(" ", allArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))}";
        var result = ProcessHelper.ExecuteQuietly(command, Configuration.SourceCodeFolder);

        return (result.Success, result.CombinedOutput, result.TempFilePath);
    }

    private static string ExecuteStandardMcpCommand(
        string commandName,
        bool backend = false,
        bool frontend = false,
        string? selfContainedSystem = null,
        Action<List<string>>? additionalArgs = null)
    {
        var args = new List<string> { commandName, "--quiet" };

        if (backend && !frontend) args.Add("--backend");
        if (frontend && !backend) args.Add("--frontend");
        if (selfContainedSystem is not null)
        {
            args.Add("--self-contained-system");
            args.Add(selfContainedSystem);
        }

        additionalArgs?.Invoke(args);

        var result = ExecuteCliCommandQuietly(args.ToArray());

        if (result.Success)
        {
            return result.Output;
        }

        return $"{commandName} failed.\n\n{result.Output}\n\nFull output: {result.TempFilePath}";
    }
}
