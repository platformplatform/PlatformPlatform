using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using ModelContextProtocol.Server;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ClaudeAgentCommand : Command
{
    internal static readonly Dictionary<int, WorkerSession> ActiveWorkerSessions = new();
    private static readonly Lock WorkerSessionLock = new();
    private static bool _showAllActivities;

    public ClaudeAgentCommand() : base("claude-agent", "Interactive Worker Host for agent development")
    {
        var agentTypeArgument = new Argument<string?>("agent-type", () => null)
        {
            Description = "Agent type to run (tech-lead, backend-engineer, backend-reviewer, frontend-engineer, frontend-reviewer, test-automation-engineer, test-automation-reviewer)",
            Arity = ArgumentArity.ZeroOrOne
        };

        AddArgument(agentTypeArgument);

        this.SetHandler(ExecuteAsync, agentTypeArgument);
    }

    private async Task ExecuteAsync(string? agentType)
    {
        try
        {
            // Interactive mode
            await RunInteractiveMode(agentType);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    private async Task RunInteractiveMode(string? agentType)
    {
        // If no agent type provided, prompt for selection
        if (string.IsNullOrEmpty(agentType))
        {
            agentType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an [green]agent type[/] to run:")
                    .AddChoices(
                        "tech-lead",
                        "backend-engineer",
                        "backend-reviewer",
                        "frontend-engineer",
                        "frontend-reviewer",
                        "test-automation-engineer",
                        "test-automation-reviewer"
                    )
            );
        }

        var branch = GitHelper.GetCurrentBranch();

        // Create workspace and register agent
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);
        Directory.CreateDirectory(agentWorkspaceDirectory);

        // Setup workspace with symlink to .claude directory
        await SetupAgentWorkspace(agentWorkspaceDirectory);

        // Check for stale process ID file from previous automated worker
        var processIdFile = Path.Combine(agentWorkspaceDirectory, ".process-id");
        if (File.Exists(processIdFile))
        {
            var existingPid = await File.ReadAllTextAsync(processIdFile);
            if (int.TryParse(existingPid, out var pid))
            {
                try
                {
                    var existingProcess = Process.GetProcessById(pid);
                    if (!existingProcess.HasExited)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: An automated {agentType} worker appears to be running (process ID: {pid})[/]");
                        AnsiConsole.MarkupLine("[yellow]This might be a stale process ID file from a crashed session.[/]");

                        var deleteChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Delete the stale process ID file and continue?")
                                .AddChoices("Yes, delete and continue", "No, exit")
                                .HighlightStyle(new Style(Color.Yellow))
                        );

                        if (deleteChoice == "No, exit")
                        {
                            return;
                        }
                    }
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist, process ID is stale
                }
            }

            File.Delete(processIdFile);
        }

        // Interactive mode does NOT create .process-id file
        // Only automated workers write .process-id (so MCP tools can kill them)

        // Ensure Ctrl+C exits cleanly
        Console.CancelKeyPress += (_, e) =>
        {
            // Allow normal Ctrl+C behavior (exit process)
            e.Cancel = false;
        };

        // Display FigletText banner for the agent
        var displayName = GetAgentDisplayName(agentType);

        // Set terminal title
        SetTerminalTitle($"{displayName} - {branch}");

        // Load small Figlet font for compact banner
        var smallFontPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli", "Fonts", "small.flf");
        var font = File.Exists(smallFontPath) ? FigletFont.Load(smallFontPath) : FigletFont.Default;
        var agentBanner = new FigletText(font, displayName).Color(GetAgentColor(agentType));
        AnsiConsole.Write(agentBanner);

        var agentColor = GetAgentColor(agentType);

        AnsiConsole.WriteLine(); // Extra line for spacing

        // Special handling for tech lead - launch directly into Claude Code
        if (agentType == "tech-lead")
        {
            AnsiConsole.MarkupLine($"[{agentColor}]Launching tech lead mode...[/]");
            await Task.Delay(TimeSpan.FromSeconds(2));

            // Ensure tech lead workspace is set up (for session ID file)
            await SetupAgentWorkspace(agentWorkspaceDirectory);

            // Launch tech lead directly
            await LaunchTechLeadAsync(agentType, branch);
            return;
        }

        // Check for task recovery - if .task-id exists, resume the task
        var taskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");
        Directory.CreateDirectory(messagesDirectory);

        if (File.Exists(taskIdFile))
        {
            var taskId = await File.ReadAllTextAsync(taskIdFile);
            taskId = taskId.Trim();

            // Find the request file for this task
            var requestPattern = $"{taskId}.{agentType}.request.*.md";
            var requestFiles = Directory.GetFiles(messagesDirectory, requestPattern);

            if (requestFiles.Length > 0)
            {
                var requestFile = requestFiles[0];
                AnsiConsole.MarkupLine($"[{agentColor} bold]⚡ TASK RECOVERY[/]");
                AnsiConsole.MarkupLine($"[dim]Resuming task: {Path.GetFileName(requestFile)}[/]");
                AnsiConsole.MarkupLine("[dim]Launching Claude Code in 3 seconds...[/]");
                await Task.Delay(TimeSpan.FromSeconds(3));

                // Handle the recovered task immediately
                await HandleIncomingRequest(requestFile, agentType, branch);
            }
        }

        // Display initial waiting screen with recent activity
        RedrawWaitingDisplay(agentType, branch);

        await WatchForRequestsAsync(agentType, messagesDirectory, branch);
    }

    private async Task LaunchTechLeadAsync(string agentType, string branch)
    {
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);

        // Load Tech Lead system prompt from .txt file
        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        var systemPromptText = "";

        if (File.Exists(systemPromptFile))
        {
            systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
            // Transform to single line and escape quotes for command-line usage
            systemPromptText = systemPromptText
                .Replace('\n', ' ')
                .Replace('\r', ' ')
                .Replace("\"", "'")
                .Trim();
        }

        // Prepare Tech Lead arguments
        var techLeadArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "acceptEdits"
        };

        if (!string.IsNullOrEmpty(systemPromptText))
        {
            techLeadArgs.Add("--append-system-prompt");
            techLeadArgs.Add(systemPromptText);
        }

        techLeadArgs.Add("/orchestrate/tech-lead");

        // Launch using common method (handles session management)
        var process = await LaunchClaudeCode(agentWorkspaceDirectory, techLeadArgs, Configuration.SourceCodeFolder);

        // Start tech lead health monitoring
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");
        _ = Task.Run(async () => await MonitorTechLeadHealth(process, agentType, branch, messagesDirectory));

        await process.WaitForExitAsync();

        // Tech Lead exited - clean up and show completion
        var agentColor = GetAgentColor(agentType);
        AnsiConsole.MarkupLine($"[{agentColor} bold]✓ Tech Lead session ended[/]");
    }

    private async Task MonitorTechLeadHealth(Process techLeadProcess, string agentType, string branch, string messagesDirectory)
    {
        var timeout = TimeSpan.FromMinutes(62);
        var lastActivity = DateTime.Now;

        // Monitor messages directory for activity
        using var watcher = new FileSystemWatcher(messagesDirectory, "*.md");
        watcher.EnableRaisingEvents = true;

        watcher.Created += (_, _) => lastActivity = DateTime.Now;

        while (!techLeadProcess.HasExited)
        {
            // Check for git changes to detect activity
            if (HasGitChanges())
            {
                lastActivity = DateTime.Now;
            }

            var timeSinceLastActivity = DateTime.Now - lastActivity;

            if (timeSinceLastActivity > timeout)
            {
                // Log tech lead timeout to workflow log
                var workflowLogPath = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "workflow.log");
                var timeoutLogMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] TECH LEAD TIMEOUT: {agentType} inactive for 1 hour, killing process (main thread will restart)\n";
                await File.AppendAllTextAsync(workflowLogPath, timeoutLogMessage);

                // Kill stalled tech lead if still running - main thread restart loop will handle restart
                if (!techLeadProcess.HasExited)
                {
                    techLeadProcess.Kill();
                }

                // Exit monitor - main thread has restart loop with proper terminal context
                break;
            }

            // Calculate how long to wait before next check
            var timeUntilTimeout = timeout - timeSinceLastActivity;
            // Wait for the remaining time (max 5 minutes to catch activity sooner)
            var waitTime = timeUntilTimeout > TimeSpan.FromMinutes(5)
                ? TimeSpan.FromMinutes(5)
                : timeUntilTimeout;

            await Task.Delay(waitTime);
        }
    }

    internal static async Task SetupAgentWorkspace(string agentWorkspaceDirectory)
    {
        Directory.CreateDirectory(agentWorkspaceDirectory);

        // Create symlink to .claude directory for always-current commands, agents, and settings (only if doesn't exist)
        var workerClaudeDir = Path.Combine(agentWorkspaceDirectory, ".claude");
        var rootClaudeDir = Path.Combine(Configuration.SourceCodeFolder, ".claude");

        // Only create symlink if it doesn't exist
        if (!Directory.Exists(workerClaudeDir) && !File.Exists(workerClaudeDir) && Directory.Exists(rootClaudeDir))
        {
            try
            {
                if (Configuration.IsWindows)
                {
                    ProcessHelper.StartProcess($"cmd /c mklink /D \"{workerClaudeDir}\" \"{rootClaudeDir}\"");
                }
                else
                {
                    // Create relative symlink for better portability
                    var relativePath = Path.GetRelativePath(Path.GetDirectoryName(workerClaudeDir)!, rootClaudeDir);
                    Directory.CreateSymbolicLink(workerClaudeDir, relativePath);
                }
            }
            catch
            {
                // Fallback to copying essential files if symlink creation fails
                Directory.CreateDirectory(Path.Combine(workerClaudeDir, "commands"));

                // Copy only essential commands
                var commandsSource = Path.Combine(rootClaudeDir, "commands");
                if (Directory.Exists(commandsSource))
                {
                    foreach (var file in Directory.GetFiles(commandsSource, "*.md"))
                    {
                        var fileName = Path.GetFileName(file);
                        var destFile = Path.Combine(workerClaudeDir, "commands", fileName);
                        await File.WriteAllTextAsync(destFile, await File.ReadAllTextAsync(file));
                    }
                }
            }
        }

        // Setup .mcp.json with relative path to developer-cli
        var rootMcpJsonPath = Path.Combine(Configuration.SourceCodeFolder, ".mcp.json");
        var workerMcpJsonPath = Path.Combine(agentWorkspaceDirectory, ".mcp.json");

        if (File.Exists(rootMcpJsonPath))
        {
            // Create MCP config JSON with proper formatting
            var mcpConfigJson = """
                                {
                                  "mcpServers": {
                                    "platformplatform-developer-cli": {
                                      "command": "dotnet",
                                      "args": ["run", "--project", "../../../../developer-cli", "mcp"]
                                    }
                                  }
                                }
                                """;

            await File.WriteAllTextAsync(workerMcpJsonPath, mcpConfigJson);
        }
    }

    internal static bool HasGitChanges()
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = "status --porcelain",
                WorkingDirectory = Configuration.SourceCodeFolder,
                UseShellExecute = false,
                RedirectStandardOutput = true
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();

        return !string.IsNullOrWhiteSpace(output);
    }

    private static List<string> GetRecentActivity(string agentType, string branch)
    {
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");
        var activities = new List<string>();

        if (!Directory.Exists(messagesDirectory))
        {
            return activities;
        }

        try
        {
            // Find completed response files for this agent type
            var responseFiles = Directory.GetFiles(messagesDirectory, $"*.{agentType}.response.*.md")
                .Select(file => new
                    {
                        FilePath = file,
                        FileName = Path.GetFileName(file),
                        ResponseTime = File.GetLastWriteTime(file)
                    }
                )
                .OrderBy(f => f.ResponseTime) // Chronological order: oldest first, newest last
                .ToList();

            foreach (var file in responseFiles)
            {
                // Parse filename: NNNN.agent-type.response.Title-Case-Task-Name.md
                var parts = file.FileName.Split('.');
                if (parts.Length >= 4)
                {
                    var taskNumber = parts[0];
                    var taskDescription = parts[3].Replace("-md", "").Replace('-', ' ');

                    // Determine status icon based on response filename
                    var statusIcon = "✔️"; // Default for completed tasks
                    if (taskDescription.StartsWith("Approved ", StringComparison.OrdinalIgnoreCase))
                    {
                        statusIcon = "✅"; // Green checkmark for approved
                        taskDescription = taskDescription.Substring("Approved ".Length); // Remove prefix from display
                    }
                    else if (taskDescription.StartsWith("Rejected ", StringComparison.OrdinalIgnoreCase))
                    {
                        statusIcon = "❌"; // Red X for rejected
                        taskDescription = taskDescription.Substring("Rejected ".Length); // Remove prefix from display
                    }

                    // Find corresponding request file to calculate duration
                    var requestFileName = $"{taskNumber}.{agentType}.request.*.md";
                    var requestFiles = Directory.GetFiles(messagesDirectory, requestFileName);

                    if (requestFiles.Length > 0 && File.Exists(requestFiles[0]))
                    {
                        var requestTime = File.GetLastWriteTime(requestFiles[0]);
                        var responseTime = file.ResponseTime;
                        var duration = responseTime - requestTime;

                        // Ensure duration is positive (handle clock skew, etc.)
                        if (duration.TotalSeconds > 0)
                        {
                            var requestTimeStr = requestTime.ToString("HH:mm");
                            var responseTimeStr = responseTime.ToString("HH:mm");
                            var durationStr = $"{(int)duration.TotalMinutes}m {duration.Seconds}s";

                            var activityLine = $"{statusIcon} {requestTimeStr}-{responseTimeStr} - {taskNumber} - {taskDescription} ({durationStr})";
                            activities.Add(activityLine);
                        }
                        else
                        {
                            // Duration calculation failed, use simple format
                            var timeStamp = file.ResponseTime.ToString("HH:mm");
                            var activityLine = $"{statusIcon} {timeStamp} - {taskNumber} - {taskDescription}";
                            activities.Add(activityLine);
                        }
                    }
                    else
                    {
                        // No request file found, use simple format
                        var timeStamp = file.ResponseTime.ToString("HH:mm");
                        var activityLine = $"{statusIcon} {timeStamp} - {taskNumber} - {taskDescription}";
                        activities.Add(activityLine);
                    }
                }
            }

            if (activities.Count == 0)
            {
                activities.Add("   No completed tasks yet");
            }
        }
        catch
        {
            activities.Add("   Unable to load activity history");
        }

        return activities;
    }

    // ReSharper disable once FunctionNeverReturns
    private async Task WatchForRequestsAsync(string agentType, string messagesDirectory, string branch)
    {
        using var fileSystemWatcher = new FileSystemWatcher(messagesDirectory, $"*.{agentType}.request.*.md");
        fileSystemWatcher.EnableRaisingEvents = true;

        var requestReceived = false;
        string? requestFilePath = null;

        fileSystemWatcher.Created += (_, e) =>
        {
            requestReceived = true;
            requestFilePath = e.FullPath;
        };

        // Main loop: standby display with ENTER listener
        while (true)
        {
            // Show standby display and wait for ENTER key or request file
            var userPressedEnter = await WaitInStandbyMode(agentType, branch, () => requestReceived);

            if (requestReceived && requestFilePath != null)
            {
                // Request file arrived - handle it
                requestReceived = false;
                await HandleIncomingRequest(requestFilePath, agentType, branch);
                requestFilePath = null;
            }
            else if (userPressedEnter)
            {
                // User pressed ENTER - launch manual session
                await LaunchManualClaudeSession(agentType, branch);
            }
        }
    }

    private async Task<bool> WaitInStandbyMode(string agentType, string branch, Func<bool> checkForRequest)
    {
        // Display standby screen
        RedrawWaitingDisplay(agentType, branch);

        // Wait for ENTER key or incoming request
        while (true)
        {
            // Check if request file arrived (non-blocking)
            if (checkForRequest())
            {
                return false; // Request received, not user ENTER
            }

            // Check for keyboard input (non-blocking)
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    AnsiConsole.Clear();
                    AnsiConsole.MarkupLine("[yellow]Manual control activated[/]");
                    return true; // User pressed ENTER
                }

                if (key.Key == ConsoleKey.A && (key.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    // Ctrl+A - toggle showing all activities
                    _showAllActivities = !_showAllActivities;
                    RedrawWaitingDisplay(agentType, branch);
                }
            }

            // Small delay to prevent CPU spinning
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    private async Task LaunchManualClaudeSession(string agentType, string branch)
    {
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);

        var manualArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions"
        };

        // Use common launch method (handles session management)
        var process = await LaunchClaudeCode(agentWorkspaceDirectory, manualArgs);
        await process.WaitForExitAsync();
    }

    private async Task HandleIncomingRequest(string requestFile, string agentType, string branch)
    {
        var agentColor = GetAgentColor(agentType);

        // Clear the waiting display
        AnsiConsole.Clear();

        // Show task received animation
        AnsiConsole.MarkupLine($"[{agentColor} bold]▶ TASK RECEIVED[/]");
        AnsiConsole.MarkupLine($"[dim]Request: {Path.GetFileName(requestFile)}[/]");

        // Read task content
        await Task.Delay(TimeSpan.FromMilliseconds(500)); // Let file write complete
        var taskContent = await File.ReadAllTextAsync(requestFile);
        var firstLine = taskContent.Split('\n').FirstOrDefault()?.Trim() ?? "Task";

        AnsiConsole.MarkupLine($"[dim]Task: {Markup.Escape(firstLine)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Launching Claude Code in 3 seconds...[/]");
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Launch Claude Code with the request
        var claudeProcess = await LaunchClaudeCodeAsync(agentType, branch);

        // Wait for response file and then kill Claude
        await WaitForResponseAndKillClaude(requestFile, agentType, branch, claudeProcess);

        // Return to waiting display
        RedrawWaitingDisplay(agentType, branch);
    }

    private async Task<Process> LaunchClaudeCodeAsync(string agentType, string branch)
    {
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);
        await SetupAgentWorkspace(agentWorkspaceDirectory);

        // Load agent system prompt
        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        if (!File.Exists(systemPromptFile))
        {
            throw new FileNotFoundException($"System prompt file not found: {systemPromptFile}");
        }

        var systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
        systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();

        // Load workflow and embed it
        var workflowFile = agentType.Contains("reviewer")
            ? Path.Combine(Configuration.SourceCodeFolder, ".claude", "commands", "review", "task.md")
            : Path.Combine(Configuration.SourceCodeFolder, ".claude", "commands", "implement", "task.md");

        var workflowText = "";
        if (File.Exists(workflowFile))
        {
            workflowText = await File.ReadAllTextAsync(workflowFile);
            var frontmatterEnd = workflowText.IndexOf("---", 3, StringComparison.Ordinal);
            if (frontmatterEnd > 0)
            {
                workflowText = workflowText.Substring(frontmatterEnd + 3).Trim();
            }
            workflowText = workflowText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
        }

        // Build arguments
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions",
            "--append-system-prompt", systemPromptText
        };

        if (!string.IsNullOrEmpty(workflowText))
        {
            claudeArgs.Add("--append-system-prompt");
            claudeArgs.Add(workflowText);
        }

        // Use common launch method (handles session management)
        return await LaunchClaudeCode(agentWorkspaceDirectory, claudeArgs);
    }

    private async Task WaitForResponseAndKillClaude(string requestFile, string agentType, string branch, Process claudeProcess)
    {
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");

        // Extract request file name components for response file
        var requestFileName = Path.GetFileName(requestFile);
        var match = Regex.Match(requestFileName, @"^(\d+)\.([^.]+)\.request\.(.+)\.md$");
        var counter = match.Groups[1].Value;
        var responseFilePattern = $"{counter}.{agentType}.response.*.md";

        // Wait indefinitely for response file (user is manually controlling this agent)
        AnsiConsole.MarkupLine($"[grey]Waiting for response file: {counter}.{agentType}.response.*.md[/]");

        while (true)
        {
            // Check for any file matching the pattern
            var matchingFiles = Directory.GetFiles(messagesDirectory, responseFilePattern);
            if (matchingFiles.Length > 0)
            {
                var agentColor = GetAgentColor(agentType);
                AnsiConsole.MarkupLine($"[{agentColor} bold]✓ Response file created[/]");
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500)); // Check every 500ms
        }

        // Graceful shutdown: Send Ctrl+C twice, wait, then force kill if needed
        if (!claudeProcess.HasExited)
        {
            // Send SIGINT twice (Ctrl+C, C)
            for (var i = 0; i < 2; i++)
            {
                Process.Start(new ProcessStartInfo
                    {
                        FileName = "kill",
                        Arguments = $"-SIGINT {claudeProcess.Id}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    }
                );
                await Task.Delay(TimeSpan.FromMilliseconds(100));
            }

            // Wait 3 seconds for graceful exit
            await Task.Delay(TimeSpan.FromSeconds(3));

            // Force kill if still alive
            if (!claudeProcess.HasExited)
            {
                claudeProcess.Kill();
            }
        }
    }

    private void RedrawWaitingDisplay(string agentType, string branch)
    {
        AnsiConsole.Clear();

        var displayName = GetAgentDisplayName(agentType);

        // Load small Figlet font for compact banner
        var smallFontPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli", "Fonts", "small.flf");
        var font = File.Exists(smallFontPath) ? FigletFont.Load(smallFontPath) : FigletFont.Default;
        var agentBanner = new FigletText(font, displayName).Color(GetAgentColor(agentType));
        AnsiConsole.Write(agentBanner);

        var agentColor = GetAgentColor(agentType);

        AnsiConsole.WriteLine();

        var rule = new Rule("[bold]WAITING FOR TASKS[/]")
            .RuleStyle($"{agentColor}")
            .LeftJustified();
        AnsiConsole.Write(rule);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Branch: [{agentColor} bold]{branch}[/]");
        AnsiConsole.MarkupLine("Status: [dim]Press [bold white]ENTER[/] for manual control | Press [bold white]CTRL+A[/] to toggle all activities[/]");
        AnsiConsole.WriteLine();

        // Show activities section
        var activitiesRule = new Rule("[bold]ACTIVITIES[/]")
            .RuleStyle($"{agentColor}")
            .LeftJustified();
        AnsiConsole.Write(activitiesRule);

        AnsiConsole.WriteLine();
        var recentActivities = GetRecentActivity(agentType, branch);

        // Show only last 5 by default, or all if toggled
        var activitiesToShow = _showAllActivities
            ? recentActivities
            : recentActivities.TakeLast(5).ToList();

        if (!_showAllActivities && recentActivities.Count > 5)
        {
            AnsiConsole.MarkupLine($"[dim]   ... {recentActivities.Count - 5} older activities hidden (Ctrl+A to show all)[/]");
        }

        foreach (var activity in activitiesToShow)
        {
            // Show all activities in default white color
            AnsiConsole.MarkupLine($"{Markup.Escape(activity)}");
        }

        AnsiConsole.WriteLine();
    }

    private static string GetAgentDisplayName(string agentType)
    {
        return agentType switch
        {
            "tech-lead" => "Tech Lead",
            "backend-engineer" => "Backend Engineer",
            "frontend-engineer" => "Frontend Engineer",
            "backend-reviewer" => "Backend Reviewer",
            "frontend-reviewer" => "Frontend Reviewer",
            "test-automation-engineer" => "Test Automation Engineer",
            "test-automation-reviewer" => "Test Automation Reviewer",
            _ => throw new ArgumentException($"Unknown agent type: {agentType}")
        };
    }

    private static Color GetAgentColor(string agentType)
    {
        return agentType switch
        {
            "tech-lead" => Color.Red,
            "backend-engineer" => Color.Green,
            "frontend-engineer" => Color.Blue,
            "backend-reviewer" => Color.Yellow,
            "frontend-reviewer" => Color.Orange3,
            "test-automation-engineer" => Color.Cyan1,
            "test-automation-reviewer" => Color.Purple,
            _ => throw new ArgumentException($"Unknown agent type: {agentType}")
        };
    }

    private static void SetTerminalTitle(string title)
    {
        // ANSI escape sequence to set terminal title
        // Works in most modern terminals (iTerm2, Terminal.app, Windows Terminal, etc.)
        Console.Write($"\x1b]0;{title}\x07");
    }

    internal static async Task<Process> LaunchClaudeCode(
        string agentWorkspaceDirectory,
        List<string> additionalArgs,
        string? workingDirectory = null)
    {
        // Default to agent workspace if no working directory specified
        workingDirectory ??= agentWorkspaceDirectory;

        // Session management - single source of truth
        var sessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");
        var args = new List<string>();

        if (File.Exists(sessionIdFile))
        {
            // Session exists - continue it
            args.Add("--continue");
        }
        else
        {
            // Fresh session - create marker for next time
            await File.WriteAllTextAsync(sessionIdFile, Guid.NewGuid().ToString());
        }

        // Add all other arguments
        args.AddRange(additionalArgs);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };

        process.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
        process.Start();
        return process;
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
    private static readonly string[] ValidAgentTypes =
    [
        "tech-lead", "backend-engineer", "backend-reviewer",
        "frontend-engineer", "frontend-reviewer", "test-automation-engineer", "test-automation-reviewer"
    ];

    [McpServerTool]
    [Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, test automation, or code review. The agent will work autonomously and return results.")]
    public static async Task<string> StartWorker(
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
        var branchName = GitHelper.GetCurrentBranch();
        var workflowLog = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, "workflow.log");

        await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] StartWorker called: agentType={agentType}, taskTitle={taskTitle}\n");

        AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] StartWorker called: agentType={agentType}, taskTitle={taskTitle}[/]");

        Mutex? workspaceMutex = null;
        try
        {
            if (!ValidAgentTypes.Contains(agentType))
            {
                throw new ArgumentException($"Invalid agent type '{agentType}'. Valid types: {string.Join(", ", ValidAgentTypes)}");
            }

            var currentBranchName = GitHelper.GetCurrentBranch();

            // Setup workspace paths
            var branchWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", currentBranchName);
            var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDirectory, agentType);
            var messagesDirectory = Path.Combine(branchWorkspaceDirectory, "messages");
            var processIdFile = Path.Combine(agentWorkspaceDirectory, ".process-id");

            // Debug: Log the PID file path we're checking
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Checking for PID file: {processIdFile}\n");
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] PID file exists: {File.Exists(processIdFile)}\n");

            if (File.Exists(processIdFile))
            {
                var pidContent = await File.ReadAllTextAsync(processIdFile);
                if (int.TryParse(pidContent, out var pid))
                {
                    try
                    {
                        var existingProcess = Process.GetProcessById(pid);
                        if (!existingProcess.HasExited)
                        {
                            // Interactive agent is running, just create the request file
                            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Interactive {agentType} detected (PID: {pid}), delegating task[/]");

                            Directory.CreateDirectory(messagesDirectory);

                            // Read current counter, increment, and write
                            var taskCounterFile = Path.Combine(messagesDirectory, ".task-counter");
                            var taskCounter = 1;
                            if (File.Exists(taskCounterFile) && int.TryParse(await File.ReadAllTextAsync(taskCounterFile), out var existingCounter))
                            {
                                taskCounter = existingCounter + 1;
                            }

                            await File.WriteAllTextAsync(taskCounterFile, taskCounter.ToString());

                            var taskShortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))
                                .ToLowerInvariant().Replace(".", "").Replace(",", "");
                            var taskRequestFileName = $"{taskCounter:D4}.{agentType}.request.{taskShortTitle}.md";
                            var taskRequestFilePath = Path.Combine(messagesDirectory, taskRequestFileName);

                            await File.WriteAllTextAsync(taskRequestFilePath, markdownContent);

                            // Save current task info for recovery scenarios
                            var interactiveTaskInfo = new
                            {
                                task_number = $"{taskCounter:D4}",
                                request_file = taskRequestFileName,
                                started_at = DateTime.UtcNow.ToString("O"),
                                attempt = 1,
                                branch = currentBranchName,
                                title = taskTitle
                            };

                            var interactiveTaskFile = Path.Combine(agentWorkspaceDirectory, ".current-task.json");
                            await File.WriteAllTextAsync(interactiveTaskFile, JsonSerializer.Serialize(interactiveTaskInfo, new JsonSerializerOptions { WriteIndented = true }));

                            // Create .task-id file for recovery after crash/restart
                            var interactiveTaskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
                            await File.WriteAllTextAsync(interactiveTaskIdFile, $"{taskCounter:D4}");

                            // Log task start
                            LogWorkflowEvent($"[{taskCounter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{taskRequestFileName}]", messagesDirectory);

                            // Wait for the response file to be created
                            // Interactive worker-host handles file creation and moves it to messages directory
                            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Waiting for interactive agent to complete task {taskCounter:D4}[/]");

                            // Poll for response file in messages directory (MCP tool writes it there)
                            var startTime = DateTime.Now;
                            var overallTimeout = TimeSpan.FromHours(2);

                            string? foundResponseFile = null;
                            var responseFilePattern = $"{taskCounter:D4}.{agentType}.response.*.md";

                            // ReSharper disable once LoopVariableIsNeverChangedInsideLoop - foundResponseFile set when file detected
                            while (foundResponseFile == null)
                            {
                                // Only enforce overall timeout (2 hours max)
                                if (DateTime.Now - startTime > overallTimeout)
                                {
                                    throw new TimeoutException($"Interactive {agentType} exceeded 2-hour overall timeout");
                                }

                                // Check for response file in messages directory
                                var matchingFiles = Directory.GetFiles(messagesDirectory, responseFilePattern);
                                if (matchingFiles.Length > 0)
                                {
                                    foundResponseFile = matchingFiles[0];
                                    break;
                                }

                                await Task.Delay(TimeSpan.FromMilliseconds(500)); // Check every 500ms
                            }

                            AnsiConsole.MarkupLine("[grey][[MCP DEBUG]] Response file detected, reading content...[/]");

                            // Delete .task-id file after successful completion
                            var completedTaskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
                            if (File.Exists(completedTaskIdFile))
                            {
                                File.Delete(completedTaskIdFile);
                            }

                            // Read response content immediately - file is complete via atomic rename
                            var responseContent = await File.ReadAllTextAsync(foundResponseFile);

                            // Log task completion
                            var actualResponseFileName = Path.GetFileName(foundResponseFile);
                            LogWorkflowEvent($"[{taskCounter:D4}.{agentType}.response] Completed: '{taskTitle}' -> [{actualResponseFileName}]", messagesDirectory);

                            AnsiConsole.MarkupLine("[grey][[MCP DEBUG]] Interactive agent completed task[/]");

                            // Return clear task information for the Tech Lead
                            return $"Task delegated successfully to {agentType}.\n" +
                                   $"Task number: {taskCounter:D4}\n" +
                                   $"Request file: {taskRequestFileName}\n" +
                                   $"Response file: {actualResponseFileName}\n\n" +
                                   $"Response content:\n{responseContent}";
                        }
                    }
                    catch (ArgumentException)
                    {
                        File.Delete(processIdFile);
                    }
                }
            }

            // No interactive agent, continue with normal Worker spawn
            // Acquire workspace lock to ensure only one Worker of this agent type runs per branch
            var mutexName = $"{agentType}-{branchName}";
            workspaceMutex = new Mutex(false, mutexName);

            if (!workspaceMutex.WaitOne(TimeSpan.FromSeconds(5)))
            {
                workspaceMutex.Dispose();
                throw new InvalidOperationException($"Another {agentType} is already active in branch '{branchName}'. Only one {agentType} can work per branch to maintain consistent context and memory.");
            }

            // Ensure messages directory exists
            Directory.CreateDirectory(messagesDirectory);

            // Read current counter, increment, and write
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
            var requestFile = Path.Combine(messagesDirectory, requestFileName);

            await File.WriteAllTextAsync(requestFile, markdownContent);

            Directory.CreateDirectory(agentWorkspaceDirectory);

            // Setup workspace with symlink to .claude directory
            await ClaudeAgentCommand.SetupAgentWorkspace(agentWorkspaceDirectory);

            // Save current task info for recovery scenarios
            var currentTaskInfo = new
            {
                task_number = $"{counter:D4}",
                request_file = requestFileName,
                started_at = DateTime.UtcNow.ToString("O"),
                attempt = 1,
                branch = branchName,
                title = taskTitle
            };

            var currentTaskFile = Path.Combine(agentWorkspaceDirectory, ".current-task.json");
            await File.WriteAllTextAsync(currentTaskFile, JsonSerializer.Serialize(currentTaskInfo, new JsonSerializerOptions { WriteIndented = true }));

            // Create .task-id file for recovery after crash/restart
            var taskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
            await File.WriteAllTextAsync(taskIdFile, $"{counter:D4}");

            // Load agent system prompt
            var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
            var systemPromptText = "";
            if (File.Exists(systemPromptFile))
            {
                systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
                systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
            }

            // Load workflow and embed it (100% reliable, no slash command dependency)
            var workflowFile = agentType.Contains("reviewer")
                ? Path.Combine(Configuration.SourceCodeFolder, ".claude", "commands", "review", "task.md")
                : Path.Combine(Configuration.SourceCodeFolder, ".claude", "commands", "implement", "task.md");

            var workflowText = "";
            if (File.Exists(workflowFile))
            {
                workflowText = await File.ReadAllTextAsync(workflowFile);
                // Remove YAML frontmatter
                var frontmatterEnd = workflowText.IndexOf("---", 3, StringComparison.Ordinal);
                if (frontmatterEnd > 0)
                {
                    workflowText = workflowText.Substring(frontmatterEnd + 3).Trim();
                }
                workflowText = workflowText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
            }

            // Build Claude Code arguments
            var claudeArgs = new List<string>
            {
                "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                "--add-dir", Configuration.SourceCodeFolder,
                "--permission-mode", "bypassPermissions"
            };

            if (!string.IsNullOrEmpty(systemPromptText))
            {
                claudeArgs.Add("--append-system-prompt");
                claudeArgs.Add(systemPromptText);
            }

            // Append workflow as system prompt (not slash command)
            if (!string.IsNullOrEmpty(workflowText))
            {
                claudeArgs.Add("--append-system-prompt");
                claudeArgs.Add(workflowText);
            }

            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting Claude Code worker in: {agentWorkspaceDirectory}\n");
            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Starting Claude Code worker process[/]");

            // Use common launch method (handles session management)
            var process = await ClaudeAgentCommand.LaunchClaudeCode(agentWorkspaceDirectory, claudeArgs, Configuration.SourceCodeFolder);

            // Create PID file for automated worker
            await File.WriteAllTextAsync(processIdFile, process.Id.ToString());

            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker process started with PID: {process.Id}\n");

            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Worker process started with PID: {process.Id}[/]");

            // Log what Claude Code actually receives as input
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Claude input should be: {claudeArgs.Last()}\n");

            // Check if process exits immediately
            await Task.Delay(TimeSpan.FromSeconds(3));
            if (process.HasExited)
            {
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: Worker process {process.Id} exited immediately with code: {process.ExitCode}\n");
            }
            else
            {
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Worker process {process.Id} is running\n");
            }

            // Track active Worker session
            ClaudeAgentCommand.AddWorkerSession(process.Id, agentType, taskTitle, requestFileName, process);

            LogWorkflowEvent($"[{counter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{requestFileName}]", messagesDirectory);

            try
            {
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting worker monitoring for PID: {process.Id}\n");
                // Monitor for response file creation with FileSystemWatcher
                var result = await WaitForWorkerCompletionAsync(messagesDirectory, counter, agentType, process.Id, taskTitle, requestFileName);
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker monitoring completed with result: {result.Substring(0, Math.Min(100, result.Length))}...\n");
                return result;
            }
            finally
            {
                // Clean up PID file
                var processIdFileCleanup = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, agentType, ".process-id");
                if (File.Exists(processIdFileCleanup))
                {
                    File.Delete(processIdFileCleanup);
                }

                // Remove from active sessions and release workspace lock
                ClaudeAgentCommand.RemoveWorkerSession(process.Id);
                workspaceMutex.ReleaseMutex();
                workspaceMutex.Dispose();
            }
        }
        catch (Exception ex)
        {
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR in StartWorker: {ex.Message}\n");

            // Clean up PID file on error
            var processIdFileCleanup = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, agentType, ".process-id");
            if (File.Exists(processIdFileCleanup))
            {
                File.Delete(processIdFileCleanup);
            }

            if (workspaceMutex != null)
            {
                workspaceMutex.ReleaseMutex();
                workspaceMutex.Dispose();
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
        Console.Error.WriteLine("[MCP DEBUG] ListActiveWorkers called");
        return ClaudeAgentCommand.GetActiveWorkersList();
    }

    [McpServerTool]
    [Description("Stop a development agent that is taking too long or needs to be cancelled. Use when work needs to be interrupted.")]
    public static string KillWorker([Description("Process ID of Worker to terminate")] int processId)
    {
        return ClaudeAgentCommand.TerminateWorker(processId);
    }

    [McpServerTool]
    [Description("Signal task completion from worker agent. Call this when you have finished implementing a task. This will write your response file and terminate your session.")]
    public static async Task<string> CompleteTask(
        [Description("Agent type (backend-engineer, frontend-engineer, test-automation-engineer)")]
        string agentType,
        [Description("Brief task summary in sentence case (e.g., 'Api endpoints implemented')")]
        string taskSummary,
        [Description("Full response content in markdown")]
        string responseContent)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, agentType);
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, "messages");

        // Read task ID from .task-id file
        var taskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
        if (!File.Exists(taskIdFile))
        {
            return "Error: No active task found (.task-id file missing). Are you running as a worker agent?";
        }

        var taskId = await File.ReadAllTextAsync(taskIdFile);
        taskId = taskId.Trim();

        // Create response filename
        var sanitizedSummary = string.Join("-", taskSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Replace(".", "").Replace(",", "");
        var responseFileName = $"{taskId}.{agentType}.response.{sanitizedSummary}.md";
        var responseFilePath = Path.Combine(messagesDirectory, responseFileName);

        // Write response file directly to messages directory
        await File.WriteAllTextAsync(responseFilePath, responseContent);

        // Delete .task-id file
        File.Delete(taskIdFile);

        // Log completion
        LogWorkflowEvent($"[{taskId}.{agentType}.response] Completed via MCP: '{taskSummary}' -> [{responseFileName}]", messagesDirectory);

        // Read .process-id file to find worker process
        var processIdFile = Path.Combine(agentWorkspaceDirectory, ".process-id");
        if (File.Exists(processIdFile))
        {
            var processIdContent = await File.ReadAllTextAsync(processIdFile);
            if (int.TryParse(processIdContent, out var workerProcessId))
            {
                // Kill the worker agent process (this process!)
                var workerProcess = Process.GetProcessById(workerProcessId);
                workerProcess.Kill();
            }
        }

        return $"Task completed. Response file: {responseFileName}";
    }

    [McpServerTool]
    [Description("Signal review completion from reviewer agent. Call this when you have finished reviewing a task. This will write your response file and terminate your session.")]
    public static async Task<string> CompleteReview(
        [Description("Agent type (backend-reviewer, frontend-reviewer, test-automation-reviewer)")]
        string agentType,
        [Description("Review approved (true) or rejected (false)")]
        bool approved,
        [Description("Brief review summary in sentence case (e.g., 'Excellent implementation', 'Missing tests')")]
        string reviewSummary,
        [Description("Full response content in markdown")]
        string responseContent)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, agentType);
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, "messages");

        // Read task ID from .task-id file
        var taskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
        if (!File.Exists(taskIdFile))
        {
            return "Error: No active task found (.task-id file missing). Are you running as a reviewer agent?";
        }

        var taskId = await File.ReadAllTextAsync(taskIdFile);
        taskId = taskId.Trim();

        // Create response filename with status prefix
        var statusPrefix = approved ? "Approved" : "Rejected";
        var sanitizedSummary = string.Join("-", reviewSummary.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Replace(".", "").Replace(",", "");
        var responseFileName = $"{taskId}.{agentType}.response.{statusPrefix}-{sanitizedSummary}.md";
        var responseFilePath = Path.Combine(messagesDirectory, responseFileName);

        // Write response file directly to messages directory
        await File.WriteAllTextAsync(responseFilePath, responseContent);

        // Delete .task-id file
        File.Delete(taskIdFile);

        // Log completion
        LogWorkflowEvent($"[{taskId}.{agentType}.response] Review completed via MCP ({statusPrefix}): '{reviewSummary}' -> [{responseFileName}]", messagesDirectory);

        // Read .process-id file to find reviewer process
        var processIdFile = Path.Combine(agentWorkspaceDirectory, ".process-id");
        if (File.Exists(processIdFile))
        {
            var processIdContent = await File.ReadAllTextAsync(processIdFile);
            if (int.TryParse(processIdContent, out var reviewerProcessId))
            {
                // Kill the reviewer agent process (this process!)
                var reviewerProcess = Process.GetProcessById(reviewerProcessId);
                reviewerProcess.Kill();
            }
        }

        return $"Review completed ({statusPrefix}). Response file: {responseFileName}";
    }

    private static async Task<string> WaitForWorkerCompletionAsync(string messagesDirectory, int counter, string agentType, int processId, string taskTitle, string requestFileName)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var workflowLog = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, "workflow.log");
        await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WaitForWorkerCompletion started for process ID: {processId}\n");

        var currentProcessId = processId;
        var restartCount = 0;
        var startTime = DateTime.Now;
        var overallTimeout = TimeSpan.FromHours(2);
        var inactivityCheckInterval = TimeSpan.FromMinutes(20);

        // Get Process object from active sessions
        if (!ClaudeAgentCommand.ActiveWorkerSessions.TryGetValue(processId, out var session))
        {
            return "Error: Worker session not found in active sessions";
        }

        var currentProcess = session.Process;

        while (DateTime.Now - startTime < overallTimeout)
        {
            // Block waiting for process exit or timeout (20 minutes)
            var exited = currentProcess.WaitForExit(inactivityCheckInterval);

            if (exited)
            {
                // Worker completed normally (called MCP CompleteTask or ReviewCompleted)
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker process exited normally\n");
                break;
            }

            // Timeout - check if worker is making progress
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Inactivity check: No completion after 20 minutes\n");

            var hasGitChanges = ClaudeAgentCommand.HasGitChanges();
            if (hasGitChanges)
            {
                // Worker is making changes, still active
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Git changes detected, worker is active\n");
                continue;
            }

            // No git changes for 20 minutes - worker is stuck
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] No git changes detected, restarting worker\n");

            // Kill stuck worker
            if (!currentProcess.HasExited)
            {
                currentProcess.Kill();
            }

            // Restart worker
            var restartResult = await RestartWorker(agentType, messagesDirectory, requestFileName);
            if (!restartResult.Success || restartResult.Process == null)
            {
                return $"Worker restart failed: {restartResult.ErrorMessage}";
            }

            UpdateWorkerSession(currentProcessId, restartResult.ProcessId, agentType, taskTitle, requestFileName, restartResult.Process);
            currentProcess = restartResult.Process;
            currentProcessId = restartResult.ProcessId;
            restartCount++;

            LogWorkerActivity($"WORKER RESTART: {agentType} inactive for 20 minutes (no git changes), restarted (attempt {restartCount})", messagesDirectory);
        }

        // Check for overall timeout
        if (DateTime.Now - startTime >= overallTimeout)
        {
            if (!currentProcess.HasExited)
            {
                currentProcess.Kill();
            }

            return $"Worker timeout after 2 hours (restarts: {restartCount})";
        }

        // Worker exited - response file should be in messages directory (written by MCP tool)
        var responseFilePattern = $"{counter:D4}.{agentType}.response.*.md";
        var matchingFiles = Directory.GetFiles(messagesDirectory, responseFilePattern);

        if (matchingFiles.Length == 0)
        {
            return $"Worker exited but no response file found matching: {responseFilePattern}";
        }

        var responseFilePath = matchingFiles[0];
        var responseFileName = Path.GetFileName(responseFilePath);

        var responseContent = await File.ReadAllTextAsync(responseFilePath);

        LogWorkflowEvent($"[{counter:D4}.{agentType}.response] Completed: '{responseFileName}' (restarts: {restartCount})", messagesDirectory);

        return $"Task completed successfully by {agentType}.\n" +
               $"Task number: {counter:D4}\n" +
               $"Task title: {taskTitle}\n" +
               $"Request file: {requestFileName}\n" +
               $"Response file: {responseFileName}\n" +
               $"Restarts needed: {restartCount}\n\n" +
               $"Response content:\n{responseContent}";
    }

    // ReSharper disable UnusedParameter.Local - messagesDirectory and requestFileName needed for call signature consistency
    private static async Task<(bool Success, int ProcessId, Process? Process, string ErrorMessage)> RestartWorker(string agentType, string messagesDirectory, string requestFileName)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var branchWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName);
        var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDir, agentType);

        // Load system prompt
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder
        };

        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        if (File.Exists(systemPromptFile))
        {
            var systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
            systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
            claudeArgs.Add("--append-system-prompt");
            claudeArgs.Add(systemPromptText);
        }

        // Add restart nudge
        var completionCommand = agentType.Contains("reviewer") ? "/complete/review" : "/complete/task";
        claudeArgs.Add("--append-system-prompt");
        claudeArgs.Add($"You are a {agentType} Worker. It looks like you stopped. " +
                       $"Please re-read the latest request file and continue working on it. " +
                       $"Remember to call {completionCommand} when done or if stuck.");

        // Use common launch method (handles session management)
        var process = await ClaudeAgentCommand.LaunchClaudeCode(agentWorkspaceDirectory, claudeArgs);
        await Task.Delay(TimeSpan.FromSeconds(2));

        if (process.HasExited)
        {
            return (false, -1, null, $"Process exited immediately with code: {process.ExitCode}");
        }

        return (true, process.Id, process, "");
    }

    private static void UpdateWorkerSession(int oldProcessId, int newProcessId, string agentType, string taskTitle, string requestFileName, Process newProcess)
    {
        // Update PID file with new process ID
        var branchName = GitHelper.GetCurrentBranch();
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, agentType);
        var processIdFile = Path.Combine(agentWorkspaceDirectory, ".process-id");
        File.WriteAllText(processIdFile, newProcessId.ToString());

        ClaudeAgentCommand.RemoveWorkerSession(oldProcessId);
        ClaudeAgentCommand.AddWorkerSession(newProcessId, agentType, taskTitle, requestFileName, newProcess);
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

    private static void LogWorkflowEvent(string message, string messagesDirectory)
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

// ReSharper disable once NotAccessedPositionalProperty.Global - RequestFileName used in ToString for debugging
public record WorkerSession(
    int ProcessId,
    string AgentType,
    string TaskTitle,
    string RequestFileName,
    DateTime StartTime,
    Process Process
);
