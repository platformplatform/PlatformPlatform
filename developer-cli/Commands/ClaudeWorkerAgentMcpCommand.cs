using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ClaudeWorkerAgentMcpCommand : Command
{
    public ClaudeWorkerAgentMcpCommand() : base("claude-worker-agent-mcp", "Start MCP server for agent communication")
    {
        this.SetHandler(ExecuteAsync);
    }

    private async Task ExecuteAsync()
    {
        try
        {
            AnsiConsole.MarkupLine("[green]Starting MCP claude-agent-worker-host server...[/]");
            AnsiConsole.MarkupLine("[dim]Listening on stdio for MCP communication[/]");

            // Use the official SDK hosting pattern from GitHub repo
            var builder = Host.CreateApplicationBuilder();
            builder.Logging.AddConsole(consoleLogOptions =>
            {
                // Configure all logs to go to stderr
                consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
            });
            builder.Services
                .AddMcpServer()
                .WithStdioServerTransport()
                .WithToolsFromAssembly();

            await builder.Build().RunAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]MCP server error: {ex.Message}[/]");
        }
    }
}

[McpServerToolType]
public static class WorkerTools
{
    [McpServerTool, Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, or code review. The agent will work autonomously and return results.")]
    public static string StartWorker(
        [Description("Agent type (backend, frontend, backend-reviewer, frontend-reviewer)")] string agentType,
        [Description("Short title for the task")] string taskTitle,
        [Description("Task content in markdown format")] string markdownContent)
    {
        try
        {
            var validAgentTypes = new[] { "backend", "frontend", "backend-reviewer", "frontend-reviewer" };
            if (!validAgentTypes.Contains(agentType))
            {
                return $"Error: Invalid agent type. Valid types: {string.Join(", ", validAgentTypes)}";
            }

            var branchName = GetCurrentGitBranch();
            var workspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".claude", "agent-workspaces", branchName);
            Directory.CreateDirectory(workspaceDir);

            var counterFile = Path.Combine(workspaceDir, ".task-counter");
            var counter = 1;
            if (File.Exists(counterFile))
            {
                if (int.TryParse(File.ReadAllText(counterFile), out var currentCounter))
                {
                    counter = currentCounter + 1;
                }
            }
            File.WriteAllText(counterFile, counter.ToString());

            var shortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))
                .ToLowerInvariant().Replace(".", "").Replace(",", "");
            var requestFileName = $"{counter:D4}.{agentType}.request.{shortTitle}.md";
            var requestFilePath = Path.Combine(workspaceDir, requestFileName);

            File.WriteAllText(requestFilePath, markdownContent);

            var agentWorkspaceDir = Path.Combine(workspaceDir, agentType);
            Directory.CreateDirectory(agentWorkspaceDir);

            var claudeArgs = new List<string>
            {
                "--continue",
                "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                "--add-dir", Configuration.SourceCodeFolder,
                "--append-system-prompt", $"You are a {agentType} Worker. Process task in shared messages: {requestFilePath}"
            };

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = string.Join(" ", claudeArgs),
                    WorkingDirectory = agentWorkspaceDir,
                    UseShellExecute = false
                }
            };

            process.Start();

            return $"Started {agentType} worker (PID: {process.Id}) for task: {taskTitle}. Request file: {requestFileName}";
        }
        catch (Exception ex)
        {
            return $"Error starting worker: {ex.Message}";
        }
    }

    [McpServerTool, Description("View the details of a development task that was assigned to an agent. Use this to check what work was requested.")]
    public static string ReadTaskFile([Description("Path to task file to read")] string filePath)
    {
        try
        {
            return File.Exists(filePath) ? File.ReadAllText(filePath) : $"File not found: {filePath}";
        }
        catch (Exception ex)
        {
            return $"Error reading file: {ex.Message}";
        }
    }

    [McpServerTool, Description("Check which development agents are currently working on tasks. Shows what work is in progress.")]
    public static string ListActiveWorkers()
    {
        try
        {
            var processes = Process.GetProcesses()
                .Where(p => p.ProcessName.Contains("claude") && !p.HasExited)
                .ToList();

            return processes.Any()
                ? $"Active workers:\n{string.Join("\n", processes.Select(p => $"PID: {p.Id}, Process: {p.ProcessName}"))}"
                : "No active workers currently";
        }
        catch (Exception ex)
        {
            return $"Error listing workers: {ex.Message}";
        }
    }

    [McpServerTool, Description("Stop a development agent that is taking too long or needs to be cancelled. Use when work needs to be interrupted.")]
    public static string KillWorker([Description("Process ID of Worker to terminate")] int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill();
            return $"Terminated worker PID: {processId}";
        }
        catch (Exception ex)
        {
            return $"Error terminating worker: {ex.Message}";
        }
    }

    private static string GetCurrentGitBranch()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "branch --show-current",
                    WorkingDirectory = Configuration.SourceCodeFolder,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var branch = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            return string.IsNullOrEmpty(branch) ? "main" : branch;
        }
        catch
        {
            return "main";
        }
    }
}