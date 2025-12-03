using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Commands;
using PlatformPlatform.DeveloperCli.Installation;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class ClaudeAgentLifecycle
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<string> CompleteAndExitTask(
        string agentType,
        string taskSummary,
        string responseContent,
        string branch)
    {
        // Validate branch matches current git branch
        var currentGitBranch = GitHelper.GetCurrentBranch();
        if (currentGitBranch != branch)
        {
            return $"ERROR: Branch mismatch detected!\n\n" +
                   $"Task assigned for branch: '{branch}'\n" +
                   $"Current git branch: '{currentGitBranch}'\n\n" +
                   $"Please checkout '{branch}' or restart worker-hosts on the correct branch.";
        }

        var workspace = new Workspace(agentType, branch);

        // Read task number from current-task.json
        if (!File.Exists(workspace.CurrentTaskFile))
        {
            return TerminateSession(workspace);
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
        if (validationError is not null) return validationError;

        // Create response filename
        var sanitizedSummary = string.Join("-", taskSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();
        sanitizedSummary = Regex.Replace(sanitizedSummary, "[^a-z0-9-]", "");
        var responseFileName = $"{taskId}.{agentType}.response.{sanitizedSummary}.md";
        var responseFilePath = Path.Combine(workspace.MessagesDirectory, responseFileName);

        // Add headers to response content
        var now = DateTime.Now;
        var responseContentWithHeaders =
            $"""
             ---
             from: {agentType}
             to: {taskInfo.SenderAgentType}
             request-number: {taskId}
             timestamp: {now:yyyy-MM-ddTHH:mm:sszzz}
             feature-id: {taskInfo.FeatureId ?? "ad-hoc"}
             task-id: {taskInfo.TaskId}
             ---

             {responseContent}
             """;

        // Write response file directly to messages directory
        await File.WriteAllTextAsync(responseFilePath, responseContentWithHeaders);

        // Delete current-task.json now that response is written
        if (File.Exists(workspace.CurrentTaskFile))
        {
            File.Delete(workspace.CurrentTaskFile);
        }

        // Log completion
        LogWorkflowEvent($"[{taskId}.{agentType}.response] Completed via MCP: '{taskSummary}' -> [{responseFileName}]");

        // Return success message immediately so it's saved in conversation
        var successMessage = $"""
                              ✅ Task {taskId} completed successfully!

                              ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
                              Summary:
                              {taskSummary}
                              ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

                              ✓ CompleteWork has been called for task {taskId}
                              ✓ DO NOT call CompleteWork again - this task is finished

                              ⏰ Session will terminate in 5 seconds...
                              📝  Please clear your todo list now
                              """;

        // Schedule termination after delay (fire and forget)
        _ = Task.Run(async () =>
            {
                // Wait for Claude Code to persist session state
                await Task.Delay(TimeSpan.FromSeconds(5));

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
            }
        );

        return successMessage;
    }

    public static async Task<string> CompleteAndExitReview(
        string agentType,
        string? commitHash,
        string? rejectReason,
        string responseContent,
        string branch)
    {
        if (!string.IsNullOrWhiteSpace(commitHash) && !string.IsNullOrWhiteSpace(rejectReason))
        {
            throw new InvalidOperationException("Cannot provide both commitHash and rejectReason");
        }

        if (string.IsNullOrWhiteSpace(commitHash) && string.IsNullOrWhiteSpace(rejectReason))
        {
            throw new InvalidOperationException("Must provide either commitHash or rejectReason");
        }

        // Validate branch matches current git branch
        var currentGitBranch = GitHelper.GetCurrentBranch();
        if (currentGitBranch != branch)
        {
            return $"ERROR: Branch mismatch detected!\n\n" +
                   $"Task assigned for branch: '{branch}'\n" +
                   $"Current git branch: '{currentGitBranch}'\n\n" +
                   $"Please checkout '{branch}' or restart worker-hosts on the correct branch.";
        }

        var approved = !string.IsNullOrEmpty(commitHash);
        var workspace = new Workspace(agentType, branch);

        // Read task number from current-task.json
        if (!File.Exists(workspace.CurrentTaskFile))
        {
            return TerminateSession(workspace);
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
            var commitCheckResult = await ProcessHelper.ExecuteQuietlyAsync($"git cat-file -t {commitHash}", Configuration.SourceCodeFolder);
            if (!commitCheckResult.Success || commitCheckResult.StdOut.Trim() != "commit")
            {
                throw new InvalidOperationException($"Commit {commitHash} does not exist");
            }

            // Extract commit message as review summary
            var commitMessageResult = await ProcessHelper.ExecuteQuietlyAsync($"git log -1 --format=%s {commitHash}", Configuration.SourceCodeFolder);
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
            .ToLowerInvariant();
        sanitizedSummary = Regex.Replace(sanitizedSummary, "[^a-z0-9-]", "");
        var responseFileName = $"{taskId}.{agentType}.response.{statusPrefix}-{sanitizedSummary}.md";
        var responseFilePath = Path.Combine(workspace.MessagesDirectory, responseFileName);

        // Add headers to response content
        var now = DateTime.Now;
        var responseContentWithHeaders =
            $"""
             ---
             from: {agentType}
             to: {taskInfo.SenderAgentType}
             request-number: {taskId}
             timestamp: {now:yyyy-MM-ddTHH:mm:sszzz}
             feature-id: {taskInfo.FeatureId ?? "ad-hoc"}
             task-id: {taskInfo.TaskId}
             approved: {approved}
             {(approved ? $"commit-hash: {commitHash}" : $"reject-reason: {rejectReason}")}
             ---

             {responseContent}
             """;

        // Write response file directly to messages directory
        await File.WriteAllTextAsync(responseFilePath, responseContentWithHeaders);

        // Delete current-task.json now that response is written
        if (File.Exists(workspace.CurrentTaskFile))
        {
            File.Delete(workspace.CurrentTaskFile);
        }

        // Log completion
        var logMessage = approved
            ? $"[{taskId}.{agentType}.response] Review completed via MCP ({statusPrefix}, commit: {commitHash}): '{reviewSummary}' -> [{responseFileName}]"
            : $"[{taskId}.{agentType}.response] Review completed via MCP ({statusPrefix}): '{reviewSummary}' -> [{responseFileName}]";
        LogWorkflowEvent(logMessage);

        // Return success message immediately so it's saved in conversation
        var successMessage = approved
            ? $"""
               ✅ Review of task {taskId} completed: Code APPROVED!

               ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
               Commit: {commitHash}
               🎯 Summary:
               {reviewSummary}
               ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

               ✓ CompleteWork has been called for task {taskId}
               ✓ DO NOT call CompleteWork again - this task is finished

               ⏰ Session will terminate in 5 seconds...
               📝 Please clear your todo list now
               """
            : $"""
               ❌ Review of task {taskId} completed: REJECTED

               ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
               🎯 Rejection Reason:
               {rejectReason}
               ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

               ✓ CompleteWork has been called for task {taskId}
               ✓ DO NOT call CompleteWork again - this task is finished

               ⏰ Session will terminate in 5 seconds...
               📝 Please clear your todo list now
               """;

        // Schedule termination after delay (fire and forget)
        _ = Task.Run(async () =>
            {
                // Wait for Claude Code to persist session state
                await Task.Delay(TimeSpan.FromSeconds(5));

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
            }
        );

        return successMessage;
    }

    public static void LogWorkflowEvent(string message)
    {
        Logger.Info(message);
    }

    private static string TerminateSession(Workspace workspace)
    {
        // Close orphaned request to prevent infinite re-invocation loop
        CreateResponseForOrphanedRequest(workspace);

        _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
                if (File.Exists(workspace.WorkerProcessIdFile))
                {
                    var processIdContent = await File.ReadAllTextAsync(workspace.WorkerProcessIdFile);
                    if (int.TryParse(processIdContent, out var processId))
                    {
                        try
                        {
                            var process = Process.GetProcessById(processId);
                            if (!process.HasExited) process.Kill();
                        }
                        catch (ArgumentException)
                        {
                        }
                    }
                }
            }
        );

        return """
               ✓ Your work is done - session will terminate in 5 seconds

               You will be reactivated with a new task assignment if needed.
               """;
    }

    private static void CreateResponseForOrphanedRequest(Workspace workspace)
    {
        if (!Directory.Exists(workspace.MessagesDirectory)) return;

        var agentType = Path.GetFileName(workspace.AgentWorkspaceDirectory);
        var requestFiles = Directory.GetFiles(workspace.MessagesDirectory, $"*.{agentType}.request.*.md");

        foreach (var requestFile in requestFiles)
        {
            var taskNumber = Path.GetFileNameWithoutExtension(requestFile).Split('.')[0];
            var responseFiles = Directory.GetFiles(workspace.MessagesDirectory, $"{taskNumber}.{agentType}.response.*.md");

            if (responseFiles.Length == 0)
            {
                var responseFileName = Path.GetFileName(requestFile).Replace(".request.", ".response.").Replace(".md", "-recovered.md");
                var responseFilePath = Path.Combine(workspace.MessagesDirectory, responseFileName);
                File.WriteAllText(responseFilePath, $"---\nrequest-number: {taskNumber}\ntimestamp: {DateTime.Now:yyyy-MM-ddTHH:mm:sszzz}\nrecovered: true\n---\n\n# Task recovered from system interruption\n\nAgent terminated gracefully without completing work.");
                LogWorkflowEvent($"[{taskNumber}.{agentType}] Created recovery response for orphaned request");
                return;
            }
        }
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
        var elapsedSeconds = (int)(DateTime.Now - startedAt).TotalSeconds;

        if (elapsedSeconds >= 60 || attempt > 1)
        {
            // Don't delete current-task.json here - it will be deleted after response file is written
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

    public static string ExtractBranchFromPath(string workspacePath)
    {
        // Path format: ".../agent-workspaces/{branch}/messages/..." or
        //              ".../agent-workspaces/{branch}/{agentType}/..." or
        //              ".../agent-workspaces/{agentType}/..." (for branch-agnostic agents)
        var agentWorkspacesIndex = workspacePath.IndexOf("agent-workspaces/", StringComparison.Ordinal);
        if (agentWorkspacesIndex == -1)
        {
            throw new InvalidOperationException($"Invalid workspace path (missing 'agent-workspaces/'): {workspacePath}");
        }

        var afterWorkspaces = workspacePath[(agentWorkspacesIndex + "agent-workspaces/".Length)..];
        var parts = afterWorkspaces.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            throw new InvalidOperationException($"Invalid workspace path (no branch found): {workspacePath}");
        }

        // Check if first part is a branch-agnostic agent (no branch in path)
        if (parts[0] is "pair-programmer" or "tech-lead")
        {
            return GitHelper.GetCurrentBranch(); // Use current git branch as logical branch
        }

        return parts[0]; // First part after agent-workspaces/ is the branch
    }
}
