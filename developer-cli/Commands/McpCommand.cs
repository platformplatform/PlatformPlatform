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

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Configuration.SourceCodeFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Use ArgumentList instead of Arguments to avoid shell parsing issues
        processStartInfo.ArgumentList.Add("run");
        processStartInfo.ArgumentList.Add("--project");
        processStartInfo.ArgumentList.Add(developerCliPath);
        processStartInfo.ArgumentList.Add("--");
        foreach (var arg in args)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

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

[McpServerToolType]
public static class WorkerMcpTools
{
    [McpServerTool]
    [Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, test automation, or code review. The agent will work autonomously and return results.")]
    public static string StartWorker(
        [Description("Worker type (backend-engineer, backend-reviewer, frontend-engineer, frontend-reviewer, test-automation-engineer, test-automation-reviewer)")]
        string agentType,
        [Description("Short title for the task")]
        string taskTitle,
        [Description("Task content in markdown format")]
        string markdownContent,
        [Description("PRD file path (optional, for Product Increment tasks)")]
        string? prdPath = null,
        [Description("Product Increment file path (optional, for Product Increment tasks)")]
        string? productIncrementPath = null,
        [Description("Task number or title (optional, for Product Increment tasks)")]
        string? taskNumber = null,
        [Description("Engineer's request file path (optional, for review tasks)")]
        string? requestFilePath = null,
        [Description("Engineer's response file path (optional, for review tasks)")]
        string? responseFilePath = null)
    {
        // Thin wrapper - calls the claude-agent CLI command in MCP mode
        var args = new List<string> { "claude-agent", agentType, "--mcp", "--task-title", taskTitle, "--markdown-content", markdownContent };

        if (prdPath != null)
        {
            args.Add("--prd-path");
            args.Add(prdPath);
        }

        if (productIncrementPath != null)
        {
            args.Add("--product-increment-path");
            args.Add(productIncrementPath);
        }

        if (taskNumber != null)
        {
            args.Add("--task-number");
            args.Add(taskNumber);
        }

        if (requestFilePath != null)
        {
            args.Add("--request-file-path");
            args.Add(requestFilePath);
        }

        if (responseFilePath != null)
        {
            args.Add("--response-file-path");
            args.Add(responseFilePath);
        }

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? result.Output : $"StartWorker failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("View the details of a development task that was assigned to an agent. Use this to check what work was requested.")]
    public static string ReadTaskFile([Description("Path to task file to read")] string filePath)
    {
        try
        {
            return File.Exists(filePath) ? File.ReadAllText(filePath) : $"File not found: '{filePath}'";
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Check which development agents are currently working on tasks. Shows what work is in progress.")]
    public static string ListActiveWorkers()
    {
        return ClaudeAgentLifecycle.GetActiveWorkersList();
    }

    [McpServerTool]
    [Description("Stop a development agent that is taking too long or needs to be cancelled. Use when work needs to be interrupted.")]
    public static string KillWorker([Description("Process ID of Worker to terminate")] int processId)
    {
        return ClaudeAgentLifecycle.TerminateWorker(processId);
    }

    [McpServerTool]
    [Description("⚠️ TERMINATES SESSION IMMEDIATELY ⚠️ Signal task completion from worker agent. Call this when you have finished implementing a task. This will write your response file and immediately terminate your session. There is no going back after this call.")]
    public static async Task<string> CompleteAndExitTask(
        [Description("Agent type (backend-engineer, frontend-engineer, test-automation-engineer)")]
        string agentType,
        [Description("Brief task summary in sentence case (e.g., 'Api endpoints implemented')")]
        string taskSummary,
        [Description("Full response content in markdown")]
        string responseContent)
    {
        return await ClaudeAgentLifecycle.CompleteAndExitTask(agentType, taskSummary, responseContent);
    }

    [McpServerTool]
    [Description("⚠️ TERMINATES SESSION IMMEDIATELY ⚠️ Signal review completion. Approved: provide commitHash. Rejected: provide rejectReason. Never both.")]
    public static async Task<string> CompleteAndExitReview(
        [Description("Agent type (backend-reviewer, frontend-reviewer, test-automation-reviewer)")]
        string agentType,
        [Description("Commit hash containing approved changes (approved only)")]
        string? commitHash,
        [Description("Rejection reason (rejected only)")]
        string? rejectReason,
        [Description("Concise but precise review in markdown")]
        string responseContent)
    {
        return await ClaudeAgentLifecycle.CompleteAndExitReview(agentType, commitHash, rejectReason, responseContent);
    }

    private static (bool Success, string Output) ExecuteCliCommand(string[] args)
    {
        var outputLines = new List<string>();

        var developerCliPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = Configuration.SourceCodeFolder,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        // Use ArgumentList instead of Arguments to avoid shell parsing issues
        processStartInfo.ArgumentList.Add("run");
        processStartInfo.ArgumentList.Add("--project");
        processStartInfo.ArgumentList.Add(developerCliPath);
        processStartInfo.ArgumentList.Add("--");
        foreach (var arg in args)
        {
            processStartInfo.ArgumentList.Add(arg);
        }

        using var process = new Process();
        process.StartInfo = processStartInfo;

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) outputLines.Add(e.Data);
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) outputLines.Add(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        var allOutput = string.Join("\n", outputLines);

        return (process.ExitCode == 0, allOutput);
    }
}
