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

        var mcpOption = new Option<bool>("--mcp", () => false, "Run in MCP mode (called from MCP server)");
        var taskTitleOption = new Option<string?>("--task-title", "Task title for MCP mode");
        var markdownContentOption = new Option<string?>("--markdown-content", "Task content in markdown format");
        var prdPathOption = new Option<string?>("--prd-path", "PRD file path (optional)");
        var productIncrementPathOption = new Option<string?>("--product-increment-path", "Product Increment file path (optional)");
        var taskNumberOption = new Option<string?>("--task-number", "Task number (optional)");
        var requestFilePathOption = new Option<string?>("--request-file-path", "Engineer's request file path (optional)");
        var responseFilePathOption = new Option<string?>("--response-file-path", "Engineer's response file path (optional)");

        AddArgument(agentTypeArgument);
        AddOption(mcpOption);
        AddOption(taskTitleOption);
        AddOption(markdownContentOption);
        AddOption(prdPathOption);
        AddOption(productIncrementPathOption);
        AddOption(taskNumberOption);
        AddOption(requestFilePathOption);
        AddOption(responseFilePathOption);

        this.SetHandler(async (context) =>
        {
            var agentType = context.ParseResult.GetValueForArgument(agentTypeArgument);
            var mcp = context.ParseResult.GetValueForOption(mcpOption);
            var taskTitle = context.ParseResult.GetValueForOption(taskTitleOption);
            var markdownContent = context.ParseResult.GetValueForOption(markdownContentOption);
            var prdPath = context.ParseResult.GetValueForOption(prdPathOption);
            var productIncrementPath = context.ParseResult.GetValueForOption(productIncrementPathOption);
            var taskNumber = context.ParseResult.GetValueForOption(taskNumberOption);
            var requestFilePath = context.ParseResult.GetValueForOption(requestFilePathOption);
            var responseFilePath = context.ParseResult.GetValueForOption(responseFilePathOption);

            await ExecuteAsync(agentType, mcp, taskTitle, markdownContent, prdPath, productIncrementPath, taskNumber, requestFilePath, responseFilePath);
        });
    }

    // Entry Point
    private static async Task ExecuteAsync(
        string? agentType,
        bool mcp,
        string? taskTitle,
        string? markdownContent,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber,
        string? requestFilePath,
        string? responseFilePath)
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

                var result = await RunMcpMode(agentType, taskTitle, markdownContent, prdPath, productIncrementPath, taskNumber, requestFilePath, responseFilePath);

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
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    // MCP Mode (called from MCP server to delegate or spawn worker)
    private static async Task<string> RunMcpMode(
        string agentType,
        string taskTitle,
        string markdownContent,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber,
        string? requestFilePath,
        string? responseFilePath)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var branchWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName);
        var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDirectory, agentType);
        var messagesDirectory = Path.Combine(branchWorkspaceDirectory, "messages");
        var hostProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".host-process-id");

        // Check if interactive worker-host is already running
        if (File.Exists(hostProcessIdFile))
        {
            var pidContent = await File.ReadAllTextAsync(hostProcessIdFile);
            if (int.TryParse(pidContent, out var pid))
            {
                try
                {
                    var existingProcess = Process.GetProcessById(pid);
                    if (!existingProcess.HasExited)
                    {
                        // Interactive worker-host is running - delegate task to it
                        return await DelegateToInteractiveWorkerHost(agentType, taskTitle, markdownContent, agentWorkspaceDirectory, messagesDirectory);
                    }
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist, clean up stale PID file
                    File.Delete(hostProcessIdFile);
                }
            }
        }

        // No interactive worker-host - spawn temporary automated worker-host
        return await SpawnAutomatedWorkerHost(agentType, taskTitle, markdownContent, prdPath, productIncrementPath, taskNumber, requestFilePath, responseFilePath);
    }

    private static async Task<string> DelegateToInteractiveWorkerHost(
        string agentType,
        string taskTitle,
        string markdownContent,
        string agentWorkspaceDirectory,
        string messagesDirectory)
    {

        // Get next task counter
        var taskCounterFile = Path.Combine(messagesDirectory, ".task-counter");
        var taskCounter = 1;
        if (File.Exists(taskCounterFile) && int.TryParse(await File.ReadAllTextAsync(taskCounterFile), out var existingCounter))
        {
            taskCounter = existingCounter + 1;
        }
        await File.WriteAllTextAsync(taskCounterFile, taskCounter.ToString());

        // Create request file
        var taskShortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))
            .ToLowerInvariant().Replace(".", "").Replace(",", "");
        var taskRequestFileName = $"{taskCounter:D4}.{agentType}.request.{taskShortTitle}.md";
        var taskRequestFilePath = Path.Combine(messagesDirectory, taskRequestFileName);

        await File.WriteAllTextAsync(taskRequestFilePath, markdownContent);

        // Save task metadata with full paths
        var taskInfo = new
        {
            task_number = $"{taskCounter:D4}",
            request_file_path = taskRequestFilePath,  // Full absolute path
            started_at = DateTime.UtcNow.ToString("O"),
            attempt = 1,
            branch = GitHelper.GetCurrentBranch(),
            title = taskTitle
        };

        var taskFile = Path.Combine(agentWorkspaceDirectory, "current-task.json");
        await File.WriteAllTextAsync(taskFile, JsonSerializer.Serialize(taskInfo, new JsonSerializerOptions { WriteIndented = true }));

        // Create .task-id for recovery
        var taskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
        await File.WriteAllTextAsync(taskIdFile, $"{taskCounter:D4}");

        LogWorkflowEvent($"[{taskCounter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{taskRequestFileName}]", messagesDirectory);

        // Wait for response file (interactive agent will process it)
        var startTime = DateTime.Now;
        var overallTimeout = TimeSpan.FromHours(2);
        string? foundResponseFile = null;
        var responseFilePattern = $"{taskCounter:D4}.{agentType}.response.*.md";

        while (foundResponseFile == null)
        {
            if (DateTime.Now - startTime > overallTimeout)
            {
                throw new TimeoutException($"Interactive {agentType} exceeded 2-hour overall timeout");
            }

            var matchingFiles = Directory.GetFiles(messagesDirectory, responseFilePattern);
            if (matchingFiles.Length > 0)
            {
                foundResponseFile = matchingFiles[0];
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        // Clean up task ID file
        if (File.Exists(taskIdFile))
        {
            File.Delete(taskIdFile);
        }

        // Read and return response
        var responseContent = await File.ReadAllTextAsync(foundResponseFile);
        var actualResponseFileName = Path.GetFileName(foundResponseFile);

        LogWorkflowEvent($"[{taskCounter:D4}.{agentType}.response] Completed: '{taskTitle}' -> [{actualResponseFileName}]", messagesDirectory);

        return $"Task delegated successfully to {agentType}.\n" +
               $"Task number: {taskCounter:D4}\n" +
               $"Request file: {taskRequestFileName}\n" +
               $"Response file: {actualResponseFileName}\n\n" +
               $"Response content:\n{responseContent}";
    }

    private static async Task<string> SpawnAutomatedWorkerHost(
        string agentType,
        string taskTitle,
        string markdownContent,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber,
        string? requestFilePath,
        string? responseFilePath)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var branchWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName);
        var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDirectory, agentType);
        var messagesDirectory = Path.Combine(branchWorkspaceDirectory, "messages");
        var hostProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".host-process-id");
        var workerProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".worker-process-id");

        // Acquire workspace lock
        var mutexName = $"{agentType}-{branchName}";
        var workspaceMutex = new Mutex(false, mutexName);

        if (!workspaceMutex.WaitOne(TimeSpan.FromSeconds(5)))
        {
            workspaceMutex.Dispose();
            throw new InvalidOperationException($"Another {agentType} is already active in branch '{branchName}'");
        }

        try
        {
            // Create directories first
            Directory.CreateDirectory(agentWorkspaceDirectory);
            Directory.CreateDirectory(messagesDirectory);

            // Create .host-process-id with this automated worker-host's PID
            await File.WriteAllTextAsync(hostProcessIdFile, Process.GetCurrentProcess().Id.ToString());

            // Get next task counter
            var counterFile = Path.Combine(messagesDirectory, ".task-counter");
            var counter = 1;
            if (File.Exists(counterFile) && int.TryParse(await File.ReadAllTextAsync(counterFile), out var currentCounter))
            {
                counter = currentCounter + 1;
            }
            await File.WriteAllTextAsync(counterFile, counter.ToString());

            // Create request file
            var shortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))
                .ToLowerInvariant().Replace(".", "").Replace(",", "");
            var requestFileName = $"{counter:D4}.{agentType}.request.{shortTitle}.md";
            var requestFile = Path.Combine(messagesDirectory, requestFileName);

            await File.WriteAllTextAsync(requestFile, markdownContent);

            await SetupAgentWorkspace(agentWorkspaceDirectory);

            // Save task metadata with full paths
            var currentTaskInfo = new
            {
                task_number = $"{counter:D4}",
                request_file_path = requestFile,  // Full absolute path
                started_at = DateTime.UtcNow.ToString("O"),
                attempt = 1,
                branch = branchName,
                title = taskTitle
            };

            var currentTaskFile = Path.Combine(agentWorkspaceDirectory, "current-task.json");
            await File.WriteAllTextAsync(currentTaskFile, JsonSerializer.Serialize(currentTaskInfo, new JsonSerializerOptions { WriteIndented = true }));

            var taskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
            await File.WriteAllTextAsync(taskIdFile, $"{counter:D4}");

            // Load system prompt and workflow
            var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
            var systemPromptText = "";
            if (File.Exists(systemPromptFile))
            {
                systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
                systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
            }

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

            // Build Claude arguments
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

            if (!string.IsNullOrEmpty(workflowText))
            {
                claudeArgs.Add("--append-system-prompt");
                claudeArgs.Add(workflowText);
            }

            // Add slash command to trigger workflow
            var slashCommand = agentType.Contains("reviewer") ? "/review/task" : "/implement/task";
            claudeArgs.Add(slashCommand);

            // Launch worker-agent (Claude Code) in agent workspace
            var process = await LaunchClaudeCode(agentWorkspaceDirectory, claudeArgs);

            // Create .worker-process-id with worker-agent's PID
            await File.WriteAllTextAsync(workerProcessIdFile, process.Id.ToString());

            // Track active worker session
            AddWorkerSession(process.Id, agentType, taskTitle, requestFileName, process);

            LogWorkflowEvent($"[{counter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{requestFileName}]", messagesDirectory);

            try
            {
                // Monitor for completion
                var result = await WaitForWorkerCompletionAsync(messagesDirectory, counter, agentType, process.Id, taskTitle, requestFileName);
                return result;
            }
            finally
            {
                // Clean up PID files
                if (File.Exists(hostProcessIdFile))
                {
                    File.Delete(hostProcessIdFile);
                }
                if (File.Exists(workerProcessIdFile))
                {
                    File.Delete(workerProcessIdFile);
                }

                RemoveWorkerSession(process.Id);
                workspaceMutex.ReleaseMutex();
                workspaceMutex.Dispose();
            }
        }
        catch
        {
            // Clean up on error
            if (File.Exists(hostProcessIdFile))
            {
                File.Delete(hostProcessIdFile);
            }
            if (File.Exists(workerProcessIdFile))
            {
                File.Delete(workerProcessIdFile);
            }

            workspaceMutex.ReleaseMutex();
            workspaceMutex.Dispose();

            throw; // Re-throw to be caught by ExecuteAsync
        }
    }

    private static async Task RunInteractiveMode(string? agentType)
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

        // Check for existing worker-host process
        var hostProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".host-process-id");
        if (File.Exists(hostProcessIdFile))
        {
            var existingPid = await File.ReadAllTextAsync(hostProcessIdFile);
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
                        existingProcess.Kill();
                        await Task.Delay(TimeSpan.FromSeconds(1)); // Wait for cleanup
                    }
                }
                catch (ArgumentException)
                {
                    // Process doesn't exist - stale PID file, just delete it silently
                }
            }

            File.Delete(hostProcessIdFile);
        }

        // Create .host-process-id so MCP can detect this interactive worker-host
        await File.WriteAllTextAsync(hostProcessIdFile, Process.GetCurrentProcess().Id.ToString());

        // Ensure Ctrl+C exits cleanly and removes PID files
        Console.CancelKeyPress += (_, e) =>
        {
            // Clean up .host-process-id on exit
            if (File.Exists(hostProcessIdFile))
            {
                File.Delete(hostProcessIdFile);
            }

            // Clean up .worker-process-id if exists
            var workerProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".worker-process-id");
            if (File.Exists(workerProcessIdFile))
            {
                File.Delete(workerProcessIdFile);
            }

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

        // Check for task recovery - if .task-id exists, resume the task
        var taskIdFile = Path.Combine(agentWorkspaceDirectory, ".task-id");
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");

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

        // Tech-lead launches immediately, other agents wait for requests
        if (agentType == "tech-lead")
        {
            // Tech-lead launches directly (same as pressing ENTER)
            await LaunchManualClaudeSession(agentType, branch);
            AnsiConsole.MarkupLine($"[{agentColor} bold]✓ Tech Lead session ended[/]");
        }
        else
        {
            // Display initial waiting screen with recent activity
            RedrawWaitingDisplay(agentType, branch);

            await WatchForRequestsAsync(agentType, messagesDirectory, branch);
        }
    }

    // Request Watching & Handling
    private static async Task WatchForRequestsAsync(string agentType, string messagesDirectory, string branch)
    {
        // Create messages directory if it doesn't exist (needed for FileSystemWatcher)
        Directory.CreateDirectory(messagesDirectory);

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

    private static async Task<bool> WaitInStandbyMode(string agentType, string branch, Func<bool> checkForRequest)
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

    private static async Task HandleIncomingRequest(string requestFile, string agentType, string branch)
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

        // Launch worker-agent (Claude Code) with /implement/task slash command
        var claudeProcess = await LaunchClaudeCodeAsync(agentType, branch);

        // Create .worker-process-id with worker-agent's PID (so CompleteTask can kill it)
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);
        var workerProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".worker-process-id");
        await File.WriteAllTextAsync(workerProcessIdFile, claudeProcess.Id.ToString());

        // Wait for response file (worker will call CompleteTask which kills itself)
        await WaitForResponseAndKillClaude(requestFile, agentType, branch, claudeProcess);

        // Clean up .worker-process-id after worker exits
        if (File.Exists(workerProcessIdFile))
        {
            File.Delete(workerProcessIdFile);
        }

        // Return to waiting display
        RedrawWaitingDisplay(agentType, branch);
    }

    private static async Task LaunchManualClaudeSession(string agentType, string branch)
    {
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);

        // Tech-lead uses acceptEdits, others use bypassPermissions
        var permissionMode = agentType == "tech-lead" ? "acceptEdits" : "bypassPermissions";

        var manualArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", permissionMode
        };

        // Load system prompt
        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        if (File.Exists(systemPromptFile))
        {
            var systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
            systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
            manualArgs.Add("--append-system-prompt");
            manualArgs.Add(systemPromptText);
        }

        // Add slash command based on agent type
        if (agentType == "tech-lead")
        {
            manualArgs.Add("/orchestrate/tech-lead");
        }

        // Launch and wait
        var process = await LaunchClaudeCode(agentWorkspaceDirectory, manualArgs);

        // Create .worker-process-id
        var workerProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".worker-process-id");
        await File.WriteAllTextAsync(workerProcessIdFile, process.Id.ToString());

        await process.WaitForExitAsync();

        // Clean up
        if (File.Exists(workerProcessIdFile))
        {
            File.Delete(workerProcessIdFile);
        }
    }

    private static async Task<Process> LaunchClaudeCodeAsync(string agentType, string branch)
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

        // Add slash command to trigger workflow (like tech-lead uses /orchestrate/tech-lead)
        var slashCommand = agentType.Contains("reviewer") ? "/review/task" : "/implement/task";
        claudeArgs.Add(slashCommand);

        // Use common launch method (handles session management)
        return await LaunchClaudeCode(agentWorkspaceDirectory, claudeArgs);
    }

    private static async Task WaitForResponseAndKillClaude(string requestFile, string agentType, string branch, Process claudeProcess)
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

    // Setup & Utilities
    internal static async Task SetupAgentWorkspace(string agentWorkspaceDirectory)
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

    internal static async Task<Process> LaunchClaudeCode(string agentWorkspaceDirectory, List<string> additionalArgs, string? workingDirectory = null)
    {
        workingDirectory ??= agentWorkspaceDirectory;
        var sessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");

        // Try with --continue if session marker exists
        if (File.Exists(sessionIdFile))
        {
            var argsWithContinue = new List<string> { "--continue" };
            argsWithContinue.AddRange(additionalArgs);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = string.Join(" ", argsWithContinue.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                }
            };

            process.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
            process.Start();

            // Wait briefly to check if it started successfully
            await Task.Delay(TimeSpan.FromSeconds(2));

            // If still running, --continue succeeded
            if (!process.HasExited)
            {
                return process; // Return LIVE process
            }

            // --continue failed (no conversation to continue), delete marker and start fresh
            File.Delete(sessionIdFile);
        }

        // Fresh start (no session marker or --continue failed)
        // Create session marker BEFORE starting so any exit (crash, kill, normal) leaves the file
        await File.WriteAllTextAsync(sessionIdFile, Guid.NewGuid().ToString());

        var freshArgs = new List<string>();
        freshArgs.AddRange(additionalArgs);

        var freshProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", freshArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };

        freshProcess.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
        freshProcess.Start();

        return freshProcess;
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

    // Worker Completion & Restart Logic

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
        if (!ActiveWorkerSessions.TryGetValue(processId, out var session))
        {
            return "Error: Worker session not found in active sessions";
        }

        var currentProcess = session.Process;

        while (DateTime.Now - startTime < overallTimeout)
        {
            // Check process exit frequently (every 5 seconds) instead of blocking for 20 minutes
            // This allows prompt detection when worker calls CompleteTask and kills itself
            var checkInterval = TimeSpan.FromSeconds(5);
            var checksUntilInactivityCheck = (int)(inactivityCheckInterval.TotalSeconds / checkInterval.TotalSeconds);

            var processExited = false;
            for (var i = 0; i < checksUntilInactivityCheck; i++)
            {
                var exited = currentProcess.WaitForExit(checkInterval);
                if (exited)
                {
                    processExited = true;
                    break; // Process died, exit immediately!
                }
            }

            if (processExited)
            {
                // Worker completed normally (called CompleteTask which killed itself)
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker process exited normally\n");
                break;
            }

            // 20 minutes passed - check if worker is making progress
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Inactivity check: No completion after 20 minutes\n");

            var hasGitChanges = HasGitChanges();
            if (hasGitChanges)
            {
                // Worker is making changes, still active
                await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Git changes detected, worker is active\n");
                continue;
            }

            // No git changes for 20 minutes - worker is stuck, restart it
            await File.AppendAllTextAsync(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] No git changes detected, restarting worker\n");

            if (!currentProcess.HasExited)
            {
                currentProcess.Kill();
            }

            // Restart worker
            var restartResult = await RestartWorker(agentType);
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

        // Worker exited - response file should be in messages directory
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

    private static async Task<(bool Success, int ProcessId, Process? Process, string ErrorMessage)> RestartWorker(string agentType)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var branchWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName);
        var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDir, agentType);

        // Load system prompt
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions"
        };

        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        if (File.Exists(systemPromptFile))
        {
            var systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
            systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
            claudeArgs.Add("--append-system-prompt");
            claudeArgs.Add(systemPromptText);
        }

        // Add workflow
        var workflowFile = agentType.Contains("reviewer")
            ? Path.Combine(Configuration.SourceCodeFolder, ".claude", "commands", "review", "task.md")
            : Path.Combine(Configuration.SourceCodeFolder, ".claude", "commands", "implement", "task.md");

        if (File.Exists(workflowFile))
        {
            var workflowText = await File.ReadAllTextAsync(workflowFile);
            var frontmatterEnd = workflowText.IndexOf("---", 3, StringComparison.Ordinal);
            if (frontmatterEnd > 0)
            {
                workflowText = workflowText.Substring(frontmatterEnd + 3).Trim();
            }
            workflowText = workflowText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();

            if (!string.IsNullOrEmpty(workflowText))
            {
                claudeArgs.Add("--append-system-prompt");
                claudeArgs.Add(workflowText);
            }
        }

        // Add restart nudge
        var completionCommand = agentType.Contains("reviewer") ? "/complete/review" : "/complete/task";
        claudeArgs.Add("--append-system-prompt");
        claudeArgs.Add($"You are a {agentType} Worker. It looks like you stopped. " +
                       $"Please re-read the latest request file and continue working on it. " +
                       $"Remember to call {completionCommand} when done or if stuck.");

        // Use common launch method (handles session management) in agent workspace
        var process = await LaunchClaudeCode(agentWorkspaceDirectory, claudeArgs);
        await Task.Delay(TimeSpan.FromSeconds(2));

        if (process.HasExited)
        {
            return (false, -1, null, $"Process exited immediately with code: {process.ExitCode}");
        }

        return (true, process.Id, process, "");
    }

    private static void UpdateWorkerSession(int oldProcessId, int newProcessId, string agentType, string taskTitle, string requestFileName, Process newProcess)
    {
        // Update .worker-process-id file with new process ID
        var branchName = GitHelper.GetCurrentBranch();
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, agentType);
        var workerProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".worker-process-id");
        File.WriteAllText(workerProcessIdFile, newProcessId.ToString());

        RemoveWorkerSession(oldProcessId);
        AddWorkerSession(newProcessId, agentType, taskTitle, requestFileName, newProcess);
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

    internal static void LogWorkflowEvent(string message, string messagesDirectory)
    {
        var logFile = Path.Combine(Path.GetDirectoryName(messagesDirectory)!, "workflow.log");
        var logEntry = $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] {message}\n";

        if (!Directory.Exists(Path.GetDirectoryName(logFile)))
        {
            return;
        }

        File.AppendAllText(logFile, logEntry);
    }

    // Workers (Session Management)

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

    // MCP Tools for Worker Completion

    public static async Task<string> CompleteTask(
        string agentType,
        string taskSummary,
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

        // Read .worker-process-id file to find worker-agent process
        var workerProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".worker-process-id");
        if (File.Exists(workerProcessIdFile))
        {
            var processIdContent = await File.ReadAllTextAsync(workerProcessIdFile);
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

    public static async Task<string> CompleteReview(
        string agentType,
        bool approved,
        string reviewSummary,
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

        // Read .worker-process-id file to find worker-agent process
        var workerProcessIdFile = Path.Combine(agentWorkspaceDirectory, ".worker-process-id");
        if (File.Exists(workerProcessIdFile))
        {
            var processIdContent = await File.ReadAllTextAsync(workerProcessIdFile);
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

        return $"Review completed ({statusPrefix}). Response file: {responseFileName}";
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
