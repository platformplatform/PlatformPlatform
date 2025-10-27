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
    public static string ExecuteCommand(
        [Description("Command to execute: 'build', 'test', 'format', or 'inspect'")]
        string command,
        [Description("Backend")] bool backend = false,
        [Description("Frontend")] bool frontend = false,
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
        string? selfContainedSystem = null,
        [Description("Skip build (for test, format, inspect only)")]
        bool noBuild = true,
        [Description("Filter tests by name (test command only)")]
        string? filter = null)
    {
        var validCommands = new[] { "build", "test", "format", "inspect" };
        if (!validCommands.Contains(command))
        {
            return $"Invalid command: '{command}'. Valid commands: {string.Join(", ", validCommands)}";
        }

        return ExecuteStandardMcpCommand(command, backend, frontend, selfContainedSystem, args =>
            {
                if (noBuild && command != "build")
                {
                    args.Add("--no-build");
                }

                if (filter != null && command == "test")
                {
                    args.Add("--filter");
                    args.Add(filter);
                }
            }
        );
    }

    [McpServerTool]
    [Description("Restart .NET Aspire and run database migrations at https://localhost:9000. Runs in the background so you can continue working while it starts.")]
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
    public static string E2e(
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

        var result = ExecuteCliCommandQuietly(args.ToArray());
        return result.Output;
    }

    [McpServerTool]
    [Description("Sync AI rules from .claude to .windsurf and .cursor. CRITICAL: Always make AI rule changes in .claude folder first, then sync")]
    public static string SyncAiRules()
    {
        var result = ExecuteCliCommand(["sync-ai-rules"]);
        return result.Success ? "AI rules synced successfully to .windsurf and .cursor" : $"Failed to sync AI rules.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description(@"⚠️ MANDATORY: Report bugs in the AGENTIC SYSTEM (workflows, MCP tools, message protocols, agent communication).

YOU ARE PART OF AN AGENTIC SYSTEM:
- Multiple agents communicate via MCP tools and message files
- This tool is for fixing bugs in: MCP tool contracts, /commands workflows, system prompts, message file protocols, agent communication patterns

MUST REPORT system/workflow bugs:
✓ Workflow says read file at path X but file doesn't exist there
✓ MCP tool returns errors or has wrong parameter descriptions
✓ Instructions reference non-existent tools or commands
✓ Message file missing expected JSON fields
✓ Agent called with parameters that don't match interface
✓ Conflicting instructions in different workflow files

DO NOT REPORT:
✗ Feature implementation issues (wrong business logic, missing validation)
✗ Code quality problems (bad patterns, missing tests)
✗ User's PRD or requirements unclear
✗ Your own implementation bugs

IF YOU RECOVER - REPORT PROBLEM + SOLUTION:
1. Report the system bug (severity: error/warning)
2. Find workaround and continue
3. Report what actually worked (severity: info, include solution)

Example: Workflow says read current-task.json from workspace root, but actual path is .workspace/agent-workspaces/branch/agent/current-task.json - report BOTH.

Reports: .workspace/agent-workspaces/{branch}/feedback-reports/problems/HH-MM-SS-severity-title.md"
    )]
    public static string ReportProblem(
        [Description("Your agent type (e.g., backend-engineer, tech-lead, backend-reviewer)")]
        string reporter,
        [Description("Severity: 'error' (blocking issue), 'warning' (should fix), 'info' (suggestion)")]
        string severity,
        [Description("Short title describing the problem")]
        string title,
        [Description("Detailed explanation of what went wrong and what you were trying to do")]
        string description,
        [Description("Where the issue occurred (file path:line or context)")]
        string location,
        [Description("Optional: Your suggestion for how to fix this issue")]
        string? suggestedFix = null)
    {
        // Validate severity
        var validSeverities = new[] { "error", "warning", "info" };

        if (!validSeverities.Contains(severity))
        {
            return $"Invalid severity: '{severity}'. Valid severities: {string.Join(", ", validSeverities)}";
        }

        try
        {
            // Get current branch
            var branch = GitHelper.GetCurrentBranch();

            // Sanitize title for filename (same as ClaudeAgentCommand.cs)
            var sanitizedTitle = string.Join("-", title.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                .ToLowerInvariant();
            // Remove all special characters, keep only alphanumeric and hyphens
            sanitizedTitle = System.Text.RegularExpressions.Regex.Replace(sanitizedTitle, @"[^a-z0-9-]", "");

            // Generate timestamp and report ID (use local time)
            var now = DateTime.Now;
            var timeStamp = now.ToString("HH-mm-ss");
            var reportId = $"{now:yyyy-MM-dd}-{timeStamp}-{reporter}-{severity}-{sanitizedTitle}";
            var fileName = $"{timeStamp}-{severity}-{sanitizedTitle}.md";

            // Create directory structure
            var reportsDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "feedback-reports", "problems");
            Directory.CreateDirectory(reportsDirectory);

            // Build report content
            var reportContent = $@"---
report-id: {reportId}
timestamp: {now:yyyy-MM-ddTHH:mm:ss}
reporter: {reporter}
severity: {severity}
location: {location}
status: open
---

# {title}

## Description
{description}

## Context
{location}
{(suggestedFix != null ? $"\n## Suggested Fix\n{suggestedFix}" : "")}
";

            // Write report file
            var reportPath = Path.Combine(reportsDirectory, fileName);
            File.WriteAllText(reportPath, reportContent);

            return $@"✓ Problem reported successfully
Report ID: {reportId}
Location: .workspace/agent-workspaces/{branch}/feedback-reports/problems/{fileName}

You can continue working. This issue will be reviewed.";
        }
        catch (Exception ex)
        {
            return $"Failed to write problem report: {ex.Message}";
        }
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
        bool backend,
        bool frontend,
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

        return ExecuteCliCommandQuietly(args.ToArray()).Output;
    }
}

[McpServerToolType]
public static class WorkerMcpTools
{
    [McpServerTool]
    [Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, test automation, or code review. The agent will work autonomously and return results.")]
    public static string StartWorkerAgent(
        [Description("Worker type (backend-engineer, backend-reviewer, frontend-engineer, frontend-reviewer, test-automation-engineer, test-automation-reviewer)")]
        string agentType,
        [Description("Short title for the task")]
        string taskTitle,
        [Description("Task content in markdown format")]
        string markdownContent,
        [Description("Branch name to ensure all agents work on same branch")]
        string branch,
        [Description("Identifier for [task] in [PRODUCT_MANAGEMENT_TOOL] (required, must be distinct from storyId)")]
        string taskId,
        [Description("Identifier for [story] in [PRODUCT_MANAGEMENT_TOOL] (required)")]
        string storyId,
        [Description("Set resetMemory to true only on your first delegation to a downstream agent when working on a new story. Set to false for all re-delegations and for peer-to-peer messages to other engineers.")]
        bool resetMemory,
        [Description("Engineer's request file path (optional, for review tasks)")]
        string? requestFilePath = null,
        [Description("Engineer's response file path (optional, for review tasks)")]
        string? responseFilePath = null)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(taskId))
        {
            throw new ArgumentException("Parameter `taskId` is required and must not be empty. `taskId` must match the actual [task] identifier from [PRODUCT_MANAGEMENT_TOOL] and must be distinct from `storyId`.");
        }

        // Thin wrapper - calls the claude-agent CLI command in MCP mode
        var args = new List<string> { "claude-agent", agentType, "--mcp", "--task-title", taskTitle, "--markdown-content", markdownContent, "--branch", branch };

        args.Add("--story-id");
        args.Add(storyId);

        args.Add("--task-id");
        args.Add(taskId);

        args.Add("--reset-memory");
        args.Add(resetMemory.ToString().ToLower());

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
    [Description("Signal completion from worker agent (task or review). Returns success confirmation that will be saved in your conversation history, then terminates session after 5 seconds. This gives you time to see the confirmation before session ends.")]
    public static async Task<string> CompleteWork(
        [Description("Completion mode: 'task' or 'review'")]
        string mode,
        [Description("Agent type (e.g., backend-engineer, frontend-reviewer, test-automation-engineer)")]
        string agentType,
        [Description("Full response content in markdown")]
        string responseContent,
        [Description("Branch name to validate workspace consistency")]
        string branch,
        [Description("Brief task summary in sentence case (for task mode only, e.g., 'Api endpoints implemented')")]
        string? taskSummary = null,
        [Description("Commit hash containing approved changes (for review mode - approved only)")]
        string? commitHash = null,
        [Description("Rejection reason (for review mode - rejected only)")]
        string? rejectReason = null)
    {
        if (mode == "task")
        {
            if (string.IsNullOrEmpty(taskSummary))
            {
                return "Error: taskSummary is required for task mode";
            }

            return await ClaudeAgentLifecycle.CompleteAndExitTask(agentType, taskSummary, responseContent, branch);
        }

        if (mode == "review")
        {
            return await ClaudeAgentLifecycle.CompleteAndExitReview(agentType, commitHash, rejectReason, responseContent, branch);
        }

        return $"Invalid mode: '{mode}'. Valid modes: task, review";
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
