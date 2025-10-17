using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ClaudeAgentCommand : Command
{
    private static bool _showAllActivities;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ClaudeAgentCommand() : base("claude-agent", "Interactive Worker Host for agent development")
    {
        var agentTypeArgument = new Argument<string?>("agent-type", () => null)
        {
            Description = "Agent type to run (tech-lead, backend-engineer, backend-reviewer, frontend-engineer, frontend-reviewer, test-automation-engineer, test-automation-reviewer)",
            Arity = ArgumentArity.ZeroOrOne
        };

        var mcpOption = new Option<bool>("--mcp", () => false, "Run in MCP mode (called from MCP server)");
        var taskTitleOption = new Option<string?>("--task-title", "Task title for MCP mode");
        var markdownContentOption = new Option<string?>("--markdown-content", "Task content in markdown format");
        var prdPathOption = new Option<string?>("--prd-path", "PRD file path (optional)");
        var productIncrementPathOption = new Option<string?>("--product-increment-path", "Product Increment file path (optional)");
        var taskNumberOption = new Option<string?>("--task-number", "Task number (optional)");

        AddArgument(agentTypeArgument);
        AddOption(mcpOption);
        AddOption(taskTitleOption);
        AddOption(markdownContentOption);
        AddOption(prdPathOption);
        AddOption(productIncrementPathOption);
        AddOption(taskNumberOption);

        this.SetHandler(async context =>
            {
                var agentType = context.ParseResult.GetValueForArgument(agentTypeArgument);
                var mcp = context.ParseResult.GetValueForOption(mcpOption);
                var taskTitle = context.ParseResult.GetValueForOption(taskTitleOption);
                var markdownContent = context.ParseResult.GetValueForOption(markdownContentOption);
                var prdPath = context.ParseResult.GetValueForOption(prdPathOption);
                var productIncrementPath = context.ParseResult.GetValueForOption(productIncrementPathOption);
                var taskNumber = context.ParseResult.GetValueForOption(taskNumberOption);

                await ExecuteAsync(agentType, mcp, taskTitle, markdownContent, prdPath, productIncrementPath, taskNumber);
            }
        );
    }

    // Entry Point
    private async Task ExecuteAsync(
        string? agentType,
        bool mcp,
        string? taskTitle,
        string? markdownContent,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber)
    {
        try
        {
            if (mcp)
            {
                // MCP mode - called from MCP server to delegate or spawn worker
                if (string.IsNullOrEmpty(agentType) || string.IsNullOrEmpty(taskTitle) || string.IsNullOrEmpty(markdownContent))
                {
                    throw new ArgumentException("--mcp mode requires agent-type, --task-title, and --markdown-content");
                }

                var result = await RunMcpMode(agentType, taskTitle, markdownContent, prdPath, productIncrementPath, taskNumber);

                // Output to stdout for MCP to capture (use plain WriteLine for clean output)
                await Console.Out.WriteLineAsync(result);
            }
            else
            {
                // Interactive mode
                await RunInteractiveMode(agentType);
            }
        }
        catch (Exception ex)
        {
            TryLogError("Command execution failed", ex);
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    // MCP Mode (called from MCP server to delegate or spawn worker)
    private async Task<string> RunMcpMode(
        string agentType,
        string taskTitle,
        string markdownContent,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber)
    {
        var workspace = new Workspace(agentType);

        // Check if interactive worker-host is already running
        if (File.Exists(workspace.HostProcessIdFile))
        {
            var pidContent = await File.ReadAllTextAsync(workspace.HostProcessIdFile);
            if (int.TryParse(pidContent, out var pid))
            {
                try
                {
                    var existingProcess = Process.GetProcessById(pid);
                    if (!existingProcess.HasExited)
                    {
                        // Interactive worker-host is running - delegate task to it
                        return await DelegateToInteractiveWorkerHost(workspace, taskTitle, markdownContent, prdPath, productIncrementPath, taskNumber);
                    }
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist, clean up stale PID file
                    File.Delete(workspace.HostProcessIdFile);
                }
            }
        }

        // No interactive worker-host - spawn temporary automated worker-host
        try
        {
            // Create directories first
            Directory.CreateDirectory(workspace.AgentWorkspaceDirectory);
            Directory.CreateDirectory(workspace.MessagesDirectory);

            // Create .host-process-id with this automated worker-host's PID
            await File.WriteAllTextAsync(workspace.HostProcessIdFile, Process.GetCurrentProcess().Id.ToString());

            // Get next task counter
            var counter = await GetNextTaskCounter(workspace);

            // Create request file
            var requestFileName = CreateRequestFileName(counter, workspace.AgentType, taskTitle);
            var requestFile = Path.Combine(workspace.MessagesDirectory, requestFileName);
            await File.WriteAllTextAsync(requestFile, markdownContent);

            await SetupAgentWorkspace(workspace.AgentWorkspaceDirectory);

            // Save task metadata with full paths
            var currentTaskInfo = CreateTaskMetadata(counter, requestFile, taskTitle, prdPath, productIncrementPath, taskNumber);
            await WriteTaskMetadata(workspace, currentTaskInfo);

            // Launch worker-agent (Claude Code) in agent workspace
            var process = await LaunchWorker(workspace, taskTitle);

            // Create .worker-process-id with worker-agent's process ID
            await File.WriteAllTextAsync(workspace.WorkerProcessIdFile, process.Id.ToString());

            ClaudeAgentLifecycle.LogWorkflowEvent($"[{counter:D4}.{workspace.AgentType}.request] Started: '{taskTitle}' -> [{requestFileName}]");

            // Monitor process with unified timeout/restart logic
            var responseFilePattern = $"{counter:D4}.{workspace.AgentType}.response.*.md";
            var options = new ProcessMonitoringOptions(
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(115),
                true,
                taskTitle,
                requestFileName,
                $"{counter:D4}",
                responseFilePattern,
                workspace.MessagesDirectory
            );

            var result = await MonitorProcessWithTimeout(process, workspace.AgentType, options);

            if (result.Success)
            {
                return $"Task completed successfully by {workspace.AgentType}.\n" +
                       $"Task number: {counter:D4}\n" +
                       $"Task title: {taskTitle}\n" +
                       $"Request file: {requestFileName}\n" +
                       $"Restarts needed: {result.RestartCount}\n\n" +
                       $"Response content:\n{result.ResponseContent}";
            }
            else
            {
                return result.Message;
            }
        }
        finally
        {
            // Clean up PID files
            if (File.Exists(workspace.HostProcessIdFile))
            {
                File.Delete(workspace.HostProcessIdFile);
            }

            if (File.Exists(workspace.WorkerProcessIdFile))
            {
                File.Delete(workspace.WorkerProcessIdFile);
            }
        }
    }

    private async Task<string> DelegateToInteractiveWorkerHost(
        Workspace workspace,
        string taskTitle,
        string markdownContent,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber)
    {
        // Get next task counter
        var taskCounter = await GetNextTaskCounter(workspace);

        // Create request file
        var taskRequestFileName = CreateRequestFileName(taskCounter, workspace.AgentType, taskTitle);
        var taskRequestFilePath = Path.Combine(workspace.MessagesDirectory, taskRequestFileName);
        await File.WriteAllTextAsync(taskRequestFilePath, markdownContent);

        // Save task metadata with full paths
        var taskInfo = CreateTaskMetadata(taskCounter, taskRequestFilePath, taskTitle, prdPath, productIncrementPath, taskNumber);
        await WriteTaskMetadata(workspace, taskInfo);

        ClaudeAgentLifecycle.LogWorkflowEvent($"[{taskCounter:D4}.{workspace.AgentType}.request] Started: '{taskTitle}' -> [{taskRequestFileName}]");

        // Wait for response file (interactive agent will process it)
        var startTime = DateTime.Now;
        var overallTimeout = TimeSpan.FromHours(2);
        string? foundResponseFile;
        var responseFilePattern = $"{taskCounter:D4}.{workspace.AgentType}.response.*.md";

        while (true)
        {
            if (DateTime.Now - startTime > overallTimeout)
            {
                throw new TimeoutException($"Interactive {workspace.AgentType} exceeded 2-hour overall timeout");
            }

            var matchingFiles = Directory.GetFiles(workspace.MessagesDirectory, responseFilePattern);
            if (matchingFiles.Length > 0)
            {
                foundResponseFile = matchingFiles[0];
                break;
            }

            // Polling delay to reduce filesystem query frequency while waiting for interactive worker to create response file
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        // Read and return response
        var responseContent = await File.ReadAllTextAsync(foundResponseFile);
        var actualResponseFileName = Path.GetFileName(foundResponseFile);

        ClaudeAgentLifecycle.LogWorkflowEvent($"[{taskCounter:D4}.{workspace.AgentType}.response] Completed: '{taskTitle}' -> [{actualResponseFileName}]");

        return $"Task delegated successfully to {workspace.AgentType}.\n" +
               $"Task number: {taskCounter:D4}\n" +
               $"Request file: {taskRequestFileName}\n" +
               $"Response file: {actualResponseFileName}\n\n" +
               $"Response content:\n{responseContent}";
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

        var workspace = new Workspace(agentType);

        // Create workspace and register agent
        Directory.CreateDirectory(workspace.AgentWorkspaceDirectory);

        // Setup workspace with symlink to .claude directory
        await SetupAgentWorkspace(workspace.AgentWorkspaceDirectory);

        // Check for existing worker-host process
        if (File.Exists(workspace.HostProcessIdFile))
        {
            var existingPid = await File.ReadAllTextAsync(workspace.HostProcessIdFile);
            if (int.TryParse(existingPid, out var pid))
            {
                try
                {
                    var existingProcess = Process.GetProcessById(pid);
                    if (!existingProcess.HasExited)
                    {
                        // Active worker-host is running - calculate how long it's been alive
                        var processAge = DateTime.Now - existingProcess.StartTime;
                        var ageString = processAge.TotalMinutes < 1
                            ? $"{(int)processAge.TotalSeconds} seconds ago"
                            : processAge.TotalHours < 1
                                ? $"{(int)processAge.TotalMinutes} minutes {(int)(processAge.TotalSeconds % 60)} seconds ago"
                                : $"{(int)processAge.TotalHours} hours {processAge.Minutes} minutes ago";

                        AnsiConsole.MarkupLine($"[yellow]⚠ Another {agentType} worker-host is currently running (PID: {pid}, Started: {ageString})[/]");

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

        // Create .host-process-id so MCP can detect this interactive worker-host
        await File.WriteAllTextAsync(workspace.HostProcessIdFile, Process.GetCurrentProcess().Id.ToString());

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
        var displayName = GetAgentDisplayName(agentType);

        // Set terminal title
        SetTerminalTitle($"{displayName} - {workspace.Branch}");

        // Load small Figlet font for compact banner
        var smallFontPath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli", "Fonts", "small.flf");
        var font = File.Exists(smallFontPath) ? FigletFont.Load(smallFontPath) : FigletFont.Default;
        var agentBanner = new FigletText(font, displayName).Color(GetAgentColor(agentType));
        AnsiConsole.Write(agentBanner);

        var agentColor = GetAgentColor(agentType);

        AnsiConsole.WriteLine(); // Extra line for spacing

        // Check for task recovery - if current-task.json exists, prompt user
        if (File.Exists(workspace.CurrentTaskFile))
        {
            var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
            var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);

            if (taskInfo is not null)
            {
                // Show incomplete task prompt
                AnsiConsole.MarkupLine($"[{agentColor} bold]⚠️ INCOMPLETE TASK DETECTED[/]");
                AnsiConsole.MarkupLine($"[dim]Task {taskInfo.TaskNumber} - '{Markup.Escape(taskInfo.Title)}' is currently in development.[/]");
                AnsiConsole.WriteLine();

                var wantsToContinue = AnsiConsole.Confirm("Do you want to continue this task?");

                AnsiConsole.MarkupLine($"[{agentColor}]Resuming session...[/]");
                // Brief pause to allow user to read the resuming message before launching Claude Code
                await Task.Delay(TimeSpan.FromSeconds(1));

                // Launch manual session (with or without slash command based on user choice)
                await LaunchManualClaudeSession(workspace, wantsToContinue ? taskInfo.Title : null);
                return; // Exit after session ends
            }
        }

        // Tech-lead launches immediately, other agents wait for requests
        if (agentType == "tech-lead")
        {
            // Check if session exists - if yes, use --continue only, otherwise use slash command
            var sessionIdFile = Path.Combine(workspace.AgentWorkspaceDirectory, ".claude-session-id");
            var useSlashCommand = !File.Exists(sessionIdFile);

            await LaunchManualClaudeSession(workspace, useSlashCommand: useSlashCommand);
            AnsiConsole.MarkupLine($"[{agentColor} bold]✓ Tech Lead session ended[/]");
        }
        else
        {
            // Display initial waiting screen with recent activity
            RedrawWaitingDisplay(agentType, workspace.Branch);

            await WaitForTasksOrManualControl(workspace);
        }
    }

    // Request Watching & Handling
    private async Task WaitForTasksOrManualControl(Workspace workspace)
    {
        // Create messages directory if it doesn't exist (needed for FileSystemWatcher)
        Directory.CreateDirectory(workspace.MessagesDirectory);

        using var fileSystemWatcher = new FileSystemWatcher(workspace.MessagesDirectory, $"*.{workspace.AgentType}.request.*.md");
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
            var userPressedEnter = await WaitInStandbyMode(workspace.AgentType, workspace.Branch, () => requestReceived);

            if (requestReceived && requestFilePath != null)
            {
                // Request file arrived - handle it
                requestReceived = false;
                await HandleIncomingRequest(requestFilePath, workspace);
                requestFilePath = null;
            }
            else if (userPressedEnter)
            {
                // User pressed ENTER - launch manual session and return to waiting
                await LaunchManualClaudeSession(workspace, useSlashCommand: false);
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

            // Prevent tight polling loop from consuming excessive CPU while checking for keyboard input and request files
            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }
    }

    private async Task HandleIncomingRequest(string requestFile, Workspace workspace)
    {
        var agentColor = GetAgentColor(workspace.AgentType);

        // Clear the waiting display
        AnsiConsole.Clear();

        // Show task received animation
        AnsiConsole.MarkupLine($"[{agentColor} bold]▶ TASK RECEIVED[/]");
        AnsiConsole.MarkupLine($"[dim]Request: {Path.GetFileName(requestFile)}[/]");

        // Allow filesystem time to complete file write operation before reading to avoid partial content
        await Task.Delay(TimeSpan.FromMilliseconds(500));
        var taskContent = await File.ReadAllTextAsync(requestFile);
        var firstLine = taskContent.Split('\n').FirstOrDefault()?.Trim() ?? "Task";

        AnsiConsole.MarkupLine($"[dim]Task: {Markup.Escape(firstLine)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Launching Claude Code in 3 seconds...[/]");
        // Countdown delay to give user time to see task details before Claude Code UI takes over
        await Task.Delay(TimeSpan.FromSeconds(3));

        // Launch worker-agent (Claude Code) - task title read from current-task.json
        var claudeProcess = await LaunchWorker(workspace);

        // Create .worker-process-id with worker-agent's process ID (so CompleteAndExitTask can kill it)
        await File.WriteAllTextAsync(workspace.WorkerProcessIdFile, claudeProcess.Id.ToString());

        // Extract task number from request file
        var requestFileName = Path.GetFileName(requestFile);
        var match = Regex.Match(requestFileName, @"^(\d+)\.([^.]+)\.request\.(.+)\.md$");
        var taskNumber = match.Groups[1].Value;
        var responseFilePattern = $"{taskNumber}.{workspace.AgentType}.response.*.md";

        // Monitor process with unified timeout/restart logic
        var options = new ProcessMonitoringOptions(
            TimeSpan.FromMinutes(20),
            TimeSpan.FromMinutes(115),
            true,
            firstLine,
            requestFileName,
            taskNumber,
            responseFilePattern,
            workspace.MessagesDirectory
        );

        var result = await MonitorProcessWithTimeout(claudeProcess, workspace.AgentType, options);

        // Clean up .worker-process-id after worker exits
        if (File.Exists(workspace.WorkerProcessIdFile))
        {
            File.Delete(workspace.WorkerProcessIdFile);
        }

        // Show completion status
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[{agentColor} bold]✓ {result.Message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red bold]✗ {result.Message}[/]");
        }

        // Return to waiting display
        RedrawWaitingDisplay(workspace.AgentType, workspace.Branch);
    }

    private async Task LaunchManualClaudeSession(Workspace workspace, string? taskTitleForSlashCommand = null, bool useSlashCommand = true)
    {
        // Launch worker (slash command usage controlled by useSlashCommand parameter)
        var process = await LaunchWorker(workspace, taskTitleForSlashCommand, useSlashCommand: useSlashCommand);

        // Create .worker-process-id
        await File.WriteAllTextAsync(workspace.WorkerProcessIdFile, process.Id.ToString());

        // Monitor process with unified timeout/restart logic
        // Tech-lead gets 62 minutes (allows 3 worker restarts @ 20 min each)
        // Workers get 20 minutes
        var inactivityTimeout = workspace.AgentType == "tech-lead"
            ? TimeSpan.FromMinutes(62)
            : TimeSpan.FromMinutes(20);

        var options = new ProcessMonitoringOptions(
            inactivityTimeout,
            TimeSpan.FromMinutes(115),
            false, // Manual sessions don't expect response files
            workspace.AgentType == "tech-lead" ? "Tech Lead Session" : "Manual Session",
            "" // Not applicable for manual sessions
        );

        var result = await MonitorProcessWithTimeout(process, workspace.AgentType, options);

        // Show completion status
        var agentColor = GetAgentColor(workspace.AgentType);
        if (result.Success)
        {
            AnsiConsole.MarkupLine($"[{agentColor} bold]✓ {result.Message}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red bold]✗ {result.Message}[/]");
        }

        // Clean up
        if (File.Exists(workspace.WorkerProcessIdFile))
        {
            File.Delete(workspace.WorkerProcessIdFile);
        }
    }

    private async Task<Process> LaunchWorker(
        Workspace workspace,
        string? taskTitle = null,
        bool isRestart = false,
        bool useSlashCommand = true)
    {
        await SetupAgentWorkspace(workspace.AgentWorkspaceDirectory);

        // Load system prompt (REQUIRED - throw if missing)
        if (!File.Exists(workspace.SystemPromptFile))
        {
            throw new FileNotFoundException($"System prompt file not found: {workspace.SystemPromptFile}");
        }

        var systemPromptText = await File.ReadAllTextAsync(workspace.SystemPromptFile);
        systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();

        // Build standard arguments
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions",
            "--append-system-prompt", systemPromptText
        };

        // Add restart nudge if this is a restart
        if (isRestart)
        {
            claudeArgs.Add("--append-system-prompt");
            claudeArgs.Add("You were restarted because you appeared stuck. Please re-read current-task.json and continue working.");
        }

        // Add slash command only if requested (manual sessions use --continue only)
        if (useSlashCommand)
        {
            // Determine task title
            var effectiveTaskTitle = taskTitle;
            if (effectiveTaskTitle == null)
            {
                // Read from current-task.json
                effectiveTaskTitle = "task";
                if (File.Exists(workspace.CurrentTaskFile))
                {
                    var taskJson = await File.ReadAllTextAsync(workspace.CurrentTaskFile);
                    var taskInfo = JsonSerializer.Deserialize<CurrentTaskInfo>(taskJson, JsonOptions);
                    if (taskInfo is not null)
                    {
                        effectiveTaskTitle = taskInfo.Title;
                    }
                }
            }

            // Add slash command to trigger workflow
            var slashCommand = workspace.AgentType switch
            {
                "tech-lead" => "/orchestrate:tech-lead",
                "test-automation-engineer" => $"/implement:e2e-tests {effectiveTaskTitle}",
                "test-automation-reviewer" => $"/review:e2e-tests {effectiveTaskTitle}",
                _ => workspace.AgentType.Contains("reviewer")
                    ? $"/review:task {effectiveTaskTitle}"
                    : $"/implement:task {effectiveTaskTitle}"
            };
            claudeArgs.Add(slashCommand);
        }

        // DEBUG: Log the exact command being executed
        var commandLine = $"claude {string.Join(" ", claudeArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg))}";
        var mode = isRestart ? "RESTART" : "LAUNCH";
        Logger.Debug($"{mode} - Starting Claude Code");
        Logger.Debug($"Agent Type: {workspace.AgentType}");
        Logger.Debug($"Working Directory: {workspace.AgentWorkspaceDirectory}");
        Logger.Debug($"Command: {commandLine}");

        // Launch with session management
        var process = await LaunchClaudeCode(workspace.AgentWorkspaceDirectory, claudeArgs);

        Logger.Debug($"Process started with ID: {process.Id}");
        // Verification delay to confirm process launched successfully before returning (longer for initial launch to allow Claude Code startup)
        await Task.Delay(TimeSpan.FromSeconds(isRestart ? 2 : 3));
        Logger.Debug($"Process alive after delay: {!process.HasExited}");

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
        var shortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))
            .ToLowerInvariant().Replace(".", "").Replace(",", "");
        return $"{taskCounter:D4}.{agentType}.request.{shortTitle}.md";
    }

    private static CurrentTaskInfo CreateTaskMetadata(
        int taskCounter,
        string requestFilePath,
        string taskTitle,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber)
    {
        return new CurrentTaskInfo(
            $"{taskCounter:D4}",
            requestFilePath,
            DateTime.UtcNow.ToString("O"),
            1,
            taskTitle,
            prdPath,
            productIncrementPath,
            taskNumber
        );
    }

    private async Task WriteTaskMetadata(Workspace workspace, CurrentTaskInfo taskInfo)
    {
        await File.WriteAllTextAsync(workspace.CurrentTaskFile, JsonSerializer.Serialize(taskInfo, JsonOptions));
    }

    // Setup & Utilities
    internal async Task SetupAgentWorkspace(string agentWorkspaceDirectory)
    {
        // Note: agentWorkspaceDirectory already created by caller

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
                                    "developer-cli": {
                                      "command": "dotnet",
                                      "args": ["run", "--project", "../../../../developer-cli", "mcp"]
                                    }
                                  }
                                }
                                """;

            await File.WriteAllTextAsync(workerMcpJsonPath, mcpConfigJson);
        }
    }

    internal bool HasGitChanges()
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

    internal async Task<Process> LaunchClaudeCode(string agentWorkspaceDirectory, List<string> additionalArgs, string? workingDirectory = null)
    {
        workingDirectory ??= agentWorkspaceDirectory;
        var sessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");

        Logger.Debug($"LaunchClaudeCode - Session file exists: {File.Exists(sessionIdFile)}");

        // Try --continue if session marker exists
        if (File.Exists(sessionIdFile))
        {
            Logger.Debug("Attempting --continue (session marker exists)");
            var argsWithContinue = new List<string> { "--continue" };
            argsWithContinue.AddRange(additionalArgs);

            var process = new Process
            {
                StartInfo = BuildProcessStartInfo(argsWithContinue, workingDirectory)
            };

            Logger.Debug($"Starting with --continue, working directory: {workingDirectory}");
            process.Start();

            // Verification delay to determine if --continue succeeded or if Claude Code exited immediately due to no conversation to continue
            await Task.Delay(TimeSpan.FromSeconds(2));

            // If still running, --continue succeeded
            if (!process.HasExited)
            {
                Logger.Debug($"--continue succeeded, process ID: {process.Id}");
                return process; // Return LIVE process
            }

            // --continue failed (no conversation to continue), delete marker and start fresh
            Logger.Debug($"--continue failed (process exited with code {process.ExitCode}), deleting session marker");
            File.Delete(sessionIdFile);
        }

        // Fresh start (no session marker or --continue failed)
        Logger.Debug("Starting fresh session (no session marker or --continue failed)");
        // Create session marker BEFORE starting so any exit (crash, kill, normal) leaves the file
        await File.WriteAllTextAsync(sessionIdFile, Guid.NewGuid().ToString());

        var freshArgs = new List<string>();
        freshArgs.AddRange(additionalArgs);

        var commandLine = string.Join(" ", freshArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
        Logger.Debug($"Starting fresh process - UseShellExecute: true, Working directory: {workingDirectory}");
        Logger.Debug($"Command: claude {commandLine}");

        var freshProcess = new Process
        {
            StartInfo = BuildProcessStartInfo(freshArgs, workingDirectory)
        };

        freshProcess.Start();
        Logger.Debug($"Fresh process started with ID: {freshProcess.Id}");

        return freshProcess;
    }

    // Display & UI
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

    private List<string> GetRecentActivity(string agentType, string branch)
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

    private string GetAgentDisplayName(string agentType)
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

    private Color GetAgentColor(string agentType)
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

    private void SetTerminalTitle(string title)
    {
        // ANSI escape sequence to set terminal title
        // Works in most modern terminals (iTerm2, Terminal.app, Windows Terminal, etc.)
        Console.Write($"\x1b]0;{title}\x07");
    }

    // Worker Completion & Restart Logic

    private async Task<(bool Success, int ProcessId, Process? Process, string ErrorMessage)> RestartWorker(string agentType)
    {
        var workspace = new Workspace(agentType);

        // Launch worker with restart flag (adds restart nudge and reads task title from current-task.json)
        var process = await LaunchWorker(workspace, isRestart: true);

        if (process.HasExited)
        {
            return (false, -1, null, $"Process exited immediately with code: {process.ExitCode}");
        }

        return (true, process.Id, process, "");
    }

    private void UpdateWorkerSession(int oldProcessId, int newProcessId, string agentType, string taskTitle, string requestFileName, Process newProcess)
    {
        var workspace = new Workspace(agentType);
        File.WriteAllText(workspace.WorkerProcessIdFile, newProcessId.ToString());

        ClaudeAgentLifecycle.RemoveWorkerSession(oldProcessId);
        ClaudeAgentLifecycle.AddWorkerSession(newProcessId, agentType, taskTitle, requestFileName, newProcess);
    }

    private async Task<ProcessCompletionResult> MonitorProcessWithTimeout(
        Process process,
        string agentType,
        ProcessMonitoringOptions options)
    {
        var startTime = DateTime.Now;
        var currentProcess = process;
        var currentProcessId = process.Id;
        var restartCount = 0;

        // Track in active sessions (for workers only, not tech-lead)
        if (options.ExpectResponseFile)
        {
            ClaudeAgentLifecycle.AddWorkerSession(currentProcessId, agentType, options.TaskTitle, options.RequestFileName, currentProcess);
        }

        try
        {
            while (DateTime.Now - startTime < options.OverallTimeout)
            {
                // Block until process exits OR inactivity timeout (no polling!)
                var exited = currentProcess.WaitForExit(options.InactivityTimeout);

                if (exited)
                {
                    // Process completed normally
                    Logger.Debug($"Process {currentProcessId} exited normally");

                    if (options.ExpectResponseFile)
                    {
                        // Allow MCP tool time to complete writing response file after process exit
                        await Task.Delay(TimeSpan.FromMilliseconds(500));

                        // Check for response file
                        var matchingFiles = Directory.GetFiles(options.MessagesDirectory!, options.ResponseFilePattern!);
                        if (matchingFiles.Length == 0)
                        {
                            return new ProcessCompletionResult(
                                false,
                                $"Worker exited but no response file found matching: {options.ResponseFilePattern}"
                            );
                        }

                        var responseFilePath = matchingFiles[0];
                        var responseFileName = Path.GetFileName(responseFilePath);
                        var responseContent = await File.ReadAllTextAsync(responseFilePath);

                        ClaudeAgentLifecycle.LogWorkflowEvent($"[{options.TaskNumber}.{agentType}.response] Completed: '{responseFileName}' (restarts: {restartCount})");

                        return new ProcessCompletionResult(
                            true,
                            $"Task completed successfully (restarts: {restartCount})",
                            responseContent,
                            restartCount
                        );
                    }

                    // Tech-lead - no response file expected
                    return new ProcessCompletionResult(true, "Session completed", null, restartCount);
                }

                // Inactivity timeout reached - check if worker is making progress
                Logger.Debug($"Inactivity timeout ({options.InactivityTimeout.TotalMinutes} min) reached for {agentType} process {currentProcessId}");

                var hasGitChanges = HasGitChanges();
                if (hasGitChanges)
                {
                    Logger.Debug("Git changes detected, worker is active - continuing");
                    continue;
                }

                // No git changes - worker is stuck, restart it
                Logger.Debug("No git changes detected, restarting worker");

                if (!currentProcess.HasExited)
                {
                    KillProcess(currentProcess);
                }

                // Restart worker
                var restartResult = await RestartWorker(agentType);
                if (!restartResult.Success || restartResult.Process == null)
                {
                    return new ProcessCompletionResult(false, $"Worker restart failed: {restartResult.ErrorMessage}");
                }

                // Update tracking
                if (options.ExpectResponseFile)
                {
                    UpdateWorkerSession(currentProcessId, restartResult.ProcessId, agentType, options.TaskTitle, options.RequestFileName, restartResult.Process);
                }

                currentProcess = restartResult.Process;
                currentProcessId = restartResult.ProcessId;
                restartCount++;

                LogWorkerActivity($"WORKER RESTART: {agentType} inactive for {options.InactivityTimeout.TotalMinutes} minutes (no git changes), restarted (attempt {restartCount})");
            }

            // Overall timeout reached
            Logger.Debug($"Overall timeout ({options.OverallTimeout.TotalMinutes} minutes) reached for {agentType}");

            if (!currentProcess.HasExited)
            {
                KillProcess(currentProcess);
            }

            return new ProcessCompletionResult(
                false,
                $"Worker timeout after {options.OverallTimeout.TotalMinutes} minutes (restarts: {restartCount})"
            );
        }
        finally
        {
            if (options.ExpectResponseFile)
            {
                ClaudeAgentLifecycle.RemoveWorkerSession(currentProcessId);
            }
        }
    }

    private void LogWorkerActivity(string message)
    {
        Logger.Debug(message);
    }

    private static void TryLogError(string message, Exception? ex = null)
    {
        try
        {
            if (ex is not null)
            {
                Logger.Error($"{message}: {ex.Message}");
            }
            else
            {
                Logger.Error(message);
            }
        }
        catch
        {
            // Best effort logging - if logging fails, silently continue
        }
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
        var commandLine = string.Join(" ", claudeArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));

        return new ProcessStartInfo
        {
            FileName = "claude",
            Arguments = commandLine,
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };
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

public record CurrentTaskInfo(
    string TaskNumber,
    string RequestFilePath,
    string StartedAt,
    int Attempt,
    string Title,
    string? PrdPath,
    string? ProductIncrementPath,
    string? TaskNumberInIncrement
);

public record ProcessMonitoringOptions(
    TimeSpan InactivityTimeout,
    TimeSpan OverallTimeout,
    bool ExpectResponseFile,
    string TaskTitle,
    string RequestFileName,
    string? TaskNumber = null,
    string? ResponseFilePattern = null,
    string? MessagesDirectory = null
);

public record ProcessCompletionResult(
    bool Success,
    string Message,
    string? ResponseContent = null,
    int RestartCount = 0
);

public class Workspace(string agentType, string? branch = null)
{
    public string AgentType { get; } = agentType;

    public string Branch { get; } = branch ?? GitHelper.GetCurrentBranch();

    public string BranchWorkspaceDirectory => Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", Branch);

    public string AgentWorkspaceDirectory => Path.Combine(BranchWorkspaceDirectory, AgentType);

    public string MessagesDirectory => Path.Combine(BranchWorkspaceDirectory, "messages");

    public string HostProcessIdFile => Path.Combine(AgentWorkspaceDirectory, ".host-process-id");

    public string WorkerProcessIdFile => Path.Combine(AgentWorkspaceDirectory, ".worker-process-id");

    public string CurrentTaskFile => Path.Combine(AgentWorkspaceDirectory, "current-task.json");

    public string TaskCounterFile => Path.Combine(MessagesDirectory, ".task-counter");

    public string SystemPromptFile => Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{AgentType}.txt");
}
