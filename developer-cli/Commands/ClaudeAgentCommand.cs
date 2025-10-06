using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ClaudeAgentCommand : Command
{
    private static readonly Dictionary<int, WorkerSession> ActiveWorkerSessions = new();
    private static readonly Lock WorkerSessionLock = new();
    private static string? SelectedAgentType;
    private static bool ShowAllActivities = false;

    public ClaudeAgentCommand() : base("claude-agent", "Interactive Worker Host for agent development")
    {
        var agentTypeArgument = new Argument<string?>("agent-type", () => null)
        {
            Description = "Agent type to run (tech-lead, backend-engineer, backend-reviewer, frontend-engineer, frontend-reviewer, test-automation-engineer, test-automation-reviewer)"
        };
        agentTypeArgument.Arity = ArgumentArity.ZeroOrOne;

        var resumeOption = new Option<bool>("--resume", "Resume specific tech lead session from workspace (only for tech-lead agent type)");
        var continueOption = new Option<bool>("--continue", "Continue most recent conversation in main repo (only for tech-lead agent type)");

        AddArgument(agentTypeArgument);
        AddOption(resumeOption);
        AddOption(continueOption);

        this.SetHandler(ExecuteAsync, agentTypeArgument, resumeOption, continueOption);
    }

    private async Task ExecuteAsync(string? agentType, bool resume, bool continueSession)
    {
        try
        {
            SelectedAgentType = agentType;

            // Interactive mode
            await RunInteractiveMode(agentType, resume, continueSession);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    private async Task RunInteractiveMode(string? agentType, bool resume, bool continueSession)
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

        SelectedAgentType = agentType;
        var branch = GitHelper.GetCurrentBranch();

        // Create workspace and register agent
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);
        Directory.CreateDirectory(agentWorkspaceDirectory);

        // Setup workspace with symlink to .claude directory
        await SetupAgentWorkspace(agentWorkspaceDirectory);

        // Check for stale PID file
        var pidFile = Path.Combine(agentWorkspaceDirectory, ".pid");
        if (File.Exists(pidFile))
        {
            var existingPid = await File.ReadAllTextAsync(pidFile);
            if (int.TryParse(existingPid, out var pid))
            {
                try
                {
                    var existingProcess = Process.GetProcessById(pid);
                    if (!existingProcess.HasExited)
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning: An interactive {agentType} appears to be already running (PID: {pid})[/]");
                        AnsiConsole.MarkupLine("[yellow]This might be a stale PID file from a crashed session.[/]");

                        var deleteChoice = AnsiConsole.Prompt(
                            new SelectionPrompt<string>()
                                .Title("Delete the stale PID file and continue?")
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
                    // Process doesn't exist, PID is stale
                }
            }

            File.Delete(pidFile);
        }

        // Create PID file to register this agent
        var currentPid = Environment.ProcessId;
        await File.WriteAllTextAsync(pidFile, currentPid.ToString());

        // Ensure PID file is deleted on exit (keep session ID for conversation persistence)
        AppDomain.CurrentDomain.ProcessExit += (_, _) => CleanupPidFile(pidFile);
        Console.CancelKeyPress += (_, e) =>
        {
            CleanupPidFile(pidFile);
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
            await LaunchTechLeadAsync(agentType, branch, resume, continueSession);
            return;
        }

        // Display initial waiting screen with recent activity
        RedrawWaitingDisplay(agentType, branch);

        // Start watching for request files
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");
        Directory.CreateDirectory(messagesDirectory);

        await WatchForRequestsAsync(agentType, messagesDirectory, branch);
    }

    private async Task LaunchTechLeadAsync(string agentType, string branch, bool resume, bool continueSession)
    {
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);

        // Load Tech Lead system prompt from .txt file
        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        string systemPromptText = "";

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

        // Tech Lead uses same deterministic session management as other agents
        var claudeSessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");
        var techLeadArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "acceptEdits"
        };

        // Add system prompt if available
        if (!string.IsNullOrEmpty(systemPromptText))
        {
            techLeadArgs.Add("--append-system-prompt");
            techLeadArgs.Add(systemPromptText);
        }

        techLeadArgs.Add("/tech-lead-mode");

        // TODO: When Anthropic fixes bug #3188, switch back to --resume {session-id}
        // Deterministic session management (ignoring command line flags for consistency)
        if (File.Exists(claudeSessionIdFile))
        {
            // Session exists - use --continue (workaround for Anthropic bug #3188 where --resume ignores session ID)
            // var claudeSessionId = await File.ReadAllTextAsync(claudeSessionIdFile);
            // techLeadArgs.Insert(0, "--resume");
            // techLeadArgs.Insert(1, claudeSessionId.Trim());
            techLeadArgs.Insert(0, "--continue");
        }
        else
        {
            // First time - create session marker file for tracking
            var newClaudeSessionId = Guid.NewGuid().ToString();
            await File.WriteAllTextAsync(claudeSessionIdFile, newClaudeSessionId);
            techLeadArgs.Insert(0, "--session-id");
            techLeadArgs.Insert(1, newClaudeSessionId);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", techLeadArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = Configuration.SourceCodeFolder,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };

        process.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
        process.Start();

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
        using var watcher = new FileSystemWatcher(messagesDirectory)
        {
            Filter = "*.md",
            EnableRaisingEvents = true
        };

        watcher.Created += (sender, e) => lastActivity = DateTime.Now;

        while (!techLeadProcess.HasExited)
        {
            // Find most recent file modification
            var mostRecentFileTime = ClaudeAgentCommand.GetMostRecentFileModification();
            if (mostRecentFileTime > lastActivity)
            {
                lastActivity = mostRecentFileTime;
            }

            var timeSinceLastActivity = DateTime.Now - lastActivity;

            if (timeSinceLastActivity > timeout)
            {
                // Log tech lead timeout to workflow log
                var workflowLogPath = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "workflow.log");
                var timeoutLogMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] TECH LEAD TIMEOUT: {agentType} inactive for 1 hour, killing process (main thread will restart)\n";
                await File.AppendAllTextAsync(workflowLogPath, timeoutLogMessage);

                // Kill stalled tech lead - main thread restart loop will handle restart
                try
                {
                    techLeadProcess.Kill();
                }
                catch
                {
                    // Process might have already exited
                }

                // Exit monitor - main thread has restart loop with proper terminal context
                break;
            }
            else
            {
                // Calculate how long to wait before next check
                var timeUntilTimeout = timeout - timeSinceLastActivity;
                // Wait for the remaining time (max 5 minutes to catch activity sooner)
                var waitTime = timeUntilTimeout > TimeSpan.FromMinutes(5)
                    ? TimeSpan.FromMinutes(5)
                    : timeUntilTimeout;

                await Task.Delay(waitTime);
            }
        }
    }

    internal static DateTime GetMostRecentFileModification()
    {
        try
        {
            var repositoryRoot = Configuration.SourceCodeFolder;

            // Find the most recently modified file anywhere
            return Directory.GetFiles(repositoryRoot, "*.*", SearchOption.AllDirectories)
                .Max(f => File.GetLastWriteTime(f));
        }
        catch
        {
            // If check fails, return current time to reset activity timer
            return DateTime.Now;
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

    private static void CleanupPidFile(string pidFile)
    {
        if (File.Exists(pidFile))
        {
            File.Delete(pidFile);
        }
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
                })
                .OrderBy(f => f.ResponseTime)  // Chronological order: oldest first, newest last
                .ToList();

            foreach (var file in responseFiles)
            {
                // Parse filename: NNNN.agent-type.response.Title-Case-Task-Name.md
                var parts = file.FileName.Split('.');
                if (parts.Length >= 4)
                {
                    var taskNumber = parts[0];
                    var taskDescription = parts[3].Replace("-md", "").Replace('-', ' ');

                    try
                    {
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

                                var activityLine = $"✅ {requestTimeStr}-{responseTimeStr} - {taskNumber} - {taskDescription} ({durationStr})";
                                activities.Add(activityLine);
                            }
                            else
                            {
                                // Duration calculation failed, use simple format
                                var timeStamp = file.ResponseTime.ToString("HH:mm");
                                var activityLine = $"✅ {timeStamp} - {taskNumber} - {taskDescription}";
                                activities.Add(activityLine);
                            }
                        }
                        else
                        {
                            // No request file found, use simple format
                            var timeStamp = file.ResponseTime.ToString("HH:mm");
                            var activityLine = $"✅ {timeStamp} - {taskNumber} - {taskDescription}";
                            activities.Add(activityLine);
                        }
                    }
                    catch
                    {
                        // Any file access error, use simple format
                        var timeStamp = file.ResponseTime.ToString("HH:mm");
                        var activityLine = $"✅ {timeStamp} - {taskNumber} - {taskDescription}";
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

    private async Task WatchForRequestsAsync(string agentType, string messagesDirectory, string branch)
    {
        using var fileSystemWatcher = new FileSystemWatcher(messagesDirectory)
        {
            Filter = $"*.{agentType}.request.*.md",
            EnableRaisingEvents = true
        };

        var requestReceived = false;
        string? requestFilePath = null;

        fileSystemWatcher.Created += (sender, e) =>
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
                try
                {
                    await HandleIncomingRequest(requestFilePath, agentType, branch);
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]Error handling request: {ex.Message}[/]");
                }
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
                else if (key.Key == ConsoleKey.A && (key.Modifiers & ConsoleModifiers.Control) != 0)
                {
                    // Ctrl+A - toggle showing all activities
                    ShowAllActivities = !ShowAllActivities;
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

        // Use same session ID for manual control
        var claudeSessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");
        var manualArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions"
        };

        // TODO: When Anthropic fixes bug #3188, switch back to --resume {session-id}
        if (File.Exists(claudeSessionIdFile))
        {
            // Session exists - use --continue (workaround for Anthropic bug #3188 where --resume ignores session ID)
            manualArgs.Insert(0, "--continue");
            // var claudeSessionId = await File.ReadAllTextAsync(claudeSessionIdFile);
            // manualArgs.Insert(0, "--resume");
            // manualArgs.Insert(1, claudeSessionId.Trim());
        }
        else
        {
            // First time - create session marker file for tracking
            var newClaudeSessionId = Guid.NewGuid().ToString();
            await File.WriteAllTextAsync(claudeSessionIdFile, newClaudeSessionId);
            manualArgs.Insert(0, "--continue");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", manualArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = agentWorkspaceDirectory,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };

        process.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
        process.Start();
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
        var claudeProcess = await LaunchClaudeCodeAsync(requestFile, agentType, branch);

        // Wait for response file and then kill Claude
        await WaitForResponseAndKillClaude(requestFile, agentType, branch, claudeProcess);

        // Return to waiting display
        RedrawWaitingDisplay(agentType, branch);
    }

    private async Task<Process> LaunchClaudeCodeAsync(string requestFile, string agentType, string branch)
    {
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");

        // Setup workspace with symlink to .claude directory (no custom CLAUDE.md needed)
        await SetupAgentWorkspace(agentWorkspaceDirectory);

        // Extract request file name components for response file
        var requestFileName = Path.GetFileName(requestFile);
        var match = Regex.Match(requestFileName, @"^(\d+)\.([^.]+)\.request\.(.+)\.md$");
        var counter = match.Groups[1].Value;
        var shortTitle = match.Groups[3].Value;
        var responseFileName = $"{counter}.{agentType}.response.{shortTitle}.md";

        // Configure args based on agent type
        var isTechLead = agentType == "tech-lead";

        if (isTechLead)
        {
            // Tech Lead launches directly with /tech-lead-mode (no request file needed)
            var techLeadArgs = new List<string>
            {
                "--continue",
                "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                "--add-dir", Configuration.SourceCodeFolder,
                "--permission-mode", "default",
"/tech-lead-mode"
            };

            var techLeadProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = string.Join(" ", techLeadArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                    WorkingDirectory = Configuration.SourceCodeFolder,
                    UseShellExecute = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                }
            };

            techLeadProcess.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
            techLeadProcess.Start();
            await techLeadProcess.WaitForExitAsync();

            // If --continue failed, try fresh tech lead session
            if (techLeadProcess.ExitCode != 0)
            {
                AnsiConsole.MarkupLine("[yellow]No existing conversation found, starting fresh tech lead session...[/]");
                await Task.Delay(TimeSpan.FromSeconds(1));

                var freshTechLeadArgs = new List<string>
                {
                    "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                    "--add-dir", Configuration.SourceCodeFolder,
                    "--permission-mode", "default",
    "/tech-lead-mode"
                };

                techLeadProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "claude",
                        Arguments = string.Join(" ", freshTechLeadArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                        WorkingDirectory = Configuration.SourceCodeFolder,
                        UseShellExecute = false,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    }
                };

                techLeadProcess.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
                techLeadProcess.Start();
            }

            return techLeadProcess;
        }

        // Regular worker configuration
        var workflowName = agentType switch
        {
            "backend-engineer" => "Backend Engineer Systematic Workflow",
            "frontend-engineer" => "Frontend Engineer Systematic Workflow",
            "backend-reviewer" => "Backend Reviewer Systematic Workflow",
            "frontend-reviewer" => "Frontend Reviewer Systematic Workflow",
            "e2e-test-reviewer" => "E2E Test Reviewer Systematic Workflow",
            _ => $"{agentType} Systematic Workflow"
        };

        // Parse task content using simple string operations - no regex needed
        var taskContent = await File.ReadAllTextAsync(requestFile);
        var isProductIncrementTask = taskContent.Contains("PRD:") ||
                                     (taskContent.Contains("Request:") && taskContent.Contains("Response:"));

        string finalPrompt;
        if (isProductIncrementTask)
        {
            if (agentType.Contains("reviewer"))
            {
                // Extract paths using simple string operations
                var prdPath = ExtractPathAfterKey(taskContent, "PRD:");
                var productIncrementPath = ExtractPathAfterKey(taskContent, "Product Increment:");
                var taskNumber = ExtractTextAfterKey(taskContent, "Task:").Trim('"');
                var requestFilePath = ExtractPathAfterKey(taskContent, "Request:");
                var responseFilePath = ExtractPathAfterKey(taskContent, "Response:");

                // Log extracted parameters for debugging
                var branchName = GitHelper.GetCurrentBranch();
                var workflowLog = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, "workflow.log");
                try
                {
                    var parameterLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] REVIEWER PARAMETERS - PRD:'{prdPath}' ProductIncrement:'{productIncrementPath}' Task:'{taskNumber}' Request:'{requestFilePath}' Response:'{responseFilePath}'\n";
                    File.AppendAllText(workflowLog, parameterLog);
                }
                catch { }

                finalPrompt = $"/review-task '{prdPath}' '{productIncrementPath}' '{taskNumber}' '{requestFilePath}' '{responseFilePath}'";

                // Log the final prompt for debugging
                try
                {
                    var promptLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] REVIEWER FINAL PROMPT: {finalPrompt}\n";
                    File.AppendAllText(workflowLog, promptLog);
                }
                catch { }
            }
            else
            {
                // Extract paths for engineers
                var prdPath = ExtractPathAfterKey(taskContent, "PRD:");
                var productIncrementPath = ExtractPathAfterKey(taskContent, "from ");
                var taskNumber = ExtractTextBetweenQuotes(taskContent, "task ");

                // Log extracted parameters for debugging
                var branchName = GitHelper.GetCurrentBranch();
                var workflowLog = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, "workflow.log");
                try
                {
                    var parameterLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ENGINEER PARAMETERS - PRD:'{prdPath}' ProductIncrement:'{productIncrementPath}' Task:'{taskNumber}'\n";
                    File.AppendAllText(workflowLog, parameterLog);
                }
                catch { }

                finalPrompt = $"/implement-task '{prdPath}' '{productIncrementPath}' '{taskNumber}'";

                // Log the final prompt for debugging
                try
                {
                    var promptLog = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ENGINEER FINAL PROMPT: {finalPrompt}\n";
                    File.AppendAllText(workflowLog, promptLog);
                }
                catch { }
            }
        }
        else
        {
            finalPrompt = $"Read {requestFile} and follow your {workflowName} exactly";
        }

        // Add response file instruction to the prompt itself (not just system prompt) to ensure it works on resumed sessions
        finalPrompt += $"\n\nCRITICAL: Create response file in same directory as request: {messagesDirectory}/{counter}.{agentType}.response.{{Your-Descriptive-Title}}.md.tmp then rename to .md. NOTE: Creating this file triggers immediate shutdown - you cannot continue working after. If user is actively communicating with you, DO NOT create response file until user says to continue. When user interrupts, respond immediately and update your todo list.";

        // Load system prompt from .txt file and transform for command-line usage
        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        string systemPromptText;

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
        else
        {
            // Fallback for unknown agent types
            systemPromptText = $"You are a {agentType} specialist";
        }

        // Deterministic session management with session IDs
        var claudeSessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions",
            "--append-system-prompt", systemPromptText,
            finalPrompt
        };

        // TODO: When Anthropic fixes bug #3188, switch back to --resume {session-id}
        if (File.Exists(claudeSessionIdFile))
        {
            // Session exists - use --continue (workaround for Anthropic bug #3188 where --resume ignores session ID)
            claudeArgs.Insert(0, "--continue");
            //var claudeSessionId = await File.ReadAllTextAsync(claudeSessionIdFile);
            //claudeArgs.Insert(0, "--resume");
            //claudeArgs.Insert(1, claudeSessionId.Trim());
            AnsiConsole.MarkupLine("[yellow]Continuing existing session...[/]");
        }
        else
        {
            // First time - create session marker file for tracking
            var newClaudeSessionId = Guid.NewGuid().ToString();
            await File.WriteAllTextAsync(claudeSessionIdFile, newClaudeSessionId);
            claudeArgs.Insert(0, "--session-id");
            claudeArgs.Insert(1, newClaudeSessionId);
            AnsiConsole.MarkupLine("[yellow]Starting new session...[/]");
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", claudeArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = agentWorkspaceDirectory,
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

        string? foundResponseFile = null;
        while (foundResponseFile == null)
        {
            // Check for any file matching the pattern
            var matchingFiles = Directory.GetFiles(messagesDirectory, responseFilePattern);
            if (matchingFiles.Length > 0)
            {
                foundResponseFile = matchingFiles[0];
                break;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500)); // Check every 500ms
        }

        if (foundResponseFile != null)
        {
            var agentColor = GetAgentColor(agentType);
            AnsiConsole.MarkupLine($"[{agentColor} bold]✓ Response file created[/]");
        }

        // Graceful shutdown: Send Ctrl+C twice, wait, then force kill if needed
        if (claudeProcess != null && !claudeProcess.HasExited)
        {
            try
            {
                // Send SIGINT twice (Ctrl+C, C)
                for (int i = 0; i < 2; i++)
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "kill",
                        Arguments = $"-SIGINT {claudeProcess.Id}",
                        UseShellExecute = false,
                        RedirectStandardOutput = true
                    });
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
            catch
            {
                try { claudeProcess.Kill(); } catch { }
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
        var activitiesToShow = ShowAllActivities
            ? recentActivities
            : recentActivities.TakeLast(5).ToList();

        if (!ShowAllActivities && recentActivities.Count > 5)
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
            _ => agentType
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
            _ => Color.White
        };
    }

    private static void SetTerminalTitle(string title)
    {
        // ANSI escape sequence to set terminal title
        // Works in most modern terminals (iTerm2, Terminal.app, Windows Terminal, etc.)
        Console.Write($"\x1b]0;{title}\x07");
    }

    private static string ExtractPathAfterKey(string content, string key)
    {
        var keyIndex = content.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (keyIndex == -1) return "";

        var startIndex = keyIndex + key.Length;
        // Skip any whitespace after the key
        while (startIndex < content.Length && char.IsWhiteSpace(content[startIndex]) && content[startIndex] != '\r' && content[startIndex] != '\n')
        {
            startIndex++;
        }

        // Extract until end of line OR until ". " (sentence boundary)
        var endIndex = content.IndexOfAny(['\r', '\n'], startIndex);
        if (endIndex == -1) endIndex = content.Length;

        // Check for ". " (sentence boundary) before end of line
        var sentenceEnd = content.IndexOf(". ", startIndex, StringComparison.Ordinal);
        if (sentenceEnd != -1 && sentenceEnd < endIndex)
        {
            endIndex = sentenceEnd;
        }

        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static string ExtractTextAfterKey(string content, string key)
    {
        var keyIndex = content.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (keyIndex == -1) return "";

        var startIndex = keyIndex + key.Length;
        // Skip any whitespace after the key
        while (startIndex < content.Length && char.IsWhiteSpace(content[startIndex]) && content[startIndex] != '\r' && content[startIndex] != '\n')
        {
            startIndex++;
        }

        // Extract until end of line
        var endIndex = content.IndexOfAny(['\r', '\n'], startIndex);
        if (endIndex == -1) endIndex = content.Length;

        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static string ExtractTextBetweenQuotes(string content, string beforeText)
    {
        var startPattern = beforeText + "\"";
        var startIndex = content.IndexOf(startPattern, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return "";

        startIndex += startPattern.Length;
        var endIndex = content.IndexOf('"', startIndex);
        if (endIndex == -1) return "";

        return content.Substring(startIndex, endIndex - startIndex);
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
    {
        "tech-lead", "backend-engineer", "backend-reviewer",
        "frontend-engineer", "frontend-reviewer", "test-automation-engineer", "test-automation-reviewer"
    };

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

        try
        {
            File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] StartWorker called: agentType={agentType}, taskTitle={taskTitle}\n");
        }
        catch (Exception logEx)
        {
            Console.Error.WriteLine($"[DEBUG LOG ERROR] {logEx.Message}");
        }

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
            var pidFile = Path.Combine(agentWorkspaceDirectory, ".pid");

            // Debug: Log the PID file path we're checking
            File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Checking for PID file: {pidFile}\n");
            File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] PID file exists: {File.Exists(pidFile)}\n");

            if (File.Exists(pidFile))
            {
                var pidContent = await File.ReadAllTextAsync(pidFile);
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

                            // Use file-based locking to prevent race conditions when multiple processes increment the counter
                            var taskCounterFile = Path.Combine(messagesDirectory, ".task-counter");
                            var taskCounterLockFile = Path.Combine(messagesDirectory, ".task-counter.lock");
                            var taskCounter = 1;

                            // Retry with exponential backoff if lock is held
                            for (int retry = 0; retry < 10; retry++)
                            {
                                try
                                {
                                    using (var lockStream = new FileStream(taskCounterLockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                                    {
                                        // We have the lock, read current value
                                        if (File.Exists(taskCounterFile) && int.TryParse(await File.ReadAllTextAsync(taskCounterFile), out var existingCounter))
                                        {
                                            taskCounter = existingCounter + 1;
                                        }

                                        // Write new value
                                        await File.WriteAllTextAsync(taskCounterFile, taskCounter.ToString());
                                        break; // Success, exit retry loop
                                    }
                                }
                                catch (IOException)
                                {
                                    // Lock is held by another process, wait and retry
                                    if (retry == 9)
                                    {
                                        throw new InvalidOperationException("Failed to acquire task counter lock after 10 attempts");
                                    }
                                    await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, retry)));
                                }
                            }

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

                            // Log task start
                            LogWorkflowEvent($"[{taskCounter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{taskRequestFileName}]", messagesDirectory);

                            // Wait for the response file to be created
                            // Agent creates "response.md" and we rename it to the correct pattern
                            var fixedResponseName = "response.md";
                            var expectedResponsePattern = $"{taskCounter:D4}.{agentType}.response.*.md";

                            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Waiting for interactive agent to complete: {fixedResponseName}[/]");

                            // Poll for response file with activity monitoring (2-hour overall max)
                            var startTime = DateTime.Now;
                            var lastActivity = DateTime.Now;
                            var overallTimeout = TimeSpan.FromHours(2);

                            string? foundResponseFile = null;
                            // Wait for any file matching the pattern (agents can use descriptive names)
                            while (foundResponseFile == null)
                            {
                                // Check for file system activity
                                var mostRecentFileTime = ClaudeAgentCommand.GetMostRecentFileModification();
                                if (mostRecentFileTime > lastActivity)
                                {
                                    lastActivity = mostRecentFileTime;
                                }

                                // Only enforce overall timeout (2 hours max), no inactivity timeout for interactive agents
                                if (DateTime.Now - startTime > overallTimeout)
                                {
                                    throw new TimeoutException($"Interactive {agentType} exceeded 2-hour overall timeout");
                                }

                                // Check for the fixed response file
                                var fixedResponsePath = Path.Combine(agentWorkspaceDirectory, fixedResponseName);
                                if (File.Exists(fixedResponsePath))
                                {
                                    // Generate proper response filename
                                    var interactiveShortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5))
                                        .ToLowerInvariant().Replace(".", "").Replace(",", "");
                                    var properResponseName = $"{taskCounter:D4}.{agentType}.response.{interactiveShortTitle}.md";
                                    var properResponsePath = Path.Combine(messagesDirectory, properResponseName);

                                    // Move the file to the correct location with proper naming
                                    File.Move(fixedResponsePath, properResponsePath);
                                    foundResponseFile = properResponsePath;
                                    break;
                                }

                                await Task.Delay(TimeSpan.FromMilliseconds(500)); // Check every 500ms
                            }

                            AnsiConsole.MarkupLine("[grey][[MCP DEBUG]] Response file detected, reading content...[/]");

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
                        File.Delete(pidFile);
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

            // Use file-based locking to prevent race conditions when multiple processes increment the counter
            var counterFile = Path.Combine(messagesDirectory, ".task-counter");
            var counterLockFile = Path.Combine(messagesDirectory, ".task-counter.lock");
            var counter = 1;

            // Retry with exponential backoff if lock is held
            for (int retry = 0; retry < 10; retry++)
            {
                try
                {
                    using (var lockStream = new FileStream(counterLockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                    {
                        // We have the lock, read current value
                        if (File.Exists(counterFile) && int.TryParse(await File.ReadAllTextAsync(counterFile), out var currentCounter))
                        {
                            counter = currentCounter + 1;
                        }

                        // Write new value
                        await File.WriteAllTextAsync(counterFile, counter.ToString());
                        break; // Success, exit retry loop
                    }
                }
                catch (IOException)
                {
                    // Lock is held by another process, wait and retry
                    if (retry == 9)
                    {
                        throw new InvalidOperationException("Failed to acquire task counter lock after 10 attempts");
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, retry)));
                }
            }

            var shortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(3))
                .ToLowerInvariant().Replace(".", "").Replace(",", "");
            var requestFileName = $"{counter:D4}.{agentType}.request.{shortTitle}.md";
            var requestFile = Path.Combine(messagesDirectory, requestFileName);

            await File.WriteAllTextAsync(requestFile, markdownContent);

            // Check if this is a new workspace
            var isNewWorkspace = !Directory.Exists(agentWorkspaceDirectory);
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

            // Deterministic session management for automated workers
            var claudeSessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");
            var claudeArgs = new List<string>
            {
                "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                "--add-dir", Configuration.SourceCodeFolder,
                "--permission-mode", "bypassPermissions",
                "--append-system-prompt", $"You are a {agentType} Worker. Process task in shared messages: {requestFile}",
                $"Read {requestFile}"
            };

            if (File.Exists(claudeSessionIdFile))
            {
                var claudeSessionId = await File.ReadAllTextAsync(claudeSessionIdFile);
                claudeArgs.Insert(0, "--resume");
                claudeArgs.Insert(1, claudeSessionId.Trim());
            }
            else
            {
                var newClaudeSessionId = Guid.NewGuid().ToString();
                await File.WriteAllTextAsync(claudeSessionIdFile, newClaudeSessionId);
                claudeArgs.Insert(0, "--session-id");
                claudeArgs.Insert(1, newClaudeSessionId);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = string.Join(" ", claudeArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                    WorkingDirectory = Configuration.SourceCodeFolder,
                    UseShellExecute = false
                }
            };

            // CRITICAL: Remove CLAUDECODE to prevent forced print mode
            process.StartInfo.Environment.Remove("CLAUDECODE");

            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Environment: CLAUDECODE removed\n");
            }
            catch
            {
            }

            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Full arguments array: [{string.Join(", ", claudeArgs.Select(arg => $"'{arg}'"))}]\n");
            }
            catch
            {
            }

            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting Claude Code worker in: {agentWorkspaceDirectory}\n");
            }
            catch
            {
            }

            var quotedArgs = string.Join(" ", claudeArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Command: claude {quotedArgs}\n");
            }
            catch
            {
            }

            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Starting Claude Code worker process in: {agentWorkspaceDirectory}[/]");
            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Claude args: {string.Join(" ", claudeArgs)}[/]");

            process.Start();

            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker process started with PID: {process.Id}\n");
            }
            catch
            {
            }

            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Worker process started with PID: {process.Id}[/]");

            // Log what Claude Code actually receives as input
            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Claude input should be: {claudeArgs.Last()}\n");
            }
            catch
            {
            }

            // Check if process exits immediately
            await Task.Delay(TimeSpan.FromSeconds(3));
            if (process.HasExited)
            {
                File.AppendAllText(workflowLog, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: Worker process {process.Id} exited immediately with code: {process.ExitCode}\n");
            }
            else
            {
                File.AppendAllText(workflowLog, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Worker process {process.Id} is running\n");
            }

            // Track active Worker session
            ClaudeAgentCommand.AddWorkerSession(process.Id, agentType, taskTitle, requestFileName, process);

            LogWorkflowEvent($"[{counter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{requestFileName}]", messagesDirectory);

            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting worker monitoring for PID: {process.Id}\n");
                // Monitor for response file creation with FileSystemWatcher
                var result = await WaitForWorkerCompletionAsync(messagesDirectory, counter, agentType, process.Id, taskTitle, requestFileName);
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker monitoring completed with result: {result.Substring(0, Math.Min(100, result.Length))}...\n");
                return result;
            }
            finally
            {
                // Remove from active sessions and release workspace lock
                ClaudeAgentCommand.RemoveWorkerSession(process.Id);
                workspaceMutex.ReleaseMutex();
                workspaceMutex.Dispose();
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR in StartWorker: {ex.Message}\n");
            }
            catch
            {
            }

            if (workspaceMutex != null)
            {
                try
                {
                    workspaceMutex.ReleaseMutex();
                    workspaceMutex.Dispose();
                }
                catch
                {
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
        Console.Error.WriteLine("[MCP DEBUG] ListActiveWorkers called");
        return ClaudeAgentCommand.GetActiveWorkersList();
    }

    [McpServerTool]
    [Description("Stop a development agent that is taking too long or needs to be cancelled. Use when work needs to be interrupted.")]
    public static string KillWorker([Description("Process ID of Worker to terminate")] int processId)
    {
        return ClaudeAgentCommand.TerminateWorker(processId);
    }


    private static async Task<string> WaitForWorkerCompletionAsync(string messagesDirectory, int counter, string agentType, int processId, string taskTitle, string requestFileName)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var workflowLog = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, "workflow.log");
        File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WaitForWorkerCompletion started for PID: {processId}\n");

        // Agent creates "response.md" in their workspace, we'll rename it properly
        var agentWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName, agentType);
        var fixedResponseName = "response.md";
        var fixedResponsePath = Path.Combine(agentWorkspaceDir, fixedResponseName);

        var responseDetected = false;
        string? responseFilePath = null;
        var currentProcessId = processId;
        var restartCount = 0;

        // Watch for the fixed response file in the agent's workspace
        using var fileSystemWatcher = new FileSystemWatcher(agentWorkspaceDir, fixedResponseName);
        fileSystemWatcher.Created += (_, e) =>
        {
            File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FileSystemWatcher detected: {e.Name}\n");
            responseDetected = true;
            // We'll move and rename it later
        };
        fileSystemWatcher.EnableRaisingEvents = true;

        File.AppendAllText(workflowLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FileSystemWatcher monitoring: {agentWorkspaceDir} for file: {fixedResponseName}\n");

        var startTime = DateTime.Now;
        var lastActivity = DateTime.Now;
        var workerTimeout = agentType.Contains("reviewer")
            ? TimeSpan.FromMinutes(20)
            : TimeSpan.FromMinutes(10);  // Engineers: 10 min inactivity timeout
        var overallTimeout = TimeSpan.FromHours(2);

        while (!responseDetected && DateTime.Now - startTime < overallTimeout)
        {
            // Find most recent file modification
            var mostRecentFileTime = ClaudeAgentCommand.GetMostRecentFileModification();
            if (mostRecentFileTime > lastActivity)
            {
                lastActivity = mostRecentFileTime;
            }

            var timeSinceLastActivity = DateTime.Now - lastActivity;

            // If worker inactive for 5 minutes, restart it
            if (timeSinceLastActivity > workerTimeout)
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
                    var timeoutMinutes = agentType.Contains("reviewer") ? 20 : 10;
                    LogWorkerActivity($"WORKER RESTART: {agentType} inactive for {timeoutMinutes} minutes, restarted (attempt {restartCount})", messagesDirectory);
                    lastActivity = DateTime.Now;  // Reset activity after restart
                }
            }

            // Calculate smart wait time
            var timeUntilTimeout = workerTimeout - timeSinceLastActivity;
            var waitTime = timeUntilTimeout > TimeSpan.FromMinutes(1)
                ? TimeSpan.FromMinutes(1)
                : timeUntilTimeout;

            if (waitTime > TimeSpan.Zero)
            {
                await Task.Delay(waitTime);
            }
        }

        if (!responseDetected)
        {
            return $"Worker timeout after 2 hours (restarts: {restartCount})";
        }

        await Task.Delay(TimeSpan.FromSeconds(5)); // Grace period for file writing

        // Check if the fixed response file exists
        if (!File.Exists(fixedResponsePath))
        {
            return "Response file was detected but no longer exists";
        }

        // Generate proper response filename and move it to messages directory
        var shortTitle = string.Join("-", taskTitle.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(5))
            .ToLowerInvariant().Replace(".", "").Replace(",", "");
        var responseFileName = $"{counter:D4}.{agentType}.response.{shortTitle}.md";
        responseFilePath = Path.Combine(messagesDirectory, responseFileName);

        // Move the file to the correct location with proper naming
        File.Move(fixedResponsePath, responseFilePath);

        var description = Path.GetFileNameWithoutExtension(responseFileName).Split('.').Last().Replace('-', ' ');
        LogWorkflowEvent($"[{counter:D4}.{agentType}.response] Completed: '{description}' -> [{responseFileName}]", messagesDirectory);

        var responseContent = await File.ReadAllTextAsync(responseFilePath);

        return $"Task completed successfully by {agentType}.\n" +
               $"Task number: {counter:D4}\n" +
               $"Task title: {taskTitle}\n" +
               $"Request file: {requestFileName}\n" +
               $"Response file: {responseFileName}\n" +
               $"Restarts needed: {restartCount}\n\n" +
               $"Response content:\n{responseContent}";
    }

    private static bool IsWorkerProcessHealthy(int processId)
    {
        var processes = Process.GetProcesses();
        var workerProcess = processes.FirstOrDefault(p => p.Id == processId);
        return workerProcess is { HasExited: false };
    }

    private static async Task<(bool Success, int ProcessId, Process? Process, string ErrorMessage)> RestartWorker(string agentType, string messagesDirectory, string requestFileName, int attemptNumber)
    {
        var branchName = GitHelper.GetCurrentBranch();
        var branchWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName);
        var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDir, agentType);
        var restartRequestFile = Path.Combine(messagesDirectory, requestFileName);

        // Deterministic session management for restart
        var claudeSessionIdFile = Path.Combine(agentWorkspaceDirectory, ".claude-session-id");
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder
        };

        // Load base system prompt from .txt file for ALL agent types
        var systemPromptFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "worker-agent-system-prompts", $"{agentType}.txt");
        if (File.Exists(systemPromptFile))
        {
            var systemPromptText = await File.ReadAllTextAsync(systemPromptFile);
            systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();
            claudeArgs.Add("--append-system-prompt");
            claudeArgs.Add(systemPromptText);
        }

        // Add agent-specific restart nudge message with task context
        if (agentType == "tech-lead")
        {
            claudeArgs.Add("--append-system-prompt");
            claudeArgs.Add("You are the tech-lead. It looks like you stopped. Please ultrathink and evaluate how to continue coordinating the team.");
        }
        else
        {
            // Simplified restart message - no task numbers needed
            claudeArgs.Add("--append-system-prompt");
            claudeArgs.Add($"You are a {agentType} Worker. It looks like you stopped. " +
                         $"Please re-read the latest request file in the messages directory and continue working on it. " +
                         $"When you are ready to submit your response, create a file named 'response.md' in your current directory. " +
                         $"IMPORTANT: Always name your response file exactly 'response.md' - do NOT include any numbers or other naming.");
        }

        if (File.Exists(claudeSessionIdFile))
        {
            var claudeSessionId = await File.ReadAllTextAsync(claudeSessionIdFile);
            claudeArgs.Insert(0, "--resume");
            claudeArgs.Insert(1, claudeSessionId.Trim());
        }
        else
        {
            var newClaudeSessionId = Guid.NewGuid().ToString();
            await File.WriteAllTextAsync(claudeSessionIdFile, newClaudeSessionId);
            claudeArgs.Insert(0, "--session-id");
            claudeArgs.Insert(1, newClaudeSessionId);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", claudeArgs),
                WorkingDirectory = agentWorkspaceDirectory,
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

public record WorkerSession(
    int ProcessId,
    string AgentType,
    string TaskTitle,
    string RequestFileName,
    DateTime StartTime,
    Process Process
);
