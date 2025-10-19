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
        bool noBuild = true)
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
    [Description(@"⚠️ MANDATORY: Report ANY incorrect input (paths, files, tools, parameters, instructions) you receive.

MUST REPORT when input/instructions are WRONG:
✓ Told to read file at wrong path - REPORT IT
✓ Told to call tool that doesn't exist - REPORT IT
✓ Given parameters that don't match your interface - REPORT IT
✓ Receive conflicting instructions - REPORT IT

DO NOT REPORT your own implementation bugs - only incorrect INPUT/INSTRUCTIONS.

IF YOU RECOVER/FIND WORKAROUND - REPORT BOTH PROBLEM AND SOLUTION:
1. First call: Report the problem (severity: error or warning)
   Example - title: Instructions say read current-task.json from workspace root but file not found

2. Then find workaround and continue working

3. Second call: Report your solution (severity: info)
   Example - title: Solution - current-task.json location
   Example - description: Found current-task.json at .workspace/agent-workspaces/branch/agent/current-task.json
   Example - suggestedFix: Update workflow to specify full path

CRITICAL: Reporting both problems AND solutions helps fix the system permanently.

Reports saved to: .workspace/problem-reports/YYYY-MM-DD/HH-MM-SS-severity-category.md")]
    public static string ReportProblem(
        [Description("Your agent type (e.g., backend-engineer, tech-lead, backend-reviewer)")]
        string reporter,
        [Description("Category: 'dead-reference', 'invalid-parameters', 'contradictory-instructions', 'unclear-instructions'")]
        string category,
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
        // Validate inputs
        var validCategories = new[] { "dead-reference", "invalid-parameters", "contradictory-instructions", "unclear-instructions" };
        var validSeverities = new[] { "error", "warning", "info" };

        if (!validCategories.Contains(category))
        {
            return $"Invalid category: '{category}'. Valid categories: {string.Join(", ", validCategories)}";
        }

        if (!validSeverities.Contains(severity))
        {
            return $"Invalid severity: '{severity}'. Valid severities: {string.Join(", ", validSeverities)}";
        }

        try
        {
            // Generate timestamp and report ID
            var now = DateTime.UtcNow;
            var dateFolder = now.ToString("yyyy-MM-dd");
            var timeStamp = now.ToString("HH-mm-ss");
            var reportId = $"{dateFolder}-{timeStamp}-{severity}-{category}";
            var fileName = $"{timeStamp}-{severity}-{category}.md";

            // Create directory structure
            var reportsDir = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "problem-reports", dateFolder);
            Directory.CreateDirectory(reportsDir);

            // Build report content
            var reportContent = $@"---
report-id: {reportId}
timestamp: {now:yyyy-MM-ddTHH:mm:ssZ}
reporter: {reporter}
category: {category}
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
            var reportPath = Path.Combine(reportsDir, fileName);
            File.WriteAllText(reportPath, reportContent);

            return $@"✓ Problem reported successfully
Report ID: {reportId}
Location: .workspace/problem-reports/{dateFolder}/{fileName}

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
        var args = new List<string> { "claude-agent", agentType, "--mcp", "--task-title", taskTitle, "--markdown-content", markdownContent, "--branch", branch };

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

        var result = ExecuteCliCommand(args.ToArray());
        return result.Success ? result.Output : $"StartWorker failed.\n\n{result.Output}";
    }

    [McpServerTool]
    [Description("⚠️ TERMINATES SESSION IMMEDIATELY ⚠️ Signal completion from worker agent (task or review). This will write your response file and immediately terminate your session. There is no going back after this call.")]
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
