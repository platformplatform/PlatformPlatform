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
                }
            );
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
    [McpServerTool]
    [Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, or code review. The agent will work autonomously and return results.")]
    public static async Task<string> StartWorker(
        [Description("Agent type (backend-reviewer, frontend-reviewer, coordinator, quality-gate-committer)")]
        string agentType,
        [Description("Short title for the task")]
        string taskTitle,
        [Description("Task content in markdown format")]
        string markdownContent)
    {
        try
        {
            var validAgentTypes = new[] { "backend-reviewer", "frontend-reviewer", "coordinator", "quality-gate-committer" };
            if (!validAgentTypes.Contains(agentType))
            {
                throw new ArgumentException($"Invalid agent type '{agentType}'. Valid types: {string.Join(", ", validAgentTypes)}");
            }

            var branchName = GetCurrentGitBranch();
            var branchWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".claude", "agent-workspaces", branchName);
            var messagesDirectory = Path.Combine(branchWorkspaceDir, "messages");
            Directory.CreateDirectory(messagesDirectory);

            var counterFile = Path.Combine(messagesDirectory, ".task-counter");
            var counter = 1;
            if (File.Exists(counterFile))
            {
                if (int.TryParse(await File.ReadAllTextAsync(counterFile), out var currentCounter))
                {
                    counter = currentCounter + 1;
                }
            }

            await File.WriteAllTextAsync(counterFile, counter.ToString());

            var shortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))
                .ToLowerInvariant().Replace(".", "").Replace(",", "");
            var requestFileName = $"{counter:D4}.{agentType}.request.{shortTitle}.md";
            var requestFilePath = Path.Combine(messagesDirectory, requestFileName);

            await File.WriteAllTextAsync(requestFilePath, markdownContent);

            var agentWorkspaceDir = Path.Combine(branchWorkspaceDir, agentType);
            Directory.CreateDirectory(agentWorkspaceDir);

            // Copy and customize CLAUDE.md for Worker priming
            await SetupWorkerPrimingAsync(agentWorkspaceDir, agentType);

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

            // Monitor for response file creation with FileSystemWatcher
            return await WaitForWorkerCompletionAsync(messagesDirectory, counter, agentType, process.Id, taskTitle, requestFileName);
        }
        catch (Exception ex)
        {
            return $"Error starting worker: {ex.Message}";
        }
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

    [McpServerTool]
    [Description("Stop a development agent that is taking too long or needs to be cancelled. Use when work needs to be interrupted.")]
    public static string KillWorker([Description("Process ID of Worker to terminate")] int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            process.Kill();
            return $"Terminated worker PID: '{processId}'";
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

    private static async Task SetupWorkerPrimingAsync(string agentWorkspaceDir, string agentType)
    {
        try
        {
            // Copy root repository CLAUDE.md to Worker workspace
            var rootClaudeMd = Path.Combine(Configuration.SourceCodeFolder, "CLAUDE.md");
            var workerClaudeMd = Path.Combine(agentWorkspaceDir, "CLAUDE.md");

            if (File.Exists(rootClaudeMd))
            {
                var rootContent = await File.ReadAllTextAsync(rootClaudeMd);

                // Read Worker-specific profile
                var workerProfilePath = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agents", $"{agentType}.md");
                var workerProfile = "";

                if (File.Exists(workerProfilePath))
                {
                    workerProfile = await File.ReadAllTextAsync(workerProfilePath);
                }

                // Insert Worker profile after the frontmatter section (after closing ---)
                var lines = rootContent.Split('\n');
                var frontmatterEnd = -1;
                var inFrontmatter = false;

                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].Trim() == "---")
                    {
                        if (!inFrontmatter)
                        {
                            inFrontmatter = true; // Start of frontmatter
                        }
                        else
                        {
                            frontmatterEnd = i; // End of frontmatter
                            break;
                        }
                    }
                }

                // Combine content with Worker profile
                string combinedContent;
                if (frontmatterEnd >= 0)
                {
                    var beforeProfile = string.Join('\n', lines.Take(frontmatterEnd + 1));
                    var afterProfile = string.Join('\n', lines.Skip(frontmatterEnd + 1));
                    combinedContent = $"{beforeProfile}\n\n{workerProfile}\n\n{afterProfile}";
                }
                else
                {
                    combinedContent = $"{rootContent}\n\n{workerProfile}";
                }

                await File.WriteAllTextAsync(workerClaudeMd, combinedContent);
            }
        }
        catch (Exception)
        {
            // Silent fallback - Worker will use default behavior if priming fails
        }
    }

    private static async Task<string> WaitForWorkerCompletionAsync(string messagesDirectory, int counter, string agentType, int processId, string taskTitle, string requestFileName)
    {
        try
        {
            var responsePattern = $"{counter:D4}.{agentType}.response.*.md";
            var responseDetected = false;
            string? responseFilePath = null;

            // Create FileSystemWatcher to monitor for response file creation
            using var fileSystemWatcher = new FileSystemWatcher(messagesDirectory, responsePattern);
            fileSystemWatcher.Created += (_, e) =>
            {
                responseDetected = true;
                responseFilePath = e.FullPath;
            };

            fileSystemWatcher.EnableRaisingEvents = true;

            // Wait for response file with timeout (max 2 hours)
            var timeout = TimeSpan.FromHours(2);
            var startTime = DateTime.Now;

            while (!responseDetected && DateTime.Now - startTime < timeout)
            {
                // Check if process is still running
                try
                {
                    var process = Process.GetProcessById(processId);
                    if (process.HasExited)
                    {
                        return $"Worker process '{processId}' exited unexpectedly (Exit code: {process.ExitCode})";
                    }
                }
                catch (ArgumentException)
                {
                    return $"Worker process '{processId}' terminated unexpectedly";
                }

                await Task.Delay(1000); // Check every second
            }

            if (!responseDetected)
            {
                return $"Timeout: Worker '{processId}' did not complete task '{taskTitle}' within 2 hours";
            }

            // Implement 5-second grace period after response file creation
            await Task.Delay(5000);

            // Read response file contents
            if (responseFilePath != null && File.Exists(responseFilePath))
            {
                var responseContent = await File.ReadAllTextAsync(responseFilePath);
                return $"Worker '{processId}' completed task '{taskTitle}'.\nRequest: {requestFileName}\nResponse: {Path.GetFileName(responseFilePath)}\n\nResponse content:\n{responseContent}";
            }

            return $"Error: Response file '{responseFilePath}' was created but cannot be read";
        }
        catch (Exception ex)
        {
            return $"Error monitoring worker completion: {ex.Message}";
        }
    }
}
