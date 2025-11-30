using System.CommandLine;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ClaudeAgentCommand : Command
{
    private const int MinRestartIntervalMinutes = 20;
    private static bool _showAllActivities;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static DateTime? _lastTechLeadRestartTime;

    private static readonly string[] WorkerAgentTypes =
    [
        "backend-engineer",
        "frontend-engineer",
        "qa-engineer",
        "backend-reviewer",
        "frontend-reviewer",
        "qa-reviewer",
        "tech-lead",
        "coordinator",
        "pair-programmer"
    ];

    public ClaudeAgentCommand() : base("claude-agent", "Interactive Worker Host for agent development")
    {
        var targetAgentTypeArgument = new Argument<string?>("target-agent-type")
        {
            Description = "Target agent type to run (tech-lead, coordinator, backend-engineer, backend-reviewer, frontend-engineer, frontend-reviewer, qa-engineer, qa-reviewer)",
            Arity = ArgumentArity.ZeroOrOne,
            DefaultValueFactory = _ => null
        };
        var mcpOption = new Option<bool>("--mcp") { Description = "Run in MCP mode (called from MCP server)" };
        var senderAgentTypeOption = new Option<string?>("--sender-agent-type") { Description = "Sender agent type (who is calling start_worker_agent)" };
        var taskTitleOption = new Option<string?>("--task-title") { Description = "Task title for MCP mode" };
        var markdownContentOption = new Option<string?>("--markdown-content") { Description = "Task content in markdown format" };
        var branchOption = new Option<string?>("--branch") { Description = "Branch name for MCP mode" };
        var featureIdOption = new Option<string?>("--feature-id") { Description = "[FeatureId] (optional, for regular tasks)" };
        var taskIdOption = new Option<string?>("--task-id") { Description = "[TaskId]" };
        var resetMemoryOption = new Option<bool>("--reset-memory") { Description = "Reset Claude Code session memory (true for each new [task])" };
        var requestFilePathOption = new Option<string?>("--request-file-path") { Description = "Request file path (optional, for review tasks)" };
        var responseFilePathOption = new Option<string?>("--response-file-path") { Description = "Response file path (optional, for review tasks)" };

        Arguments.Add(targetAgentTypeArgument);
        Options.Add(mcpOption);
        Options.Add(senderAgentTypeOption);
        Options.Add(taskTitleOption);
        Options.Add(markdownContentOption);
        Options.Add(branchOption);
        Options.Add(featureIdOption);
        Options.Add(taskIdOption);
        Options.Add(resetMemoryOption);
        Options.Add(requestFilePathOption);
        Options.Add(responseFilePathOption);

        SetAction(async parseResult => await ExecuteAsync(
                parseResult.GetValue(targetAgentTypeArgument),
                parseResult.GetValue(senderAgentTypeOption),
                parseResult.GetValue(mcpOption),
                parseResult.GetValue(taskTitleOption),
                parseResult.GetValue(markdownContentOption),
                parseResult.GetValue(branchOption),
                parseResult.GetValue(featureIdOption),
                parseResult.GetValue(taskIdOption),
                parseResult.GetValue(resetMemoryOption)
            )
        );
    }

    // Entry Point
    private async Task ExecuteAsync(
        string? targetAgentType,
        string? senderAgentType,
        bool mcp,
        string? taskTitle,
        string? markdownContent,
        string? branch,
        string? featureId,
        string? taskId,
        bool resetMemory)
    {
        try
        {
            if (mcp)
            {
                await RunMcpMode(targetAgentType, senderAgentType, taskTitle, markdownContent, branch, featureId, taskId, resetMemory);
            }
            else
            {
                await RunInteractiveMode(targetAgentType);
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Command execution failed: {ex.Message}");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    // MCP Mode (called from MCP server to delegate to interactive worker-host)
    private async Task RunMcpMode(
        string? targetAgentType,
        string? senderAgentType,
        string? taskTitle,
        string? markdownContent,
        string? branch,
        string? featureId,
        string? taskId,
        bool resetMemory)
    {
        if (string.IsNullOrEmpty(targetAgentType) || string.IsNullOrEmpty(taskTitle) || string.IsNullOrEmpty(markdownContent))
        {
            throw new ArgumentException("--mcp mode requires target-agent-type, --task-title, --markdown-content, and --branch");
        }

        senderAgentType ??= "";

        if (string.IsNullOrEmpty(branch))
        {
            throw new ArgumentException("--branch is required to ensure workspace consistency");
        }

        // Validate branch matches current git branch
        var currentGitBranch = GitHelper.GetCurrentBranch();
        if (currentGitBranch != branch)
        {
            await Console.Out.WriteLineAsync(
                $"ERROR: Branch mismatch detected!\n\n" +
                $"Worker requesting delegation is on branch: '{branch}'\n" +
                $"Current git branch: '{currentGitBranch}'\n\n" +
                $"This prevents workspace corruption. Ensure all agents are on the same branch."
            );
            return;
        }

        var workspace = new Workspace(targetAgentType, branch);
        Logger.SetContext($"mcp-{targetAgentType}");
        Logger.SetBranch(branch);

        // Reset Claude Code session memory if this is the first [task] in a [feature]
        if (resetMemory)
        {
            var sessionFile = workspace.SessionIdFile;
            if (File.Exists(sessionFile))
            {
                File.Delete(sessionFile);
                ClaudeAgentLifecycle.LogWorkflowEvent("Deleted .claude-session-id to reset memory for new [task]");
            }

            // Cascade memory reset to reviewer if not running
            var reviewerType = targetAgentType switch
            {
                "backend-engineer" => "backend-reviewer",
                "frontend-engineer" => "frontend-reviewer",
                "qa-engineer" => "qa-reviewer",
                _ => null
            };

            if (reviewerType is not null)
            {
                var reviewerWorkspace = new Workspace(reviewerType, branch);
                if (!IsProcessAlive(reviewerWorkspace.WorkerProcessIdFile) && File.Exists(reviewerWorkspace.SessionIdFile))
                {
                    File.Delete(reviewerWorkspace.SessionIdFile);
                    ClaudeAgentLifecycle.LogWorkflowEvent($"Cascaded memory reset to {reviewerType}");
                }
            }
        }

        // Check if interactive worker-host is running
        if (!File.Exists(workspace.HostProcessIdFile))
        {
            await Console.Out.WriteLineAsync($"ERROR: No interactive '{targetAgentType}' worker-host running on branch '{branch}'.\nStart with: {Configuration.AliasName} claude-agent {targetAgentType}");
            return;
        }

        var processId = int.Parse(await File.ReadAllTextAsync(workspace.HostProcessIdFile));
        var existingProcess = Process.GetProcessById(processId);

        if (existingProcess.HasExited)
        {
            File.Delete(workspace.HostProcessIdFile);
            await Console.Out.WriteLineAsync($"ERROR: Worker-host process {processId} has exited");
            return;
        }

        // Check for duplicate delegation - detect if this taskId is already being worked on
        var existingRequest = await FindRequestByTaskId(workspace, targetAgentType, taskId ?? "");
        var shouldCreateNewRequest = true;
        var taskCounter = 0; // Will be set below
        var taskRequestFileName = string.Empty; // Will be set below

        if (existingRequest != null)
        {
            // Request already exists for this taskId - check if response exists
            var responsePattern = $"{existingRequest.Value.TaskNumber:D4}.{targetAgentType}.response.*.md";
            var existingResponses = Directory.GetFiles(workspace.MessagesDirectory, responsePattern);

            if (existingResponses.Length == 0)
            {
                // Request exists WITHOUT response - monitor existing request instead of creating duplicate
                taskCounter = existingRequest.Value.TaskNumber;
                taskRequestFileName = Path.GetFileName(existingRequest.Value.FilePath);
                shouldCreateNewRequest = false;
                ClaudeAgentLifecycle.LogWorkflowEvent($"[{taskCounter:D4}.{targetAgentType}.request] Monitoring existing: '{taskTitle}' -> [{taskRequestFileName}]");
            }
            // If response exists, shouldCreateNewRequest remains true - will create new request below
        }

        // Create new request if needed (no existing request OR existing request already has response)
        if (shouldCreateNewRequest)
        {
            taskCounter = await GetNextTaskCounter(workspace);

            // Create request file with headers
            var now = DateTime.Now;
            var requestContentWithHeaders =
                $"""
                 ---
                 from: {senderAgentType}
                 to: {targetAgentType}
                 request-number: {taskCounter:D4}
                 timestamp: {now:yyyy-MM-ddTHH:mm:sszzz}
                 feature-id: {featureId ?? "ad-hoc"}
                 task-id: {taskId}
                 ---

                 {markdownContent}
                 """;

            taskRequestFileName = CreateRequestFileName(taskCounter, targetAgentType, taskTitle);
            var taskRequestFilePath = Path.Combine(workspace.MessagesDirectory, taskRequestFileName);
            await File.WriteAllTextAsync(taskRequestFilePath, requestContentWithHeaders);

            // Only write current-task.json if worker is NOT currently processing
            // If worker is busy, the request file will be picked up after current task completes
            // This prevents overwriting current-task.json while worker is using it
            if (!File.Exists(workspace.WorkerProcessIdFile))
            {
                // Determine attempt number by counting previous responses for this taskId (for re-reviews)
                var attempt = 1;
                if (Directory.Exists(workspace.MessagesDirectory) && !string.IsNullOrEmpty(taskId))
                {
                    // Count response files from this agent with matching taskId in headers
                    var responseFiles = Directory.GetFiles(workspace.MessagesDirectory, $"*.{targetAgentType}.response.*.md");
                    foreach (var responseFile in responseFiles)
                    {
                        var content = await File.ReadAllTextAsync(responseFile);
                        if (content.Contains($"task-id: {taskId}"))
                        {
                            attempt++;
                        }
                    }
                }

                var taskInfo = CreateTaskMetadata(taskCounter, taskRequestFilePath, taskTitle, featureId, taskId ?? "", senderAgentType, attempt);
                await WriteTaskMetadata(workspace, taskInfo);
            }

            ClaudeAgentLifecycle.LogWorkflowEvent($"[{taskCounter:D4}.{targetAgentType}.request] Started: '{taskTitle}' -> [{taskRequestFileName}]");
        }

        // Wait for response file (no polling, no timeout - worker manages its own lifecycle)
        var responseFilePattern = $"{taskCounter:D4}.{targetAgentType}.response.*.md";

        // Check if response already exists (worker might complete before we start watching)
        var existingFiles = Directory.GetFiles(workspace.MessagesDirectory, responseFilePattern);
        string foundResponseFile;

        if (existingFiles.Length > 0)
        {
            foundResponseFile = existingFiles[0];
        }
        else
        {
            // Wait for response file using FileSystemWatcher (event-based, no polling)
            using var responseWatcher = new FileSystemWatcher(workspace.MessagesDirectory, responseFilePattern);
            var responseReceived = new TaskCompletionSource<string>();

            responseWatcher.Created += (_, e) => responseReceived.TrySetResult(e.FullPath);
            responseWatcher.EnableRaisingEvents = true;

            foundResponseFile = await responseReceived.Task; // Blocks until response file created
        }

        // Read and return response
        var responseContent = await File.ReadAllTextAsync(foundResponseFile);
        var actualResponseFileName = Path.GetFileName(foundResponseFile);

        ClaudeAgentLifecycle.LogWorkflowEvent($"[{taskCounter:D4}.{targetAgentType}.response] Completed: '{taskTitle}' -> [{actualResponseFileName}]");

        var result = $"Task delegated successfully to '{targetAgentType}'.\n" +
                     $"Task number: {taskCounter:D4}\n" +
                     $"Request file: {taskRequestFileName}\n" +
                     $"Response file: {actualResponseFileName}\n\n" +
                     $"Response content:\n{responseContent}";

        await Console.Out.WriteLineAsync(result);
    }

    private async Task RunInteractiveMode(string? targetAgentType)
    {
        // If no agent type provided, prompt for selection
        if (string.IsNullOrEmpty(targetAgentType))
        {
            targetAgentType = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Select an [green]agent type[/] to run:")
                    .AddChoices(
                        "tech-lead",
                        "coordinator",
                        "pair-programmer",
                        "backend-engineer",
                        "backend-reviewer",
                        "frontend-engineer",
                        "frontend-reviewer",
                        "qa-engineer",
                        "qa-reviewer"
                    )
            );
        }

        var workspace = new Workspace(targetAgentType);
        Logger.SetContext(targetAgentType);
        Logger.SetBranch(workspace.Branch);
        Logger.Info($"Worker-host starting for '{targetAgentType}' on branch: {workspace.Branch}");

        // Coordinator specific: Check for existing session and offer clean workspace option
        if (targetAgentType is "coordinator")
        {
            var branchWorkspacePath = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", workspace.Branch);

            if (Directory.Exists(branchWorkspacePath))
            {
                var activeWorkers = DetectActiveWorkers(branchWorkspacePath);
                var deadWorkers = DetectDeadWorkers(branchWorkspacePath);

                if (activeWorkers.Count > 0 || deadWorkers.Count > 0)
                {
                    var choice = PromptCleanWorkspace(activeWorkers, deadWorkers);

                    if (choice == "Clean restart (kill workers, wipe workspace)")
                    {
                        CleanBranchWorkspace(branchWorkspacePath, activeWorkers);
                    }
                }
            }
        }

        // Create workspace and register agent
        Directory.CreateDirectory(workspace.AgentWorkspaceDirectory);

        // Check for existing worker-host process
        if (File.Exists(workspace.HostProcessIdFile))
        {
            var existingProcessIdContent = await File.ReadAllTextAsync(workspace.HostProcessIdFile);
            if (int.TryParse(existingProcessIdContent, out var existingProcessId))
            {
                try
                {
                    var existingProcess = Process.GetProcessById(existingProcessId);

                    // Verify this is actually our worker-host process, not a reused PID
                    // On Unix-like systems, PIDs can be reused, so we check the process name
                    bool isOurProcess;
                    try
                    {
                        var processName = existingProcess.ProcessName.ToLowerInvariant();
                        // Worker-host processes are .NET processes, so they'll be "dotnet" or "pp" (self-contained)
                        isOurProcess = processName is "dotnet" or "pp";
                    }
                    catch
                    {
                        // If we can't access the process name, it's likely not our process or doesn't exist
                        isOurProcess = false;
                    }

                    if (isOurProcess && !existingProcess.HasExited)
                    {
                        // Active worker-host is running - calculate how long it's been alive
                        var processAge = DateTime.Now - existingProcess.StartTime;
                        var ageString = processAge.TotalMinutes < 1
                            ? $"{(int)processAge.TotalSeconds} seconds ago"
                            : processAge.TotalHours < 1
                                ? $"{(int)processAge.TotalMinutes} minutes {(int)(processAge.TotalSeconds % 60)} seconds ago"
                                : $"{(int)processAge.TotalHours} hours {processAge.Minutes} minutes ago";

                        AnsiConsole.MarkupLine($"[yellow]⚠ Another '{targetAgentType}' worker-host is currently running (PID: {existingProcessId}, Started: {ageString})[/]");

                        var choice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("What would you like to do?")
                                .AddChoices("Kill the existing worker-host and start fresh", "Exit")
                                .HighlightStyle(new Style(Color.Yellow))
                        );

                        if (choice == "Exit")
                        {
                            return;
                        }

                        // Kill existing process
                        KillProcess(existingProcess);
                        // Allow OS time to fully terminate process and release resources before proceeding
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist - stale PID file, just delete it silently
                }
            }

            File.Delete(workspace.HostProcessIdFile);
        }

        // Create .host-process-id so MCP can detect this interactive worker-host (only if not already owned by automated host)
        if (!File.Exists(workspace.HostProcessIdFile))
        {
            await File.WriteAllTextAsync(workspace.HostProcessIdFile, Process.GetCurrentProcess().Id.ToString());
        }

        // Ensure Ctrl+C exits cleanly and removes PID files
        Console.CancelKeyPress += (_, e) =>
        {
            // Clean up .host-process-id on exit
            if (File.Exists(workspace.HostProcessIdFile))
            {
                File.Delete(workspace.HostProcessIdFile);
            }

            // Clean up .worker-process-id if exists
            if (File.Exists(workspace.WorkerProcessIdFile))
            {
                File.Delete(workspace.WorkerProcessIdFile);
            }

            // Allow normal Ctrl+C behavior (exit process)
            e.Cancel = false;
        };

        // Display FigletText banner for the agent
        var displayName = GetAgentDisplayName(targetAgentType);

        // Set terminal title (skip for pair-programmer to let Claude Code control it)
        if (targetAgentType != "pair-programmer")
        {
            SetTerminalTitle($"{displayName} - {workspace.Branch}");
        }

        // Load small Figlet font for compact banner
        var smallFontPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli", "Fonts", "small.flf");
        var font = File.Exists(smallFontPath) ? FigletFont.Load(smallFontPath) : FigletFont.Default;
        var agentBanner = new FigletText(font, displayName).Color(GetAgentColor(targetAgentType));
        AnsiConsole.Write(agentBanner);

        var agentColor = GetAgentColor(targetAgentType);

        AnsiConsole.WriteLine(); // Extra line for spacing

        // Track coordinator recovery state
        string? coordinatorFeatureId = null;
        var coordinatorContinue = false;

        // Check for task recovery - if current-task.json exists, prompt user
        if (File.Exists(workspace.CurrentTaskFile))
        {
            var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
            var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);

            if (taskInfo is not null)
            {
                // Coordinator: Show incomplete feature prompt
                if (targetAgentType == "coordinator")
                {
                    AnsiConsole.MarkupLine($"[{agentColor}]Feature in progress: {Markup.Escape(taskInfo.TaskTitle)}[/]");

                    var wantsToContinue = await AnsiConsole.Console.PromptAsync(new ConfirmationPrompt("Continue this feature?"));

                    if (wantsToContinue)
                    {
                        coordinatorFeatureId = taskInfo.FeatureId;
                        coordinatorContinue = true;
                    }
                    else
                    {
                        File.Delete(workspace.CurrentTaskFile);
                    }

                    // Coordinator auto-launches below with custom slash command
                }
                else
                {
                    // Engineers/reviewers: Show incomplete task prompt
                    AnsiConsole.MarkupLine($"[{agentColor} bold]⚠️ INCOMPLETE TASK DETECTED[/]");
                    AnsiConsole.MarkupLine($"[dim]Task {taskInfo.TaskNumber} - '{Markup.Escape(taskInfo.TaskTitle)}' is currently in development.[/]");
                    AnsiConsole.WriteLine();

                    var wantsToContinue = await AnsiConsole.Console.PromptAsync(new ConfirmationPrompt("Do you want to continue this task?"));

                    AnsiConsole.MarkupLine($"[{agentColor}]Resuming session...[/]");
                    // Brief pause to allow user to read the resuming message before launching Claude Code
                    await Task.Delay(TimeSpan.FromSeconds(1));

                    // Launch manual session (with or without slash command based on user choice)
                    await LaunchManualClaudeSession(workspace, wantsToContinue ? taskInfo.TaskTitle : null, wantsToContinue);
                    // After recovery session ends, continue to main loop to wait for MCP requests
                }
            }
        }

        // Tech-lead, coordinator, and pair-programmer launch immediately, other agents wait for requests
        if (targetAgentType is "tech-lead" or "coordinator" or "pair-programmer")
        {
            var sessionIdFile = Path.Combine(workspace.AgentWorkspaceDirectory, ".claude-session-id");

            // Pair-programmer: single session with saved session management
            if (targetAgentType == "pair-programmer")
            {
                // Build session selection menu: Continue, Start new, then saved sessions by date
                var choices = new List<string>();
                var currentSessionId = File.Exists(sessionIdFile) ? (await File.ReadAllTextAsync(sessionIdFile)).Trim() : null;
                string? continueOption = null;

                if (Directory.Exists(workspace.AgentWorkspaceDirectory))
                {
                    var savedSessions = Directory.GetFiles(workspace.AgentWorkspaceDirectory, "*.claude-session-id")
                        .Where(f => Path.GetFileName(f) != ".claude-session-id")
                        .Select(f => new
                            {
                                Name = Path.GetFileNameWithoutExtension(f),
                                SessionId = File.ReadAllText(f).Trim(),
                                Date = File.GetLastWriteTime(f)
                            }
                        )
                        .OrderByDescending(s => s.Date);

                    foreach (var session in savedSessions)
                    {
                        if (session.SessionId == currentSessionId)
                        {
                            continueOption = $"Continue: {session.Name} ({session.Date:yyyy-MM-dd})";
                        }
                        else
                        {
                            choices.Add($"Resume: {session.Name} ({session.Date:yyyy-MM-dd})");
                        }
                    }
                }

                // Build final menu: Continue, Start new, then saved sessions
                var menu = new List<string>();
                if (continueOption != null)
                {
                    menu.Add(continueOption);
                }
                else if (currentSessionId != null) menu.Add("Continue previous session");

                menu.Add("Start new session");
                menu.AddRange(choices);

                if (menu.Count > 1)
                {
                    var selection = AnsiConsole.Prompt(new SelectionPrompt<string>()
                        .Title("Select a [darkorange]session[/]:").AddChoices(menu)
                    );

                    if (selection == "Start new session")
                    {
                        File.Delete(sessionIdFile);
                    }
                    else if (selection.StartsWith("Resume: "))
                    {
                        var name = selection.Split('(')[0].Substring(8).Trim();
                        File.Copy(Path.Combine(workspace.AgentWorkspaceDirectory, $"{name}.claude-session-id"), sessionIdFile, true);
                    }
                }

                await LaunchManualClaudeSession(workspace, useSlashCommand: false);

                // On exit: rename if already saved, else offer to save with name
                if (!File.Exists(sessionIdFile)) return;

                var sessionId = (await File.ReadAllTextAsync(sessionIdFile)).Trim();
                var existingSaved = Directory.Exists(workspace.AgentWorkspaceDirectory)
                    ? Directory.GetFiles(workspace.AgentWorkspaceDirectory, "*.claude-session-id")
                        .FirstOrDefault(f => f != sessionIdFile && File.ReadAllText(f).Trim() == sessionId)
                    : null;

                if (existingSaved != null && await AnsiConsole.Console.PromptAsync(new ConfirmationPrompt($"Rename '[darkorange]{Path.GetFileNameWithoutExtension(existingSaved)}[/]'?")))
                {
                    var title = await AnsiConsole.Console.PromptAsync(new TextPrompt<string>("New title:").DefaultValue(workspace.Branch));
                    File.Move(existingSaved, Path.Combine(workspace.AgentWorkspaceDirectory, $"{title}.claude-session-id"), true);
                }
                else if (existingSaved == null && await AnsiConsole.Console.PromptAsync(new ConfirmationPrompt("Save this session?")))
                {
                    var title = await AnsiConsole.Console.PromptAsync(new TextPrompt<string>("Session title:").DefaultValue(workspace.Branch));
                    File.Copy(sessionIdFile, Path.Combine(workspace.AgentWorkspaceDirectory, $"{title}.claude-session-id"), true);
                }

                return;
            }

            // Tech-lead and coordinator: infinite loop, relaunching after each session
            while (true)
            {
                var useSlashCommand = targetAgentType is "tech-lead" or "coordinator" && !File.Exists(sessionIdFile);

                // For coordinator continuing a feature, pass featureId as taskTitle
                string? taskTitleForSlash = null;
                if (targetAgentType == "coordinator" && coordinatorContinue && coordinatorFeatureId != null)
                {
                    taskTitleForSlash = coordinatorFeatureId;
                    coordinatorContinue = false; // Only use once, then reset for next loop
                }

                await LaunchManualClaudeSession(workspace, taskTitleForSlash, useSlashCommand);
                Logger.Debug($"{targetAgentType} session ended, relaunching");
            }
        }

        // Setup for interactive mode
        Directory.CreateDirectory(workspace.MessagesDirectory);

        if (!File.Exists(workspace.HostProcessIdFile))
        {
            await File.WriteAllTextAsync(workspace.HostProcessIdFile, Process.GetCurrentProcess().Id.ToString());
        }

        // Display initial waiting screen with recent activity
        RedrawWaitingDisplay(targetAgentType, workspace.Branch);

        // Main loop: wait for requests or manual control
        while (true)
        {
            var (isRequest, requestPath) = await WaitForTasksOrManualControl(workspace);

            if (isRequest && requestPath != null)
            {
                await HandleIncomingRequest(requestPath, workspace);
            }
            else
            {
                await LaunchManualClaudeSession(workspace, useSlashCommand: false);
            }
        }
    }

    // Request Watching & Handling
    private async Task<(bool IsRequest, string? RequestPath)> WaitForTasksOrManualControl(Workspace workspace)
    {
        // Always check for unprocessed request files first (requests without responses)
        // This prevents race condition where MCP writes file between iterations
        if (!File.Exists(workspace.WorkerProcessIdFile))
        {
            var allRequests = Directory.GetFiles(workspace.MessagesDirectory, $"*.{workspace.AgentType}.request.*.md");
            var allResponses = Directory.GetFiles(workspace.MessagesDirectory, $"*.{workspace.AgentType}.response.*.md");

            var processedTaskNumbers = allResponses
                .Select(f => Regex.Match(Path.GetFileName(f), @"^(\d+)\.").Groups[1].Value)
                .ToHashSet();

            var unprocessedRequests = allRequests
                .Where(req =>
                    {
                        var taskNum = Regex.Match(Path.GetFileName(req), @"^(\d+)\.").Groups[1].Value;
                        return !processedTaskNumbers.Contains(taskNum);
                    }
                )
                .OrderBy(File.GetCreationTime)
                .ToList();

            if (unprocessedRequests.Count > 0)
            {
                Logger.Debug($"Found {unprocessedRequests.Count} unprocessed requests - processing oldest");
                return (true, unprocessedRequests[0]);
            }
        }

        // Wait for new request file or user input
        Logger.Debug($"Creating FileSystemWatcher for: {workspace.MessagesDirectory}");
        using var fileSystemWatcher = new FileSystemWatcher(workspace.MessagesDirectory, $"*.{workspace.AgentType}.request.*.md");
        fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime | NotifyFilters.LastWrite;

        var requestDetected = new TaskCompletionSource<string>();

        void OnFileDetected(object sender, FileSystemEventArgs e)
        {
            Logger.Debug($"FileSystemWatcher detected request file: {e.FullPath}");
            requestDetected.TrySetResult(e.FullPath);
        }

        fileSystemWatcher.Created += OnFileDetected;
        fileSystemWatcher.Changed += OnFileDetected;
        fileSystemWatcher.EnableRaisingEvents = true;

        // Display waiting screen
        RedrawWaitingDisplay(workspace.AgentType, workspace.Branch);

        // Wait for request file OR user ENTER
        while (true)
        {
            if (requestDetected.Task.IsCompleted)
            {
                return (true, await requestDetected.Task);
            }

            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter)
                {
                    Logger.Debug("User pressed ENTER - launching manual session");
                    return (false, null);
                }

                if (key.Key == ConsoleKey.A && (key.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    _showAllActivities = !_showAllActivities;
                    RedrawWaitingDisplay(workspace.AgentType, workspace.Branch);
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    private async Task HandleIncomingRequest(string requestFile, Workspace workspace)
    {
        Logger.Debug($"Processing request: {Path.GetFileName(requestFile)}");
        var agentColor = GetAgentColor(workspace.AgentType);
        var displayName = GetAgentDisplayName(workspace.AgentType);

        // Clear the waiting display
        AnsiConsole.Clear();

        // Show task received animation
        AnsiConsole.MarkupLine($"[{agentColor} bold]▶ TASK RECEIVED[/]");
        AnsiConsole.MarkupLine($"[dim]Request: {Path.GetFileName(requestFile)}[/]");

        // Allow filesystem time to complete file write operation before reading to avoid partial content
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Create current-task.json from request file if it doesn't exist
        // This handles the case where start_worker_agent didn't write it because worker was busy
        if (!File.Exists(workspace.CurrentTaskFile))
        {
            var requestContent = await File.ReadAllTextAsync(requestFile);
            var headerMatch = Regex.Match(requestContent, @"---\s+from:\s+(?<from>[^\n]+)\s+to:\s+(?<to>[^\n]+)\s+request-number:\s+(?<requestNumber>[^\n]+)\s+timestamp:\s+(?<timestamp>[^\n]+)\s+feature-id:\s+(?<featureId>[^\n]+)\s+task-id:\s+(?<taskId>[^\n]+)\s+---");

            if (headerMatch.Success)
            {
                var requestNumber = headerMatch.Groups["requestNumber"].Value.Trim();
                var timestampString = headerMatch.Groups["timestamp"].Value.Trim();
                var featureId = headerMatch.Groups["featureId"].Value.Trim();
                var taskId = headerMatch.Groups["taskId"].Value.Trim();
                var senderAgentType = headerMatch.Groups["from"].Value.Trim();

                // Treat "ad-hoc" as null for featureId
                if (featureId == "ad-hoc") featureId = null;

                // Extract title from request filename
                var fileName = Path.GetFileName(requestFile);
                var titleMatch = Regex.Match(fileName, @"^\d+\.[^.]+\.request\.(.+)\.md$");
                var taskTitle = titleMatch.Success ? titleMatch.Groups[1].Value.Replace("-", " ") : "task";

                // Parse and normalize timestamp to ISO 8601 format (same as CreateTaskMetadata)
                var timestamp = DateTime.TryParse(timestampString, out var parsedTime)
                    ? parsedTime.ToString("O")
                    : DateTime.Now.ToString("O");

                // Determine attempt number by counting previous responses for this taskId (for re-reviews)
                var attempt = 1;
                if (Directory.Exists(workspace.MessagesDirectory) && !string.IsNullOrEmpty(taskId))
                {
                    var responseFiles = Directory.GetFiles(workspace.MessagesDirectory, $"*.{workspace.AgentType}.response.*.md");
                    foreach (var responseFile in responseFiles)
                    {
                        var content = await File.ReadAllTextAsync(responseFile);
                        if (content.Contains($"task-id: {taskId}"))
                        {
                            attempt++;
                        }
                    }
                }

                // Manually construct CurrentTaskInfo to preserve original timestamp from request file
                var taskInfo = new CurrentTaskInfo(
                    requestNumber,
                    requestFile,
                    timestamp,
                    attempt,
                    featureId,
                    taskId,
                    taskTitle,
                    senderAgentType
                );
                await WriteTaskMetadata(workspace, taskInfo);
            }
        }

        // Update terminal title to show task context
        if (File.Exists(workspace.CurrentTaskFile))
        {
            var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
            var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);
            if (taskInfo is not null)
            {
                // Format: "AgentName - TaskId - Title - MessageId - HH:mm:ss"
                var titleParts = new List<string> { displayName };

                if (!string.IsNullOrEmpty(taskInfo.TaskId))
                {
                    titleParts.Add(taskInfo.TaskId);
                }

                titleParts.Add(taskInfo.TaskTitle);
                titleParts.Add(taskInfo.TaskNumber);

                if (DateTime.TryParse(taskInfo.StartedAt, out var startedAt))
                {
                    titleParts.Add(startedAt.ToString("HH:mm:ss"));
                }

                var title = string.Join(" - ", titleParts);
                SetTerminalTitle(title);
            }
        }

        // Launch worker-agent (Claude Code) - task title read from current-task.json
        var claudeProcess = await LaunchWorker(workspace);

        // Extract task number from request file
        var requestFileName = Path.GetFileName(requestFile);
        var match = Regex.Match(requestFileName, @"^(\d+)\.([^.]+)\.request\.(.+)\.md$");
        var taskNumber = match.Groups[1].Value;
        var responseFilePattern = $"{taskNumber}.{workspace.AgentType}.response.*.md";

        // Monitor process and wait for response file
        var options = new ProcessMonitoringOptions(
            TimeSpan.FromMinutes(20),
            true,
            taskNumber,
            responseFilePattern,
            workspace.MessagesDirectory
        );

        var result = await MonitorProcessWithTimeout(claudeProcess, workspace.AgentType, options, workspace);

        // Clean up .worker-process-id after worker exits
        if (File.Exists(workspace.WorkerProcessIdFile))
        {
            File.Delete(workspace.WorkerProcessIdFile);
        }

        // Show completion status
        AnsiConsole.MarkupLine(result.Success ? $"[{agentColor} bold]✓ {result.Message}[/]" : $"[red bold]✗ {result.Message}[/]");

        // Restore terminal title to show branch name
        SetTerminalTitle($"{displayName} - {workspace.Branch}");

        // Return to waiting display
        RedrawWaitingDisplay(workspace.AgentType, workspace.Branch);
    }

    private async Task LaunchManualClaudeSession(Workspace workspace, string? taskTitleForSlashCommand = null, bool useSlashCommand = true)
    {
        // Launch worker (slash command usage controlled by useSlashCommand parameter)
        var process = await LaunchWorker(workspace, taskTitleForSlashCommand, useSlashCommand);

        // Monitor process and wait for completion
        // Tech-lead and coordinator get 62 minutes (user might be thinking), workers get 20 minutes
        var inactivityTimeout = workspace.AgentType is "tech-lead" or "coordinator"
            ? TimeSpan.FromMinutes(62)
            : TimeSpan.FromMinutes(20);

        var options = new ProcessMonitoringOptions(
            inactivityTimeout,
            false
        );

        var result = await MonitorProcessWithTimeout(process, workspace.AgentType, options, workspace);

        // Show completion status
        var agentColor = GetAgentColor(workspace.AgentType);
        AnsiConsole.MarkupLine(result.Success ? $"[{agentColor} bold]✓ {result.Message}[/]" : $"[red bold]✗ {result.Message}[/]");
        Logger.Debug($"Manual session completed - {result.Message}");

        // Clean up
        if (File.Exists(workspace.WorkerProcessIdFile))
        {
            File.Delete(workspace.WorkerProcessIdFile);
        }
    }

    private async Task<string> GetRulesForAgentType(string agentType)
    {
        // Map agent type to rules directory
        var rulesPath = agentType switch
        {
            "backend-engineer" or "backend-reviewer" => Path.Combine(Configuration.SourceCodeFolder, ".claude", "rules", "backend"),
            "frontend-engineer" or "frontend-reviewer" => Path.Combine(Configuration.SourceCodeFolder, ".claude", "rules", "frontend"),
            "qa-engineer" or "qa-reviewer" => Path.Combine(Configuration.SourceCodeFolder, ".claude", "rules", "end-to-end-tests"),
            _ => null
        };

        if (rulesPath is null || !Directory.Exists(rulesPath))
        {
            return string.Empty;
        }

        // Get all .md files
        var allRuleFiles = Directory.GetFiles(rulesPath, "*.md");

        if (allRuleFiles.Length == 0)
        {
            return string.Empty;
        }

        // Determine the main file based on the folder name
        var mainFileName = agentType switch
        {
            "backend-engineer" or "backend-reviewer" => "backend.md",
            "frontend-engineer" or "frontend-reviewer" => "frontend.md",
            "qa-engineer" or "qa-reviewer" => "e2e-tests.md",
            _ => null
        };

        // Sort files: main file first, then alphabetically
        var sortedFiles = allRuleFiles
            .OrderBy(f =>
                {
                    var fileName = Path.GetFileName(f);
                    return fileName == mainFileName ? "000-" + fileName : fileName;
                }
            )
            .ToList();

        // Concatenate all rule files with section headers
        var rulesBuilder = new StringBuilder();

        foreach (var ruleFile in sortedFiles)
        {
            var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ruleFile);
            var content = await File.ReadAllTextAsync(ruleFile);

            // Add section header
            rulesBuilder.AppendLine($"## {fileNameWithoutExtension}");
            rulesBuilder.AppendLine();
            rulesBuilder.AppendLine(content);
            rulesBuilder.AppendLine();
        }

        return rulesBuilder.ToString();
    }

    private async Task<Process> LaunchWorker(
        Workspace workspace,
        string? taskTitle = null,
        bool useSlashCommand = true,
        string? recoveryMessage = null)
    {
        // Load system prompt (REQUIRED - throw if missing)
        if (!File.Exists(workspace.SystemPromptFile))
        {
            throw new FileNotFoundException($"System prompt file not found: {workspace.SystemPromptFile}");
        }

        var systemPromptText = await File.ReadAllTextAsync(workspace.SystemPromptFile);

        // Concatenate all rule files for this agent type
        var rulesContent = await GetRulesForAgentType(workspace.AgentType);
        if (!string.IsNullOrEmpty(rulesContent))
        {
            systemPromptText += $"\n\n---\n\n# Architectural Rules\n\nYou have complete knowledge of all architectural rules and patterns. Reference these rules when implementing or reviewing code.\n\nNote: All architectural rules are embedded in your system prompt and available for reference.\n\n{rulesContent}";
        }

        systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();

        // Build standard arguments
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--permission-mode", "bypassPermissions",
            "--append-system-prompt", systemPromptText
        };

        // Add Chrome DevTools MCP for frontend and QA agents
        if (workspace.AgentType == "frontend-engineer" ||
            workspace.AgentType == "frontend-reviewer" ||
            workspace.AgentType == "qa-engineer" ||
            workspace.AgentType == "qa-reviewer")
        {
            claudeArgs.Add("--mcp-config");
            claudeArgs.Add(Path.Combine(Configuration.SourceCodeFolder, ".claude", "agentic-workflow", "mcp-configs", "chrome-devtools.json"));
            // NOTE: Not using --strict-mcp-config so it merges with user config

            // Add -- separator to mark end of options (prevents slash command from being parsed as MCP config value)
            claudeArgs.Add("--");
        }

        // Add recovery message if this is a restart
        if (recoveryMessage != null)
        {
            claudeArgs.Add(recoveryMessage);
        }

        // Add slash command only if requested (manual sessions may launch without slash command)
        if (useSlashCommand)
        {
            // Determine task title for slash command (keep it simple - just the title)
            var effectiveTaskTitle = taskTitle;
            var attempt = 1;
            if (effectiveTaskTitle == null)
            {
                effectiveTaskTitle = "task";
                if (File.Exists(workspace.CurrentTaskFile))
                {
                    var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
                    var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);
                    if (taskInfo is not null)
                    {
                        effectiveTaskTitle = taskInfo.TaskTitle;
                        attempt = taskInfo.Attempt;
                    }
                }
            }

            // Add attempt number to task title for reviewers on re-reviews
            if (workspace.AgentType.Contains("reviewer") && attempt > 1)
            {
                effectiveTaskTitle = $"{effectiveTaskTitle} (attempt #{attempt})";
            }

            // Add slash command to trigger workflow
            var slashCommand = workspace.AgentType switch
            {
                "tech-lead" => "/modes:tech-lead",
                "coordinator" => string.IsNullOrEmpty(effectiveTaskTitle) || effectiveTaskTitle == "task"
                    ? "/process:implement-feature"
                    : $"/process:implement-feature {effectiveTaskTitle}",
                "pair-programmer" => null, // No slash command for pair-programmer
                "qa-engineer" => $"/process:implement-end-to-end-tests {effectiveTaskTitle}",
                "qa-reviewer" => $"/process:review-end-to-end-tests {effectiveTaskTitle}",
                _ => workspace.AgentType.Contains("reviewer")
                    ? $"/process:review-task {effectiveTaskTitle}"
                    : $"/process:implement-task {effectiveTaskTitle}"
            };
            if (slashCommand is not null)
            {
                claudeArgs.Add(slashCommand);
            }
        }

        // Launch with session management from source code root
        Logger.Debug($"LAUNCH - Starting Claude Code for {workspace.AgentType}");
        var process = await LaunchClaudeCode(workspace, claudeArgs);

        // Verification delay to confirm process launched successfully before returning
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Create .worker-process-id with worker-agent's process ID
        await File.WriteAllTextAsync(workspace.WorkerProcessIdFile, process.Id.ToString());

        return process;
    }

    // Task Management Helpers

    private async Task<int> GetNextTaskCounter(Workspace workspace)
    {
        var counter = 1;
        if (File.Exists(workspace.TaskCounterFile) && int.TryParse(await File.ReadAllTextAsync(workspace.TaskCounterFile), out var currentCounter))
        {
            counter = currentCounter + 1;
        }

        await File.WriteAllTextAsync(workspace.TaskCounterFile, counter.ToString());
        return counter;
    }

    private static string CreateRequestFileName(int taskCounter, string agentType, string taskTitle)
    {
        var shortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .ToLowerInvariant();

        // Remove all special characters, keep only alphanumeric and hyphens
        shortTitle = Regex.Replace(shortTitle, "[^a-z0-9-]", "");

        return $"{taskCounter:D4}.{agentType}.request.{shortTitle}.md";
    }

    private static CurrentTaskInfo CreateTaskMetadata(
        int taskCounter,
        string requestFilePath,
        string taskTitle,
        string? featureId,
        string taskId,
        string senderAgentType,
        int attempt = 1)
    {
        return new CurrentTaskInfo(
            $"{taskCounter:D4}",
            requestFilePath,
            DateTime.Now.ToString("O"),
            attempt,
            featureId,
            taskId,
            taskTitle,
            senderAgentType
        );
    }

    private async Task WriteTaskMetadata(Workspace workspace, CurrentTaskInfo taskInfo)
    {
        await File.WriteAllTextAsync(workspace.CurrentTaskFile, JsonSerializer.Serialize(taskInfo, JsonOptions));
    }

    private async Task<(int TaskNumber, string FilePath)?> FindRequestByTaskId(Workspace workspace, string targetAgentType, string taskId)
    {
        if (!Directory.Exists(workspace.MessagesDirectory))
        {
            return null;
        }

        var requestPattern = $"*.{targetAgentType}.request.*.md";
        var requestFiles = Directory.GetFiles(workspace.MessagesDirectory, requestPattern);

        foreach (var file in requestFiles)
        {
            var content = await File.ReadAllTextAsync(file);
            // Parse YAML frontmatter for task-id
            if (content.Contains($"task-id: {taskId}"))
            {
                var fileName = Path.GetFileName(file);
                var taskNumberStr = fileName.Split('.')[0];
                if (int.TryParse(taskNumberStr, out var taskNumber))
                {
                    return (taskNumber, file);
                }
            }
        }

        return null;
    }

    // Setup & Utilities

    private static async Task<Process> LaunchClaudeCode(Workspace workspace, List<string> additionalArgs)
    {
        var workingDirectory = Configuration.SourceCodeFolder;
        var sessionIdFile = Path.Combine(workspace.AgentWorkspaceDirectory, ".claude-session-id");

        string sessionId;
        bool isResume;

        // Check if session exists
        if (File.Exists(sessionIdFile))
        {
            sessionId = (await File.ReadAllTextAsync(sessionIdFile)).Trim();
            isResume = true;
        }
        else
        {
            sessionId = Guid.NewGuid().ToString();
            await File.WriteAllTextAsync(sessionIdFile, sessionId);
            isResume = false;
        }

        // Build arguments with session management
        var args = new List<string>();

        // Add session argument (--resume or --session-id)
        if (isResume)
        {
            args.Add("--resume");
            args.Add(sessionId);
        }
        else
        {
            args.Add("--session-id");
            args.Add(sessionId);
        }

        // Add all other arguments
        args.AddRange(additionalArgs);

        var commandLine = string.Join(" ", args.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
        Logger.Debug($"Command: claude {commandLine}");

        var process = new Process
        {
            StartInfo = BuildProcessStartInfo(args, workingDirectory)
        };

        process.Start();

        return process;
    }

    // Display & UI
    private static void RedrawWaitingDisplay(string agentType, string branch)
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

    private static List<string> GetRecentActivity(string agentType, string branch)
    {
        var workspace = new Workspace(agentType, branch);
        var activities = new List<string>();

        if (!Directory.Exists(workspace.MessagesDirectory))
        {
            return activities;
        }

        try
        {
            // Find completed response files for this agent type
            var responseFiles = Directory.GetFiles(workspace.MessagesDirectory, $"*.{workspace.AgentType}.response.*.md")
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
                    var requestFileName = $"{taskNumber}.{workspace.AgentType}.request.*.md";
                    var requestFiles = Directory.GetFiles(workspace.MessagesDirectory, requestFileName);

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

    private static string GetAgentDisplayName(string agentType)
    {
        return agentType switch
        {
            "tech-lead" => "Tech Lead",
            "coordinator" => "Coordinator",
            "backend-engineer" => "Backend Engineer",
            "frontend-engineer" => "Frontend Engineer",
            "backend-reviewer" => "Backend Reviewer",
            "frontend-reviewer" => "Frontend Reviewer",
            "qa-engineer" => "QA Engineer",
            "qa-reviewer" => "QA Reviewer",
            "pair-programmer" => "Pair Programmer",
            _ => throw new ArgumentException($"Unknown agent type: '{agentType}'")
        };
    }

    private static Color GetAgentColor(string agentType)
    {
        return agentType switch
        {
            "tech-lead" => Color.Red,
            "coordinator" => Color.Purple,
            "backend-engineer" => Color.Green,
            "frontend-engineer" => Color.Blue,
            "backend-reviewer" => Color.Yellow,
            "frontend-reviewer" => Color.Orange3,
            "qa-engineer" => Color.Cyan1,
            "qa-reviewer" => Color.Magenta1,
            "pair-programmer" => Color.DarkOrange,
            _ => throw new ArgumentException($"Unknown agent type: '{agentType}'")
        };
    }

    private static void SetTerminalTitle(string title)
    {
        // ANSI escape sequence to set terminal title
        // Works in most modern terminals (iTerm2, Terminal.app, Windows Terminal, etc.)
        Console.Write($"\x1b]0;{title}\x07");
    }

    private static bool HasPendingRequest(Workspace workspace)
    {
        // Check if any request files exist in messages directory
        if (!Directory.Exists(workspace.MessagesDirectory)) return false;

        var requestFiles = Directory.GetFiles(workspace.MessagesDirectory, "*.request.*.md");
        return requestFiles.Length > 0;
    }

    private async Task<ProcessCompletionResult> MonitorProcessWithTimeout(Process process, string agentType, ProcessMonitoringOptions options, Workspace workspace)
    {
        // Manual session vs MCP request session have different monitoring logic
        if (!options.ExpectResponseFile)
        {
            return await MonitorManualSession(process, workspace);
        }

        return await MonitorMcpRequestSession(process, agentType, options, workspace);
    }

    private async Task<ProcessCompletionResult> MonitorManualSession(Process process, Workspace workspace)
    {
        // Manual sessions never timeout - they run indefinitely
        // Only interrupted if MCP request arrives AND user is AFK (no git changes in 20 min)
        const int pollIntervalMinutes = 5;

        while (true)
        {
            var exited = process.WaitForExit(TimeSpan.FromMinutes(pollIntervalMinutes));

            if (exited)
            {
                Logger.Debug($"Manual session process {process.Id} exited normally - exit code: {process.ExitCode}");
                return new ProcessCompletionResult(true, "Session completed");
            }

            // Check if MCP request arrived (but not for tech-lead, coordinator, or pair-programmer which never receive requests)
            if (workspace.AgentType is not "tech-lead" and not "coordinator" and not "pair-programmer" && HasPendingRequest(workspace))
            {
                Logger.Debug("Pending MCP request detected during manual session");

                // Only interrupt manual sessions with active tasks (skip if current-task.json missing)
                if (!File.Exists(workspace.CurrentTaskFile))
                {
                    Logger.Debug("No active task (current-task.json missing) - manual session continues");
                    continue;
                }

                // Check if user is actively working (git changes in last 20 min)
                var hasGitChanges = GitHelper.HasUncommittedChanges();

                if (!hasGitChanges)
                {
                    // No git activity - user is AFK, interrupt manual session for request
                    Logger.Debug("No git changes detected, interrupting manual session for pending request");
                    KillProcess(process);
                    return new ProcessCompletionResult(true, "Session interrupted for pending request");
                }

                // User is working - let them continue, request will wait
                Logger.Debug("Git changes detected, user is working - manual session continues");
            }

            // For tech-lead and coordinator, check for extended inactivity and restart if needed
            if (workspace.AgentType is "tech-lead" or "coordinator")
            {
                // Check all activity signals: workers, conversation, git
                var (hasActiveWorkers, _) = CheckWorkerProcessStatus(workspace.Branch);
                var conversationIdleTime = GetSessionIdleTime(workspace);
                var hasRecentGitActivity = GitHelper.HasRecentGitActivity(TimeSpan.FromMinutes(60));

                // Only restart if ALL signals show 60+ min inactivity
                var conversationIdle = conversationIdleTime.HasValue && conversationIdleTime >= TimeSpan.FromMinutes(60);
                var gitIdle = !hasRecentGitActivity;

                if (!hasActiveWorkers && gitIdle && conversationIdle)
                {
                    // Check minimum restart interval
                    if (_lastTechLeadRestartTime.HasValue)
                    {
                        var timeSinceRestart = DateTime.Now - _lastTechLeadRestartTime.Value;
                        if (timeSinceRestart < TimeSpan.FromMinutes(MinRestartIntervalMinutes))
                        {
                            Logger.Debug($"Tech-lead inactive but only {timeSinceRestart.TotalMinutes:F1} minutes since last restart - continuing");
                            continue; // Too soon to restart
                        }
                    }

                    Logger.Debug("Tech-lead inactive for 60+ minutes (no workers, no git, no conversation), restarting with recovery message");
                    _lastTechLeadRestartTime = DateTime.Now;

                    // Kill and restart with recovery message
                    KillProcess(process);
                    var relativeTaskPath = Path.GetRelativePath(Configuration.SourceCodeFolder, workspace.CurrentTaskFile);
                    var recoveryMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] You appear to have been interrupted or stuck. Please analyze the current state, check your current-task.json at {relativeTaskPath} for context, review recent git history to see what's been completed, and continue from where you left off.";
                    var newProcess = await LaunchWorker(workspace, recoveryMessage: recoveryMessage, useSlashCommand: false);
                    return await MonitorManualSession(newProcess, workspace);
                }
            }
        }
    }

    private async Task<ProcessCompletionResult> MonitorMcpRequestSession(Process process, string targetAgentType, ProcessMonitoringOptions options, Workspace workspace)
    {
        // MCP request sessions have inactivity timeout with restart logic
        var currentProcess = process;
        var restartCount = 0;
        var maxRestarts = targetAgentType is "tech-lead" ? int.MaxValue : 2;
        var inactivityThreshold = targetAgentType is "tech-lead"
            ? TimeSpan.FromMinutes(60)
            : TimeSpan.FromMinutes(20);
        var lastGitChangeDetected = DateTime.Now;
        var lastRestartTime = DateTime.Now;
        const int pollIntervalMinutes = 5;

        while (true)
        {
            var exited = currentProcess.WaitForExit(TimeSpan.FromMinutes(pollIntervalMinutes));

            if (exited)
            {
                break;
            }

            var (hasRecentActivity, idleTime) = targetAgentType switch
            {
                var type when type.EndsWith("-reviewer") => CheckReviewerActivity(workspace),
                var type when type.EndsWith("-engineer") => CheckEngineerActivity(workspace, lastGitChangeDetected),
                "tech-lead" => CheckTechLeadActivity(workspace, lastGitChangeDetected),
                _ => (GitHelper.HasRecentGitActivity(TimeSpan.FromMinutes(5)), DateTime.Now - lastGitChangeDetected)
            };

            if (hasRecentActivity)
            {
                lastGitChangeDetected = DateTime.Now;
            }
            else
            {
                var timeSinceLastChange = idleTime;

                if (timeSinceLastChange >= inactivityThreshold)
                {
                    // Worker is stuck - need to restart
                    // Check restart limits
                    if (restartCount >= maxRestarts)
                    {
                        var failMsg = $"Worker exhausted {maxRestarts} restarts without making progress";
                        Logger.Debug($"RECOVERY FAILED - Worker inactive for {timeSinceLastChange.TotalMinutes:F1} minutes, max restarts ({maxRestarts}) reached - {failMsg}");
                        KillProcess(currentProcess);
                        return new ProcessCompletionResult(false, failMsg);
                    }

                    // Check minimum time between restarts
                    if (restartCount > 0)
                    {
                        var timeSinceLastRestart = DateTime.Now - lastRestartTime;
                        if (timeSinceLastRestart < TimeSpan.FromMinutes(MinRestartIntervalMinutes))
                        {
                            Logger.Debug($"Worker inactive for {timeSinceLastChange.TotalMinutes:F1} minutes but only {timeSinceLastRestart.TotalMinutes:F1} minutes since last restart - waiting before retry");
                            continue;
                        }
                    }

                    // Kill current process
                    KillProcess(currentProcess);

                    // Launch new worker with recovery message
                    var relativeTaskPath = Path.GetRelativePath(Configuration.SourceCodeFolder, workspace.CurrentTaskFile);
                    var recoveryMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] You appear to have been interrupted or stuck. Please analyze the current state, check your current-task.json at {relativeTaskPath} for context, review recent git history to see what's been completed, and continue from where you left off.";

                    Logger.Debug($"RECOVERY RESTART - Worker inactive for {timeSinceLastChange.TotalMinutes:F1} minutes with no git changes - restarting (attempt {restartCount + 1}/{maxRestarts})");
                    var newProcess = await LaunchWorker(workspace, null, false, recoveryMessage);

                    // Update tracking
                    currentProcess = newProcess;
                    restartCount++;
                    lastGitChangeDetected = DateTime.Now; // Reset timestamp
                    lastRestartTime = DateTime.Now; // Track restart time

                    Logger.Debug($"Worker restarted successfully with PID {newProcess.Id}");
                    continue;
                }

                Logger.Debug($"No git changes detected, but only {timeSinceLastChange.TotalMinutes:F1} minutes since last change - continuing");
            }
        }

        // MCP request session completed - check for response file
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        var matchingFiles = Directory.GetFiles(options.MessagesDirectory!, options.ResponseFilePattern!);
        if (matchingFiles.Length == 0)
        {
            var errorMsg = $"Worker exited but no response file found matching: '{options.ResponseFilePattern}'";
            Logger.Debug($"MCP request session FAILED - {errorMsg}");
            return new ProcessCompletionResult(false, errorMsg);
        }

        var responseFilePath = matchingFiles[0];
        var responseFileName = Path.GetFileName(responseFilePath);
        var responseContent = await File.ReadAllTextAsync(responseFilePath);

        ClaudeAgentLifecycle.LogWorkflowEvent($"Completed: '{responseFileName}'");

        return new ProcessCompletionResult(true, "Task completed successfully", responseContent);
    }

    private static void KillProcess(Process process)
    {
        if (process.HasExited)
        {
            return;
        }

        if (Configuration.IsWindows)
        {
            // Windows: Direct termination (no SIGINT support)
            process.Kill();
        }
        else
        {
            // Unix/Mac: Graceful SIGINT first, then force kill
            try
            {
                // Send SIGINT signal to allow Claude Code to cleanup
                ProcessHelper.StartProcess($"kill -SIGINT {process.Id}");

                // Wait briefly for graceful shutdown
                var exited = process.WaitForExit(TimeSpan.FromSeconds(3));

                if (!exited && !process.HasExited)
                {
                    // Graceful shutdown failed, force kill
                    process.Kill();
                }
            }
            catch
            {
                // Fallback to direct kill if SIGINT fails
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
        }
    }

    private static ProcessStartInfo BuildProcessStartInfo(List<string> claudeArgs, string workingDirectory)
    {
        // Properly escape arguments: escape backslashes and quotes, then wrap in quotes if needed
        static string EscapeArg(string arg)
        {
            // If arg contains quotes or backslashes, escape them
            var escaped = arg.Replace("\\", "\\\\").Replace("\"", "\\\"");
            // Wrap in quotes if contains spaces, quotes, or other shell metacharacters
            return arg.Contains(' ') || arg.Contains('"') || arg.Contains('\\')
                ? $"\"{escaped}\""
                : escaped;
        }

        return new ProcessStartInfo
        {
            FileName = "claude",
            Arguments = string.Join(" ", claudeArgs.Select(EscapeArg)),
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };
    }

    private static List<(string AgentType, int Pid, DateTime StartTime)> DetectActiveWorkers(string branchWorkspacePath)
    {
        var active = new List<(string, int, DateTime)>();
        if (!Directory.Exists(branchWorkspacePath)) return active;

        foreach (var dir in Directory.GetDirectories(branchWorkspacePath).Where(d => Path.GetFileName(d) != "messages"))
        {
            var pidFile = Path.Combine(dir, ".host-process-id");
            if (File.Exists(pidFile) && int.TryParse(File.ReadAllText(pidFile), out var pid))
            {
                try
                {
                    var proc = Process.GetProcessById(pid);
                    if (!proc.HasExited) active.Add((Path.GetFileName(dir), pid, proc.StartTime));
                }
                catch (ArgumentException)
                {
                }
            }
        }

        return active;
    }

    private static List<string> DetectDeadWorkers(string branchWorkspacePath)
    {
        var dead = new List<string>();
        if (!Directory.Exists(branchWorkspacePath)) return dead;

        foreach (var dir in Directory.GetDirectories(branchWorkspacePath).Where(d => Path.GetFileName(d) != "messages"))
        {
            var pidFile = Path.Combine(dir, ".host-process-id");
            if (File.Exists(pidFile) && int.TryParse(File.ReadAllText(pidFile), out var pid))
            {
                try
                {
                    if (Process.GetProcessById(pid).HasExited) dead.Add(Path.GetFileName(dir));
                }
                catch (ArgumentException)
                {
                    dead.Add(Path.GetFileName(dir));
                }
            }
        }

        return dead;
    }

    private static string PromptCleanWorkspace(List<(string AgentType, int Pid, DateTime StartTime)> active, List<string> dead)
    {
        AnsiConsole.MarkupLine("[yellow]⚠️ Existing session detected[/]\n");

        if (active.Count > 0)
        {
            AnsiConsole.MarkupLine("[green]Active:[/]");
            foreach (var (type, pid, start) in active)
            {
                var age = $"{(int)(DateTime.Now - start).TotalMinutes}m ago";
                AnsiConsole.MarkupLine($"  • {type} [dim](PID {pid}, {age})[/]");
            }
        }

        if (dead.Count > 0)
        {
            AnsiConsole.MarkupLine("\n[red]Dead:[/]");
            foreach (var type in dead)
            {
                AnsiConsole.MarkupLine($"  • {type}");
            }
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("\nWhat would you like to do?")
                .AddChoices("Continue existing session", "Clean restart (kill workers, wipe workspace)")
        );
    }

    private static void CleanBranchWorkspace(string branchPath, List<(string AgentType, int Pid, DateTime StartTime)> active)
    {
        // Kill active workers
        foreach (var (type, pid, _) in active)
        {
            try
            {
                KillProcess(Process.GetProcessById(pid));
                AnsiConsole.MarkupLine($"[yellow]Killed {type} (PID {pid})[/]");
            }
            catch
            {
                // Process might already be dead or inaccessible
            }
        }

        if (active.Count > 0) Thread.Sleep(TimeSpan.FromSeconds(1));

        // Delete branch workspace directory (contains logs, feedback-reports, and all agent workspaces)
        if (Directory.Exists(branchPath))
        {
            Directory.Delete(branchPath, true);
            AnsiConsole.MarkupLine($"[green]Deleted branch workspace: {Path.GetFileName(branchPath)}/[/]");
        }

        AnsiConsole.WriteLine();
    }

    private static (bool HasRecentActivity, TimeSpan IdleTime) CheckReviewerActivity(Workspace workspace)
    {
        var sessionIdleTime = GetSessionIdleTime(workspace);
        if (sessionIdleTime.HasValue)
        {
            var isActive = sessionIdleTime.Value < TimeSpan.FromMinutes(5);
            if (isActive) Logger.Debug($"Activity check - Reviewer session active ({sessionIdleTime.Value.TotalMinutes:F1} min ago)");
            return (isActive, sessionIdleTime.Value);
        }

        return (GitHelper.HasRecentGitActivity(TimeSpan.FromMinutes(5)), TimeSpan.Zero);
    }

    private static (bool HasRecentActivity, TimeSpan IdleTime) CheckEngineerActivity(Workspace workspace, DateTime lastGitChange)
    {
        var sessionIdleTime = GetSessionIdleTime(workspace);
        if (sessionIdleTime.HasValue)
        {
            if (CheckLastActionWasDelegation(workspace))
            {
                if (sessionIdleTime.Value < TimeSpan.FromMinutes(30))
                {
                    Logger.Debug($"Activity check - Engineer waiting for reviewer ({sessionIdleTime.Value.TotalMinutes:F1} min ago) - expected");
                    return (true, TimeSpan.Zero);
                }

                Logger.Debug($"Engineer waited {sessionIdleTime.Value.TotalMinutes:F1} min - likely deadlock");
                return (false, sessionIdleTime.Value);
            }

            var isActive = sessionIdleTime.Value < TimeSpan.FromMinutes(5);
            if (isActive) Logger.Debug($"Activity check - Engineer session active ({sessionIdleTime.Value.TotalMinutes:F1} min ago)");
            return (isActive, sessionIdleTime.Value);
        }

        return (GitHelper.HasRecentGitActivity(TimeSpan.FromMinutes(5)), DateTime.Now - lastGitChange);
    }

    private static (bool HasRecentActivity, TimeSpan IdleTime) CheckTechLeadActivity(Workspace workspace, DateTime lastGitChange)
    {
        var (hasActiveWorkers, crashedHosts) = CheckWorkerProcessStatus(workspace.Branch);

        if (crashedHosts.Count > 0)
        {
            var alert = $"\n⚠️⚠️⚠️ CRITICAL SYSTEM ERROR ⚠️⚠️⚠️\n\nWorker-host(s) crashed:\n{string.Join("\n", crashedHosts.Select(h => $"  - {h}"))}\n\nPlease manually restart:\n{string.Join("\n", crashedHosts.Select(h => $"  developer-cli agent {h}"))}\n";
            Logger.Error(alert);
            AnsiConsole.MarkupLine($"[red]{alert.Replace("[", "[[").Replace("]", "]]")}[/]");
            Thread.Sleep(TimeSpan.FromMinutes(5));
            return (true, TimeSpan.Zero);
        }

        if (hasActiveWorkers)
        {
            Logger.Debug("Tech-lead idle but workers active - expected");
            return (true, TimeSpan.Zero);
        }

        var sessionIdleTime = GetSessionIdleTime(workspace);
        if (sessionIdleTime.HasValue)
        {
            var isActive = sessionIdleTime.Value < TimeSpan.FromMinutes(10);
            if (!isActive) Logger.Debug($"Tech-lead idle ({sessionIdleTime.Value.TotalMinutes:F1} min) with no active workers - likely stuck");
            return (isActive, sessionIdleTime.Value);
        }

        return (GitHelper.HasRecentGitActivity(TimeSpan.FromMinutes(5)), DateTime.Now - lastGitChange);
    }

    private static (bool HasActiveWorkers, List<string> CrashedHosts) CheckWorkerProcessStatus(string branch)
    {
        var activeWorkers = false;
        var crashedHosts = new List<string>();

        foreach (var agentType in WorkerAgentTypes)
        {
            var workspace = new Workspace(agentType, branch);

            var hostAlive = IsProcessAlive(workspace.HostProcessIdFile);
            var workerAlive = IsProcessAlive(workspace.WorkerProcessIdFile);

            if (workerAlive) activeWorkers = true;
            if (File.Exists(workspace.HostProcessIdFile) && !hostAlive) crashedHosts.Add(agentType);
        }

        return (activeWorkers, crashedHosts);
    }

    private static bool IsProcessAlive(string processIdFile)
    {
        if (!File.Exists(processIdFile)) return false;
        if (!int.TryParse(File.ReadAllText(processIdFile), out var processId)) return false;
        try
        {
            return !Process.GetProcessById(processId).HasExited;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static bool CheckLastActionWasDelegation(Workspace workspace)
    {
        try
        {
            var lastLine = GetLastSessionLine(workspace);
            if (lastLine == null) return false;

            using var document = JsonDocument.Parse(lastLine);
            if (!document.RootElement.TryGetProperty("message", out var message)) return false;
            if (!message.TryGetProperty("content", out var content)) return false;

            foreach (var item in content.EnumerateArray())
            {
                if (item.TryGetProperty("name", out var name) && name.GetString() == "mcp__developer-cli__start_worker_agent")
                {
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private static TimeSpan? GetSessionIdleTime(Workspace workspace)
    {
        try
        {
            var lastLine = GetLastSessionLine(workspace);
            if (lastLine == null) return null;

            using var document = JsonDocument.Parse(lastLine);
            if (!document.RootElement.TryGetProperty("timestamp", out var timestamp)) return null;

            var lastActivity = DateTime.Parse(timestamp.GetString()!);
            return DateTime.Now - lastActivity;
        }
        catch
        {
            return null;
        }
    }

    private static string? GetLastSessionLine(Workspace workspace)
    {
        if (!File.Exists(workspace.SessionIdFile)) return null;

        var sessionId = File.ReadAllText(workspace.SessionIdFile).Trim();
        var claudeProjectsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".claude", "projects");

        if (!Directory.Exists(claudeProjectsDir)) return null;

        foreach (var projectDir in Directory.GetDirectories(claudeProjectsDir))
        {
            var sessionFile = Path.Combine(projectDir, $"{sessionId}.jsonl");
            if (File.Exists(sessionFile))
            {
                // ReadLines().LastOrDefault() reads entire file but is pragmatic for our use case (~10-50ms for 10-50MB files read every 5 minutes)
                try
                {
                    return File.ReadLines(sessionFile).LastOrDefault();
                }
                catch (IOException)
                {
                    return null;
                }
            }
        }

        return null;
    }
}

public record CurrentTaskInfo(
    string TaskNumber,
    string RequestFilePath,
    string StartedAt,
    int Attempt,
    string? FeatureId,
    string TaskId,
    string TaskTitle,
    string SenderAgentType
);

public record ProcessMonitoringOptions(
    TimeSpan InactivityTimeout,
    bool ExpectResponseFile,
    string? TaskNumber = null,
    string? ResponseFilePattern = null,
    string? MessagesDirectory = null
);

public record ProcessCompletionResult(bool Success, string Message, string? ResponseContent = null);

public class Workspace(string agentType, string? branch = null)
{
    // Define which agents are branch-agnostic
    private static readonly HashSet<string> BranchAgnosticAgents = ["pair-programmer", "tech-lead"];

    public string AgentType { get; } = agentType;

    private bool IsBranchAgnostic => BranchAgnosticAgents.Contains(AgentType);

    public string Branch { get; } = branch ?? GitHelper.GetCurrentBranch();

    public string BranchWorkspaceDirectory => IsBranchAgnostic
        ? Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces")
        : Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", Branch);

    public string AgentWorkspaceDirectory => IsBranchAgnostic
        ? Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", AgentType)
        : Path.Combine(BranchWorkspaceDirectory, AgentType);

    public string MessagesDirectory => Path.Combine(BranchWorkspaceDirectory, "messages");

    public string HostProcessIdFile => Path.Combine(AgentWorkspaceDirectory, ".host-process-id");

    public string WorkerProcessIdFile => Path.Combine(AgentWorkspaceDirectory, ".worker-process-id");

    public string CurrentTaskFile => Path.Combine(AgentWorkspaceDirectory, "current-task.json");

    public string TaskCounterFile => Path.Combine(MessagesDirectory, ".task-counter");

    public string SystemPromptFile => Path.Combine(Configuration.SourceCodeFolder, ".claude", "agentic-workflow", "system-prompts", $"{AgentType}.txt");

    public string SessionIdFile => Path.Combine(AgentWorkspaceDirectory, ".claude-session-id");
}
