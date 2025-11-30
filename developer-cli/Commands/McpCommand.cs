using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        [Description("Self-contained system, e.g., 'account-management' (optional)")]
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
    public static async Task<string> E2E(
        [Description("Search terms")] string[]? searchTerms = null,
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

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Sync AI rules from .claude to .windsurf and .cursor. CRITICAL: Always make AI rule changes in .claude folder first, then sync")]
    public static async Task<string> SyncAiRules()
    {
        return await ExecuteCliCommandAsync(["sync-ai-rules"]);
    }

    [McpServerTool]
    [Description("""
                 ⚠️ MANDATORY: Report bugs in the AGENTIC SYSTEM (workflows, MCP tools, message protocols, agent communication).

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

                 Reports: .workspace/agent-workspaces/{branch}/feedback-reports/problems/HH-MM-SS-severity-title.md
                 """
    )]
    public static string ReportProblem(
        [Description("Your agent type (e.g., backend-engineer, coordinator, backend-reviewer)")]
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
            sanitizedTitle = Regex.Replace(sanitizedTitle, "[^a-z0-9-]", "");
            // Fallback to "untitled" if sanitization results in empty string
            if (string.IsNullOrEmpty(sanitizedTitle)) sanitizedTitle = "untitled";

            // Generate timestamp and report ID (use local time)
            var now = DateTime.Now;
            var timeStamp = now.ToString("HH-mm-ss");
            var reportId = $"{now:yyyy-MM-dd}-{timeStamp}-{reporter}-{severity}-{sanitizedTitle}";
            var fileName = $"{timeStamp}-{severity}-{sanitizedTitle}.md";

            // Create directory structure
            var reportsDirectory = reporter is "pair-programmer" or "tech-lead"
                ? Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", reporter, "feedback-reports", "problems")
                : Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "feedback-reports", "problems");
            Directory.CreateDirectory(reportsDirectory);

            // Build report content
            var reportContent = $$"""
                                  ---
                                  report-id: {{reportId}}
                                  timestamp: {{now:yyyy-MM-ddTHH:mm:ss}}
                                  reporter: {{reporter}}
                                  severity: {{severity}}
                                  location: {{location}}
                                  status: open
                                  ---

                                  # {{title}}

                                  ## Description
                                  {{description}}

                                  ## Context
                                  {{location}}
                                  {{(suggestedFix is not null ? $"\n## Suggested Fix\n{suggestedFix}" : "")}}
                                  """;

            // Write report file
            var reportPath = Path.Combine(reportsDirectory, fileName);
            File.WriteAllText(reportPath, reportContent);

            return $$"""
                     ✓ Problem reported successfully
                     Report ID: {{reportId}}
                     Location: .workspace/agent-workspaces/{{branch}}/feedback-reports/problems/{{fileName}}

                     You can continue working. This issue will be reviewed.
                     """;
        }
        catch (Exception ex)
        {
            return $"Failed to write problem report: {ex.Message}";
        }
    }

    [McpServerTool]
    [Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, E2E testing, or code review. The agent will work autonomously and return results.")]
    public static async Task<string> StartWorkerAgent(
        [Description("Your agent type (who is sending this delegation)")]
        string senderAgentType,
        [Description("Target agent type (backend-engineer, backend-reviewer, frontend-engineer, frontend-reviewer, qa-engineer, qa-reviewer)")]
        string targetAgentType,
        [Description("Short title for the task")]
        string taskTitle,
        [Description("Task content in markdown format")]
        string markdownContent,
        [Description("Branch name to ensure all agents work on same branch")]
        string branch,
        [Description("Identifier for [feature] in [PRODUCT_MANAGEMENT_TOOL] (optional - use for regular tasks, omit for ad-hoc work)")]
        string? featureId,
        [Description("Identifier for [task] in [PRODUCT_MANAGEMENT_TOOL] (required)")]
        string taskId,
        [Description("Set resetMemory to true only on your first delegation to a downstream agent when working on a new task. Set to false for all re-delegations and for peer-to-peer messages to other engineers.")]
        bool resetMemory,
        [Description("Engineer's request file path (optional, for review tasks)")]
        string? requestFilePath = null,
        [Description("Engineer's response file path (optional, for review tasks)")]
        string? responseFilePath = null)
    {
        // Validate required parameters
        if (string.IsNullOrWhiteSpace(taskId))
        {
            return "Error: Parameter `taskId` is required and must not be empty. `taskId` must match the actual [task] identifier from [PRODUCT_MANAGEMENT_TOOL].";
        }

        // Prevent self-delegation: Engineers cannot delegate to themselves
        if (senderAgentType == targetAgentType)
        {
            return $"Error: Cannot delegate to yourself. You ARE {targetAgentType}.";
        }

        // Check if target engineer is already working on a task
        var targetWorkspace = new Workspace(targetAgentType, branch);
        if (File.Exists(targetWorkspace.CurrentTaskFile))
        {
            try
            {
                var taskJson = await File.ReadAllTextAsync(targetWorkspace.CurrentTaskFile);
                var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

                if (taskInfo is not null)
                {
                    var startedAt = DateTime.Parse(taskInfo.StartedAt);
                    var elapsed = DateTime.Now - startedAt;
                    var elapsedFormatted = elapsed.TotalMinutes >= 1
                        ? $"{(int)elapsed.TotalMinutes}m{elapsed.Seconds}s"
                        : $"{elapsed.Seconds}s";

                    // If same taskId, this is recovery - allow delegation (will monitor existing request)
                    if (taskInfo.TaskId == taskId)
                    {
                        await Console.Error.WriteLineAsync($"[MCP] Task {taskId} already active (request #{taskInfo.TaskNumber}). Will monitor existing request...");
                        // Continue with delegation - ClaudeAgentCommand will detect duplicate and monitor existing
                    }
                    else if (taskId.StartsWith("ad-hoc-"))
                    {
                        // For ad-hoc work, reject if engineer is busy with different task
                        return $"""
                                Error: Cannot delegate ad-hoc work to {targetAgentType} - engineer is currently busy.

                                The {targetAgentType} has been working on task "{taskInfo.TaskTitle}" for {elapsedFormatted}.

                                Please try again in a few minutes (e.g., use a sleep function). You can call it again when it's done.
                                """;
                    }
                    else
                    {
                        // For regular tasks, warn but allow (worker-host will queue the request)
                        await Console.Error.WriteLineAsync($"[MCP] Warning: {targetAgentType} is currently busy with task {taskInfo.TaskId} ({elapsedFormatted}). New request will be queued.");
                        // Continue with delegation - request will be queued
                    }
                }
            }
            catch (Exception ex)
            {
                // Fallback if we can't read/parse the file - continue with delegation
                await Console.Error.WriteLineAsync($"[MCP] Warning: Could not read current-task.json: {ex.Message}");
            }
        }

        // Thin wrapper - calls the claude-agent CLI command in MCP mode
        var args = new List<string> { "claude-agent", targetAgentType, "--mcp", "--task-title", taskTitle, "--markdown-content", markdownContent, "--branch", branch };

        if (!string.IsNullOrEmpty(featureId))
        {
            args.Add("--feature-id");
            args.Add(featureId);
        }

        args.Add("--task-id");
        args.Add(taskId);

        args.Add("--reset-memory");
        args.Add(resetMemory.ToString().ToLower());

        args.Add("--sender-agent-type");
        args.Add(senderAgentType);

        if (requestFilePath is not null)
        {
            args.Add("--request-file-path");
            args.Add(requestFilePath);
        }

        if (responseFilePath is not null)
        {
            args.Add("--response-file-path");
            args.Add(responseFilePath);
        }

        return await ExecuteCliCommandAsync(args.ToArray());
    }

    [McpServerTool]
    [Description("Signal completion from worker agent (task or review). Returns success confirmation that will be saved in your conversation history, then terminates session after 5 seconds. This gives you time to see the confirmation before session ends.")]
    public static async Task<string> CompleteWork(
        [Description("Completion mode: 'task' or 'review'")]
        string mode,
        [Description("Agent type (e.g., backend-engineer, frontend-reviewer, qa-engineer)")]
        string agentType,
        [Description("Full response content in markdown")]
        string responseContent,
        [Description("Branch name to validate workspace consistency")]
        string branch,
        [Description("Mandatory feedback using category prefixes. Use [system] for workflow/MCP tools/agent coordination issues. Use [requirements] for requirements/acceptance criteria clarity. Use [code] for code patterns/rules/architecture guidance. Examples: '[system] CompleteWork returned errors until title was less than 100 characters - consider adding format description' or '[requirements] Task mentioned Admin but unclear if TenantAdmin or WorkspaceAdmin'. Can provide multiple categorized items.")]
        string feedback,
        [Description("Brief task summary in sentence case (for task mode only, e.g., 'Api endpoints implemented')")]
        string? taskSummary = null,
        [Description("Commit hash containing approved changes (for review mode - approved only)")]
        string? commitHash = null,
        [Description("Rejection reason (for review mode - rejected only)")]
        string? rejectReason = null)
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            return "Error: feedback is required. Use category prefixes: [system] for workflow/MCP tools, [requirements] for requirements clarity, [code] for code guidance. Example: '[system] StartWorkerAgent unclear error when busy'";
        }

        try
        {
            var workspace = new Workspace(agentType, branch);

            if (!File.Exists(workspace.CurrentTaskFile))
            {
                // Recovery mode: current-task.json missing, skip feedback and proceed to completion
                if (mode is "task")
                {
                    return await ClaudeAgentLifecycle.CompleteAndExitTask(agentType, taskSummary ?? "Task recovered", responseContent, branch);
                }

                return await ClaudeAgentLifecycle.CompleteAndExitReview(agentType, commitHash, rejectReason, responseContent, branch);
            }

            var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
            var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            if (taskInfo is null)
            {
                return "Error: Failed to parse current-task.json for feedback.";
            }

            var feedbackDirectory = agentType is "pair-programmer" or "tech-lead"
                ? Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", agentType, "feedback-reports", "evaluations")
                : Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "feedback-reports", "evaluations");
            Directory.CreateDirectory(feedbackDirectory);

            // Sanitize task summary for filename (same logic as ReportProblem)
            var sanitizedSummary = mode is "task" && !string.IsNullOrEmpty(taskSummary)
                ? string.Join("-", taskSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries))
                : mode is "review"
                    ? string.IsNullOrEmpty(rejectReason) ? "approved" : "rejected"
                    : "feedback";
            sanitizedSummary = Regex.Replace(sanitizedSummary, "[^a-z0-9-]", "");
            // Fallback to "feedback" if sanitization results in empty string
            if (string.IsNullOrEmpty(sanitizedSummary)) sanitizedSummary = "feedback";

            var feedbackContent = $"""
                                   ---
                                   task-number: {taskInfo.TaskNumber}
                                   task-id: {taskInfo.TaskId}
                                   timestamp: {DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}
                                   agent-type: {agentType}
                                   mode: {mode}
                                   ---

                                   # Task Feedback

                                   {feedback}
                                   """;

            var feedbackFileName = $"{taskInfo.TaskNumber}.{agentType}.feedback.{sanitizedSummary}.md";
            var feedbackFilePath = Path.Combine(feedbackDirectory, feedbackFileName);
            await File.WriteAllTextAsync(feedbackFilePath, feedbackContent);
        }
        catch (Exception ex)
        {
            return $"Error: Failed to save feedback: {ex.Message}";
        }

        if (mode is "task")
        {
            if (string.IsNullOrEmpty(taskSummary))
            {
                return "Error: taskSummary is required for task mode";
            }

            try
            {
                return await ClaudeAgentLifecycle.CompleteAndExitTask(agentType, taskSummary, responseContent, branch);
            }
            catch (Exception ex)
            {
                return $"""
                        Error: Failed to complete task.

                        Mode: {mode}
                        Agent: {agentType}
                        Summary: {taskSummary}
                        Branch: {branch}

                        Details: {ex.Message}
                        """;
            }
        }

        if (mode is "review")
        {
            try
            {
                return await ClaudeAgentLifecycle.CompleteAndExitReview(agentType, commitHash, rejectReason, responseContent, branch);
            }
            catch (Exception ex)
            {
                return $"""
                        Error: Failed to complete review.

                        Mode: {mode}
                        Agent: {agentType}
                        Commit: {commitHash ?? "(none)"}
                        Reject Reason: {rejectReason ?? "(none)"}
                        Branch: {branch}

                        Details: {ex.Message}
                        """;
            }
        }

        return $"Invalid mode: '{mode}'. Valid modes: task, review";
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
