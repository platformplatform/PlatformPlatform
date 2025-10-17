using System.Diagnostics;
using System.Text.Json;
using PlatformPlatform.DeveloperCli.Commands;
using PlatformPlatform.DeveloperCli.Installation;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ClaudeAgentLifecycle
{
    private static readonly Dictionary<int, WorkerSession> ActiveWorkerSessions = new();
    private static readonly Lock WorkerSessionLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

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

    public static async Task<string> CompleteAndExitTask(
        string agentType,
        string taskSummary,
        string responseContent)
    {
        var workspace = new Workspace(agentType);

        // Read task number from current-task.json
        if (!File.Exists(workspace.CurrentTaskFile))
        {
            return "Error: No active task found (current-task.json missing). Are you running as a worker agent?";
        }

        var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
        var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);

        if (taskInfo is null)
        {
            return "Error: Failed to deserialize current-task.json";
        }

        var taskId = taskInfo.TaskNumber;

        // Anti-suicide check
        var validationError = await ValidateTaskTiming(workspace.AgentWorkspaceDirectory, "CompleteAndExitTask");
        if (validationError != null) return validationError;

        // Create response filename
        var sanitizedSummary = string.Join("-", taskSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Replace(".", "").Replace(",", "");
        var responseFileName = $"{taskId}.{agentType}.response.{sanitizedSummary}.md";
        var responseFilePath = Path.Combine(workspace.MessagesDirectory, responseFileName);

        // Write response file directly to messages directory
        await File.WriteAllTextAsync(responseFilePath, responseContent);

        // Log completion
        LogWorkflowEvent($"[{taskId}.{agentType}.response] Completed via MCP: '{taskSummary}' -> [{responseFileName}]");

        // Wait for Claude Code to persist session state before killing process
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Read .worker-process-id file to find worker-agent process
        if (File.Exists(workspace.WorkerProcessIdFile))
        {
            var processIdContent = await File.ReadAllTextAsync(workspace.WorkerProcessIdFile);
            if (int.TryParse(processIdContent, out var workerProcessId))
            {
                try
                {
                    // Kill the worker-agent Claude Code process (self-destruct)
                    var workerProcess = Process.GetProcessById(workerProcessId);
                    if (!workerProcess.HasExited)
                    {
                        workerProcess.Kill();
                    }
                }
                catch (ArgumentException)
                {
                    // Process already exited, that's fine
                }
            }
        }

        return $"Task completed. Response file: {responseFileName}";
    }

    public static async Task<string> CompleteAndExitReview(
        string agentType,
        string? commitHash,
        string? rejectReason,
        string responseContent)
    {
        if (!string.IsNullOrWhiteSpace(commitHash) && !string.IsNullOrWhiteSpace(rejectReason))
        {
            throw new InvalidOperationException("Cannot provide both commitHash and rejectReason");
        }

        if (string.IsNullOrWhiteSpace(commitHash) && string.IsNullOrWhiteSpace(rejectReason))
        {
            throw new InvalidOperationException("Must provide either commitHash or rejectReason");
        }

        var approved = !string.IsNullOrEmpty(commitHash);
        var workspace = new Workspace(agentType);

        // Read task number from current-task.json
        if (!File.Exists(workspace.CurrentTaskFile))
        {
            return "Error: No active task found (current-task.json missing). Are you running as a reviewer agent?";
        }

        var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
        var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);

        if (taskInfo is null)
        {
            return "Error: Failed to deserialize current-task.json";
        }

        var taskId = taskInfo.TaskNumber;

        // Anti-suicide check
        var validationError = await ValidateTaskTiming(workspace.AgentWorkspaceDirectory, "CompleteAndExitReview");
        if (validationError is not null) return validationError;

        string reviewSummary;
        string statusPrefix;

        if (approved)
        {
            // Verify commit exists
            var commitCheckResult = ProcessHelper.ExecuteQuietly($"git cat-file -t {commitHash}", Configuration.SourceCodeFolder);
            if (!commitCheckResult.Success || commitCheckResult.StdOut.Trim() != "commit")
            {
                throw new InvalidOperationException($"Commit {commitHash} does not exist");
            }

            // Extract commit message as review summary
            var commitMessageResult = ProcessHelper.ExecuteQuietly($"git log -1 --format=%s {commitHash}", Configuration.SourceCodeFolder);
            reviewSummary = commitMessageResult.StdOut.Trim();

            statusPrefix = "Approved";
        }
        else
        {
            reviewSummary = rejectReason!;
            statusPrefix = "Rejected";
        }

        // Create response filename with status prefix
        var sanitizedSummary = string.Join("-", reviewSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Replace(".", "").Replace(",", "");
        var responseFileName = $"{taskId}.{agentType}.response.{statusPrefix}-{sanitizedSummary}.md";
        var responseFilePath = Path.Combine(workspace.MessagesDirectory, responseFileName);

        // Write response file directly to messages directory
        await File.WriteAllTextAsync(responseFilePath, responseContent);

        // Log completion
        var logMessage = approved
            ? $"[{taskId}.{agentType}.response] Review completed via MCP ({statusPrefix}, commit: {commitHash}): '{reviewSummary}' -> [{responseFileName}]"
            : $"[{taskId}.{agentType}.response] Review completed via MCP ({statusPrefix}): '{reviewSummary}' -> [{responseFileName}]";
        LogWorkflowEvent(logMessage);

        // Wait for Claude Code to persist session state before killing process
        await Task.Delay(TimeSpan.FromSeconds(10));

        // Read .worker-process-id file to find worker-agent process
        if (File.Exists(workspace.WorkerProcessIdFile))
        {
            var processIdContent = await File.ReadAllTextAsync(workspace.WorkerProcessIdFile);
            if (int.TryParse(processIdContent, out var reviewerProcessId))
            {
                try
                {
                    // Kill the reviewer-agent Claude Code process (self-destruct)
                    var reviewerProcess = Process.GetProcessById(reviewerProcessId);
                    if (!reviewerProcess.HasExited)
                    {
                        reviewerProcess.Kill();
                    }
                }
                catch (ArgumentException)
                {
                    // Process already exited, that's fine
                }
            }
        }

        return approved
            ? $"Review completed ({statusPrefix}, commit: {commitHash}). Response file: {responseFileName}"
            : $"Review completed ({statusPrefix}). Response file: {responseFileName}";
    }

    public static void LogWorkflowEvent(string message)
    {
        Logger.Debug(message);
    }

    private static async Task<string?> ValidateTaskTiming(string agentWorkspaceDirectory, string methodName)
    {
        var currentTaskFile = Path.Combine(agentWorkspaceDirectory, "current-task.json");
        if (!File.Exists(currentTaskFile)) return null;

        var taskJson = await File.ReadAllTextAsync(currentTaskFile);
        var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);

        if (taskInfo is null)
        {
            File.Delete(currentTaskFile);
            return null;
        }

        var startedAt = DateTime.Parse(taskInfo.StartedAt);
        var attempt = taskInfo.Attempt;
        var elapsedSeconds = (int)(DateTime.UtcNow - startedAt).TotalSeconds;

        if (elapsedSeconds >= 60 || attempt > 1)
        {
            File.Delete(currentTaskFile);
            return null;
        }

        // Increment attempt counter
        var updatedTaskInfo = taskInfo with { Attempt = attempt + 1 };

        await File.WriteAllTextAsync(currentTaskFile, JsonSerializer.Serialize(updatedTaskInfo, JsonOptions));

        return $"""
                Task assigned {elapsedSeconds} seconds ago - too soon to complete.

                If you see a previous task in your conversation history: That task is already done. You died and were reborn for THIS task. Do not call {methodName} for old tasks.

                If you genuinely completed THIS task already, call {methodName} again to confirm.
                """;
    }
}
