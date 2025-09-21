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
    private static readonly Dictionary<int, WorkerSession> ActiveWorkerSessions = new();
    private static readonly Lock WorkerSessionLock = new();

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

    public static void AddWorkerSession(int processId, string agentType, string taskTitle, string requestFileName, Process process)
    {
        lock (WorkerSessionLock)
        {
            ActiveWorkerSessions[processId] = new WorkerSession(
                processId, agentType, taskTitle, requestFileName, DateTime.Now, process
            );
        }
    }

    public static void RemoveWorkerSession(int processId)
    {
        lock (WorkerSessionLock)
        {
            ActiveWorkerSessions.Remove(processId);
        }
    }

    public static string GetActiveWorkersList()
    {
        lock (WorkerSessionLock)
        {
            if (ActiveWorkerSessions.Count == 0)
            {
                return "No active workers currently";
            }

            var workerList = ActiveWorkerSessions.Values.Select(w =>
                $"PID: {w.ProcessId}, Agent: {w.AgentType}, Task: {w.TaskTitle}, Started: {w.StartTime:HH:mm:ss}, Duration: {DateTime.Now - w.StartTime:mm\\:ss}"
            ).ToList();

            return $"Active workers ({ActiveWorkerSessions.Count}):\n{string.Join("\n", workerList)}";
        }
    }

    public static string TerminateWorker(int processId)
    {
        lock (WorkerSessionLock)
        {
            if (ActiveWorkerSessions.TryGetValue(processId, out var session))
            {
                session.Process.Kill();
                ActiveWorkerSessions.Remove(processId);
                return $"Terminated worker PID: '{processId}' (Agent: {session.AgentType}, Task: {session.TaskTitle})";
            }

            // Fallback to direct process kill if not in our tracking
            try
            {
                var process = Process.GetProcessById(processId);
                process.Kill();
                return $"Terminated untracked worker PID: '{processId}'";
            }
            catch (Exception ex)
            {
                return $"Error terminating worker: {ex.Message}";
            }
        }
    }
}

[McpServerToolType]
public static class WorkerMcpTools
{
    [McpServerTool]
    [Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, or code review. The agent will work autonomously and return results.")]
    public static async Task<string> StartWorker(
        [Description("Worker type (backend-engineer-worker, frontend-engineer-worker, backend-reviewer-worker, frontend-reviewer-worker, coordinator-worker, quality-gate-committer-worker, e2e-test-reviewer-worker)")]
        string agentType,
        [Description("Short title for the task")]
        string taskTitle,
        [Description("Task content in markdown format")]
        string markdownContent)
    {
        Mutex? workspaceMutex = null;
        try
        {
            var validAgentTypes = new[]
            {
                "backend-engineer-worker", "frontend-engineer-worker",
                "backend-reviewer-worker", "frontend-reviewer-worker",
                "coordinator-worker", "quality-gate-committer-worker", "e2e-test-reviewer-worker"
            };
            if (!validAgentTypes.Contains(agentType))
            {
                throw new ArgumentException($"Invalid agent type '{agentType}'. Valid types: {string.Join(", ", validAgentTypes)}");
            }

            var branchName = GetCurrentGitBranch();

            // Acquire workspace lock to ensure only one Worker of this agent type runs per branch
            var mutexName = $"{agentType}-{branchName}";
            workspaceMutex = new Mutex(false, mutexName);

            if (!workspaceMutex.WaitOne(TimeSpan.FromSeconds(5)))
            {
                workspaceMutex.Dispose();
                throw new InvalidOperationException($"Another {agentType} is already active in branch '{branchName}'. Only one {agentType} can work per branch to maintain consistent context and memory.");
            }

            var branchWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".claude", "agent-workspaces", branchName);
            var messagesDirectory = Path.Combine(branchWorkspaceDir, "messages");
            Directory.CreateDirectory(messagesDirectory);

            var counterFile = Path.Combine(messagesDirectory, ".task-counter");
            var counter = 1;
            if (File.Exists(counterFile) && int.TryParse(await File.ReadAllTextAsync(counterFile), out var currentCounter))
            {
                counter = currentCounter + 1;
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

            // Track active Worker session
            ClaudeWorkerAgentMcpCommand.AddWorkerSession(process.Id, agentType, taskTitle, requestFileName, process);

            try
            {
                // Monitor for response file creation with FileSystemWatcher
                return await WaitForWorkerCompletionAsync(messagesDirectory, counter, agentType, process.Id, taskTitle, requestFileName);
            }
            finally
            {
                // Remove from active sessions and release workspace lock
                ClaudeWorkerAgentMcpCommand.RemoveWorkerSession(process.Id);
                workspaceMutex.ReleaseMutex();
                workspaceMutex.Dispose();
            }
        }
        catch (Exception ex)
        {
            // Clean up mutex if Worker startup fails
            if (workspaceMutex is not null)
            {
                try
                {
                    workspaceMutex.ReleaseMutex();
                    workspaceMutex.Dispose();
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }

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
        return ClaudeWorkerAgentMcpCommand.GetActiveWorkersList();
    }

    [McpServerTool]
    [Description("Stop a development agent that is taking too long or needs to be cancelled. Use when work needs to be interrupted.")]
    public static string KillWorker([Description("Process ID of Worker to terminate")] int processId)
    {
        return ClaudeWorkerAgentMcpCommand.TerminateWorker(processId);
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
        // Copy root repository CLAUDE.md to Worker workspace
        var rootClaudeMd = Path.Combine(Configuration.SourceCodeFolder, "CLAUDE.md");
        var workerClaudeMd = Path.Combine(agentWorkspaceDir, "CLAUDE.md");

        if (File.Exists(rootClaudeMd))
        {
            var rootContent = await File.ReadAllTextAsync(rootClaudeMd);

            // Read Worker-specific profile
            var workerProfilePath = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agents", $"{agentType}.md");

            var workerProfile = File.Exists(workerProfilePath) ? await File.ReadAllTextAsync(workerProfilePath) : "";

            // Insert Worker profile after the frontmatter section (after closing ---)
            var lines = rootContent.Split('\n');
            var frontmatterEnd = -1;
            var inFrontmatter = false;

            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Trim() != "---") continue;

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

    private static async Task<string> WaitForWorkerCompletionAsync(string messagesDirectory, int counter, string agentType, int processId, string taskTitle, string requestFileName)
    {
        var responsePattern = $"{counter:D4}.{agentType}.response.*.md";
        var responseDetected = false;
        string? responseFilePath = null;
        var currentProcessId = processId;
        var restartCount = 0;

        using var fileSystemWatcher = new FileSystemWatcher(messagesDirectory, responsePattern);
        fileSystemWatcher.Created += (_, e) =>
        {
            responseDetected = true;
            responseFilePath = e.FullPath;
        };
        fileSystemWatcher.EnableRaisingEvents = true;

        var startTime = DateTime.Now;
        var lastHealthCheck = startTime;
        var overallTimeout = TimeSpan.FromHours(2);

        while (!responseDetected && DateTime.Now - startTime < overallTimeout)
        {
            if (ShouldPerformHealthCheck(lastHealthCheck))
            {
                if (!IsWorkerProcessHealthy(currentProcessId))
                {
                    var healthCheckRestart = await RestartWorker(agentType, messagesDirectory, requestFileName, restartCount + 1);
                    if (!healthCheckRestart.Success)
                    {
                        return $"Worker restart failed: {healthCheckRestart.ErrorMessage}";
                    }

                    if (healthCheckRestart.Process == null)
                    {
                        return "Worker restart succeeded but process is null";
                    }

                    UpdateWorkerSession(currentProcessId, healthCheckRestart.ProcessId, agentType, taskTitle, requestFileName, healthCheckRestart.Process);
                    currentProcessId = healthCheckRestart.ProcessId;
                    restartCount++;
                    LogWorkerActivity($"Worker {agentType} restarted (attempt {restartCount})", messagesDirectory);
                }

                lastHealthCheck = DateTime.Now;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        if (!responseDetected)
        {
            return $"Worker timeout after 2 hours (restarts: {restartCount})";
        }

        await Task.Delay(TimeSpan.FromSeconds(5)); // Grace period for file writing

        if (!File.Exists(responseFilePath))
        {
            return "Response file was detected but no longer exists";
        }

        var responseContent = await File.ReadAllTextAsync(responseFilePath);

        LogWorkerActivity($"Worker {agentType} completed task (restarts: {restartCount})", messagesDirectory);
        return $"Worker completed task '{taskTitle}'.\nRequest: {requestFileName}\nResponse: {Path.GetFileName(responseFilePath)}\nRestarts: {restartCount}\n\nResponse content:\n{responseContent}";
    }

    private static bool ShouldPerformHealthCheck(DateTime lastHealthCheck)
    {
        return DateTime.Now - lastHealthCheck >= TimeSpan.FromMinutes(15);
    }

    private static bool IsWorkerProcessHealthy(int processId)
    {
        var processes = Process.GetProcesses();
        var workerProcess = processes.FirstOrDefault(p => p.Id == processId);
        return workerProcess is { HasExited: false };
    }

    private static async Task<(bool Success, int ProcessId, Process? Process, string ErrorMessage)> RestartWorker(string agentType, string messagesDirectory, string requestFileName, int attemptNumber)
    {
        var branchName = GetCurrentGitBranch();
        var branchWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".claude", "agent-workspaces", branchName);
        var agentWorkspaceDir = Path.Combine(branchWorkspaceDir, agentType);
        var requestFilePath = Path.Combine(messagesDirectory, requestFileName);

        var claudeArgs = new[]
        {
            "--continue",
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--append-system-prompt", $"You are a {agentType} Worker. Restart attempt #{attemptNumber}. Process task: {requestFilePath}"
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
        await Task.Delay(TimeSpan.FromSeconds(2)); // Allow process to initialize

        if (process.HasExited)
        {
            return (false, -1, null, $"Process exited immediately with code: {process.ExitCode}");
        }

        return (true, process.Id, process, "");
    }

    private static void UpdateWorkerSession(int oldProcessId, int newProcessId, string agentType, string taskTitle, string requestFileName, Process newProcess)
    {
        ClaudeWorkerAgentMcpCommand.RemoveWorkerSession(oldProcessId);
        ClaudeWorkerAgentMcpCommand.AddWorkerSession(newProcessId, agentType, taskTitle, requestFileName, newProcess);
    }

    private static void LogWorkerActivity(string message, string messagesDirectory)
    {
        var logFile = Path.Combine(Path.GetDirectoryName(messagesDirectory)!, "workflow.log");
        var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}\n";

        if (!Directory.Exists(Path.GetDirectoryName(logFile)))
        {
            return;
        }

        File.AppendAllText(logFile, logEntry);
    }
}

public record WorkerSession(
    int ProcessId,
    string AgentType,
    string TaskTitle,
    string RequestFileName,
    DateTime StartTime,
    Process Process
);
