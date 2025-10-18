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
                await RunMcpMode(agentType, taskTitle, markdownContent, prdPath, productIncrementPath, taskNumber);
            }
            else
            {
                await RunInteractiveMode(agentType);
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
        string? agentType,
        string? taskTitle,
        string? markdownContent,
        string? prdPath,
        string? productIncrementPath,
        string? taskNumber)
    {
        if (string.IsNullOrEmpty(agentType) || string.IsNullOrEmpty(taskTitle) || string.IsNullOrEmpty(markdownContent))
        {
            throw new ArgumentException("--mcp mode requires agent-type, --task-title, and --markdown-content");
        }

        var workspace = new Workspace(agentType);

        // Check if interactive worker-host is running
        if (!File.Exists(workspace.HostProcessIdFile))
        {
            await Console.Out.WriteLineAsync($"ERROR: No interactive '{agentType}' worker-host running. Start with: {Configuration.AliasName} claude-agent {agentType}");
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

        // Wait for response file (no polling, no timeout - worker manages its own lifecycle)
        var responseFilePattern = $"{taskCounter:D4}.{workspace.AgentType}.response.*.md";

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

        ClaudeAgentLifecycle.LogWorkflowEvent($"[{taskCounter:D4}.{workspace.AgentType}.response] Completed: '{taskTitle}' -> [{actualResponseFileName}]");

        var result = $"Task delegated successfully to '{workspace.AgentType}'.\n" +
                     $"Task number: {taskCounter:D4}\n" +
                     $"Request file: {taskRequestFileName}\n" +
                     $"Response file: {actualResponseFileName}\n\n" +
                     $"Response content:\n{responseContent}";

        await Console.Out.WriteLineAsync(result);
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
            var existingProcessIdContent = await File.ReadAllTextAsync(workspace.HostProcessIdFile);
            if (int.TryParse(existingProcessIdContent, out var existingProcessId))
            {
                try
                {
                    var existingProcess = Process.GetProcessById(existingProcessId);
                    if (!existingProcess.HasExited)
                    {
                        // Active worker-host is running - calculate how long it's been alive
                        var processAge = DateTime.Now - existingProcess.StartTime;
                        var ageString = processAge.TotalMinutes < 1
                            ? $"{(int)processAge.TotalSeconds} seconds ago"
                            : processAge.TotalHours < 1
                                ? $"{(int)processAge.TotalMinutes} minutes {(int)(processAge.TotalSeconds % 60)} seconds ago"
                                : $"{(int)processAge.TotalHours} hours {processAge.Minutes} minutes ago";

                        AnsiConsole.MarkupLine($"[yellow]⚠ Another '{agentType}' worker-host is currently running (PID: {existingProcessId}, Started: {ageString})[/]");

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

        // Ensure .host-process-id exists for MCP delegation (create if automated host exited)
        if (!File.Exists(workspace.HostProcessIdFile))
        {
            await File.WriteAllTextAsync(workspace.HostProcessIdFile, Process.GetCurrentProcess().Id.ToString());
        }

        // Check for existing request files (only if no worker currently active)
        if (!File.Exists(workspace.WorkerProcessIdFile))
        {
            var existingRequests = Directory.GetFiles(workspace.MessagesDirectory, $"*.{workspace.AgentType}.request.*.md")
                .OrderBy(File.GetCreationTime)
                .ToList();

            if (existingRequests.Count > 0)
            {
                AnsiConsole.MarkupLine($"[yellow]Found {existingRequests.Count} pending request(s), processing...[/]");
                foreach (var requestFile in existingRequests)
                {
                    await HandleIncomingRequest(requestFile, workspace);
                }
            }
        }

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
                // Request file arrived - capture path before resetting flags
                var pathToProcess = requestFilePath;
                requestReceived = false;
                requestFilePath = null;

                // Process request (blocking call - may take minutes)
                await HandleIncomingRequest(pathToProcess, workspace);
                // Note: Don't reset flags after processing - new requests arriving during processing will have already set fresh flag values
            }
            else if (userPressedEnter)
            {
                // User pressed ENTER - launch manual session and return to waiting
                await LaunchManualClaudeSession(workspace, useSlashCommand: false);
            }
        }
        // ReSharper disable once FunctionNeverReturns
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
            TimeSpan.FromMinutes(115),
            true,
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
        AnsiConsole.MarkupLine(result.Success ? $"[{agentColor} bold]✓ {result.Message}[/]" : $"[red bold]✗ {result.Message}[/]");

        // Return to waiting display
        RedrawWaitingDisplay(workspace.AgentType, workspace.Branch);
    }

    private async Task LaunchManualClaudeSession(Workspace workspace, string? taskTitleForSlashCommand = null, bool useSlashCommand = true)
    {
        // Launch worker (slash command usage controlled by useSlashCommand parameter)
        var process = await LaunchWorker(workspace, taskTitleForSlashCommand, useSlashCommand);

        // Monitor process and wait for completion
        // Tech-lead gets 62 minutes (user might be thinking), workers get 20 minutes
        var inactivityTimeout = workspace.AgentType == "tech-lead"
            ? TimeSpan.FromMinutes(62)
            : TimeSpan.FromMinutes(20);

        var options = new ProcessMonitoringOptions(
            inactivityTimeout,
            TimeSpan.FromMinutes(115),
            false
        );

        var result = await MonitorProcessWithTimeout(process, workspace.AgentType, options);

        // Show completion status
        var agentColor = GetAgentColor(workspace.AgentType);
        AnsiConsole.MarkupLine(result.Success ? $"[{agentColor} bold]✓ {result.Message}[/]" : $"[red bold]✗ {result.Message}[/]");

        // Clean up
        if (File.Exists(workspace.WorkerProcessIdFile))
        {
            File.Delete(workspace.WorkerProcessIdFile);
        }
    }

    private async Task<Process> LaunchWorker(
        Workspace workspace,
        string? taskTitle = null,
        bool useSlashCommand = true)
    {
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
        Logger.Debug("LAUNCH - Starting Claude Code");
        Logger.Debug($"Agent Type: {workspace.AgentType}");
        Logger.Debug($"Working Directory: {workspace.AgentWorkspaceDirectory}");
        Logger.Debug($"Command: {commandLine}");

        // Launch with session management
        var process = await LaunchClaudeCode(workspace.AgentWorkspaceDirectory, claudeArgs);

        Logger.Debug($"Process started with ID: {process.Id}");
        // Verification delay to confirm process launched successfully before returning
        await Task.Delay(TimeSpan.FromSeconds(3));
        Logger.Debug($"Process alive after delay: {!process.HasExited}");

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
            DateTime.Now.ToString("O"),
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

    private static async Task<Process> LaunchClaudeCode(string agentWorkspaceDirectory, List<string> additionalArgs, string? workingDirectory = null)
    {
        workingDirectory ??= agentWorkspaceDirectory;
        var sessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");

        Logger.Debug($"LaunchClaudeCode - Session file exists: {File.Exists(sessionIdFile)}");

        // Try --continue if session marker exists
        if (File.Exists(sessionIdFile))
        {
            Logger.Debug("Attempting --continue (session marker exists)");

            var argsWithContinue = new List<string> { "--continue" };

            var addDirArg = additionalArgs.IndexOf("--add-dir");
            if (addDirArg >= 0 && addDirArg + 1 < additionalArgs.Count)
            {
                argsWithContinue.Add("--add-dir");
                argsWithContinue.Add(additionalArgs[addDirArg + 1]);
            }

            argsWithContinue.Add("--permission-mode");
            argsWithContinue.Add("bypassPermissions");

            var slashCommand = additionalArgs.FirstOrDefault(arg => arg.StartsWith('/') && !arg.Substring(1).Contains('/'));
            if (slashCommand is not null)
            {
                argsWithContinue.Add(slashCommand);
            }

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
            "backend-engineer" => "Backend Engineer",
            "frontend-engineer" => "Frontend Engineer",
            "backend-reviewer" => "Backend Reviewer",
            "frontend-reviewer" => "Frontend Reviewer",
            "test-automation-engineer" => "Test Automation Engineer",
            "test-automation-reviewer" => "Test Automation Reviewer",
            _ => throw new ArgumentException($"Unknown agent type: '{agentType}'")
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
            _ => throw new ArgumentException($"Unknown agent type: '{agentType}'")
        };
    }

    private static void SetTerminalTitle(string title)
    {
        // ANSI escape sequence to set terminal title
        // Works in most modern terminals (iTerm2, Terminal.app, Windows Terminal, etc.)
        Console.Write($"\x1b]0;{title}\x07");
    }

    private async Task<ProcessCompletionResult> MonitorProcessWithTimeout(Process process, string agentType, ProcessMonitoringOptions options)
    {
        var startTime = DateTime.Now;

        while (true)
        {
            // Check overall timeout
            if (DateTime.Now - startTime >= options.OverallTimeout)
            {
                Logger.Debug($"Overall timeout ({options.OverallTimeout.TotalMinutes} minutes) reached, killing process");
                KillProcess(process);
                return new ProcessCompletionResult(false, $"Worker killed after {options.OverallTimeout.TotalMinutes} minutes (overall timeout)");
            }

            // Wait for process to exit with inactivity timeout
            var exited = process.WaitForExit(options.InactivityTimeout);

            if (exited)
            {
                Logger.Debug($"Process {process.Id} exited with code: {process.ExitCode}");
                break;
            }

            // Inactivity timeout reached - check if worker made progress
            var hasGitChanges = GitHelper.HasUncommittedChanges();
            if (!hasGitChanges)
            {
                // No git changes - worker is stuck, kill it
                Logger.Debug($"Worker inactive for {options.InactivityTimeout.TotalMinutes} minutes with no git changes, killing process");
                KillProcess(process);
                return new ProcessCompletionResult(false, $"Worker killed after {options.InactivityTimeout.TotalMinutes} minutes of inactivity (no git changes detected)");
            }

            // Has git changes - worker is active, continue monitoring
            Logger.Debug("Git changes detected, worker is active - continuing");
        }

        if (!options.ExpectResponseFile)
        {
            return new ProcessCompletionResult(true, "Session completed");
        }

        // Allow time for MCP tool to write response file
        await Task.Delay(TimeSpan.FromMilliseconds(500));

        // Check for response file
        var matchingFiles = Directory.GetFiles(options.MessagesDirectory!, options.ResponseFilePattern!);
        if (matchingFiles.Length == 0)
        {
            return new ProcessCompletionResult(false, $"Worker exited but no response file found matching: {options.ResponseFilePattern}");
        }

        var responseFilePath = matchingFiles[0];
        var responseFileName = Path.GetFileName(responseFilePath);
        var responseContent = await File.ReadAllTextAsync(responseFilePath);

        ClaudeAgentLifecycle.LogWorkflowEvent($"[{options.TaskNumber}.{agentType}.response] Completed: '{responseFileName}'");

        return new ProcessCompletionResult(true, "Task completed successfully", responseContent);

        // Tech-lead or manual session - no response file expected
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
        return new ProcessStartInfo
        {
            FileName = "claude",
            Arguments = string.Join(" ", claudeArgs.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg)),
            WorkingDirectory = workingDirectory,
            UseShellExecute = true
        };
    }
}

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
    string? TaskNumber = null,
    string? ResponseFilePattern = null,
    string? MessagesDirectory = null
);

public record ProcessCompletionResult(bool Success, string Message, string? ResponseContent = null);

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
