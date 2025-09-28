using System.CommandLine;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ClaudeWorkerAgentCommand : Command
{
    private static readonly Dictionary<int, WorkerSession> ActiveWorkerSessions = new();
    private static readonly Lock WorkerSessionLock = new();
    private static bool IsMcpMode;
    private static string? SelectedAgentType;

    private CancellationTokenSource? _enterKeyListenerCts;

    public ClaudeWorkerAgentCommand() : base("claude-worker-agent", "Interactive Worker Host for agent development")
    {
        var agentTypeArgument = new Argument<string?>("agent-type", () => null)
        {
            Description = "Agent type to run (backend-engineer, frontend-engineer, backend-reviewer, frontend-reviewer, e2e-test-reviewer)"
        };
        agentTypeArgument.Arity = ArgumentArity.ZeroOrOne;

        var mcpOption = new Option<bool>("--mcp", "Run as MCP server for automated workflows");
        var resumeOption = new Option<bool>("--resume", "Resume specific coordinator session from workspace (only for coordinator agent type)");
        var continueOption = new Option<bool>("--continue", "Continue most recent conversation in main repo (only for coordinator agent type)");

        AddArgument(agentTypeArgument);
        AddOption(mcpOption);
        AddOption(resumeOption);
        AddOption(continueOption);

        this.SetHandler(ExecuteAsync, agentTypeArgument, mcpOption, resumeOption, continueOption);
    }

    private async Task ExecuteAsync(string? agentType, bool mcp, bool resume, bool continueSession)
    {
        try
        {
            IsMcpMode = mcp;
            SelectedAgentType = agentType;

            if (mcp)
            {
                // MCP server mode for automated workflows
                AnsiConsole.MarkupLine("[green]Starting MCP claude-agent-worker-host server...[/]");
                AnsiConsole.MarkupLine("[dim]Listening on stdio for MCP communication[/]");
                Console.Error.WriteLine("[MCP DEBUG] Server starting - tools should be available now");

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
            else
            {
                // Interactive mode (default)
                await RunInteractiveMode(agentType, resume, continueSession);
            }
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
                        "coordinator",
                        "backend-engineer",
                        "frontend-engineer",
                        "backend-reviewer",
                        "frontend-reviewer",
                        "e2e-test-reviewer"
                    )
            );
        }

        SelectedAgentType = agentType;
        var branch = GitHelper.GetCurrentBranch();

        // Create workspace and register agent
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);
        Directory.CreateDirectory(agentWorkspaceDirectory);

        // Setup workspace with latest rules, commands, and agent personality
        await WorkerMcpTools.SetupWorkerPrimingAsync(agentWorkspaceDirectory, agentType);

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

        // Ensure PID file is deleted on exit
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

        var agentBanner = new FigletText(displayName).Color(GetAgentColor(agentType));
        AnsiConsole.Write(agentBanner);

        var agentColor = GetAgentColor(agentType);

        AnsiConsole.WriteLine(); // Extra line for spacing

        // Special handling for coordinator - launch directly into Claude Code
        if (agentType == "coordinator")
        {
            AnsiConsole.MarkupLine($"[{agentColor}]Launching coordinator mode...[/]");
            await Task.Delay(2000);

            // Launch coordinator directly
            await LaunchCoordinatorAsync(agentType, branch, resume, continueSession);
            return;
        }

        // Create a clean waiting display without side borders
        var rule = new Rule("[bold]WAITING FOR TASKS[/]")
            .RuleStyle($"{agentColor}")
            .LeftJustified();
        AnsiConsole.Write(rule);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Branch: [{agentColor} bold]{branch}[/]");
        AnsiConsole.MarkupLine("Status: [dim]Press [bold white]ENTER[/] for manual control[/]");
        AnsiConsole.WriteLine();

        var bottomRule = new Rule().RuleStyle($"{agentColor} dim");
        AnsiConsole.Write(bottomRule);
        AnsiConsole.WriteLine();

        // Start watching for request files
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");
        Directory.CreateDirectory(messagesDirectory);

        await WatchForRequestsAsync(agentType, messagesDirectory, branch);
    }

    private async Task LaunchCoordinatorAsync(string agentType, string branch, bool resume, bool continueSession)
    {
        // Coordinator runs from source folder with full access to commands and agents
        var coordinatorArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "default",
            "/coordinator-mode"
        };

        // Add session flags if specified
        if (resume)
        {
            coordinatorArgs.Insert(0, "--resume");
        }
        else if (continueSession)
        {
            coordinatorArgs.Insert(0, "--continue");
        }
        // Default: Fresh session (no flags)

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", coordinatorArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = Configuration.SourceCodeFolder,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };

        process.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
        process.Start();
        await process.WaitForExitAsync();

        // Coordinator exited - clean up and show completion
        var agentColor = GetAgentColor(agentType);
        AnsiConsole.MarkupLine($"[{agentColor} bold]✓ Coordinator session ended[/]");
    }

    private static void CleanupPidFile(string pidFile)
    {
        if (File.Exists(pidFile))
        {
            File.Delete(pidFile);
        }
    }

    private async Task WatchForRequestsAsync(string agentType, string messagesDirectory, string branch)
    {
        using var fileSystemWatcher = new FileSystemWatcher(messagesDirectory)
        {
            Filter = $"*.{agentType}.request.*.md",
            EnableRaisingEvents = true
        };

        var completionSource = new TaskCompletionSource();

        fileSystemWatcher.Created += async (sender, e) =>
        {
            try
            {
                // Stop ENTER key listener during task processing to avoid keyboard interference
                _enterKeyListenerCts?.Cancel();

                await HandleIncomingRequest(e.FullPath, agentType, branch);

                // Restart ENTER key listener after task processing
                StartEnterKeyListener(agentType, branch);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error handling request: {ex.Message}[/]");
                RedrawWaitingDisplay(agentType, branch);
                // Restart ENTER key listener after error
                StartEnterKeyListener(agentType, branch);
            }
        };

        // Listen for ENTER key for manual control - restarts after each use
        StartEnterKeyListener(agentType, branch);

        // Wait indefinitely (until Ctrl+C)
        await completionSource.Task;
    }

    private void StartEnterKeyListener(string agentType, string branch)
    {
        _enterKeyListenerCts = new CancellationTokenSource();
        _ = Task.Run(async () =>
            {
                while (!_enterKeyListenerCts.Token.IsCancellationRequested)
                {
                    try
                    {
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Enter)
                        {
                            AnsiConsole.Clear();
                            AnsiConsole.MarkupLine("[yellow]Manual control activated[/]");

                            // Launch manual session
                            await LaunchManualClaudeSession(agentType, branch);

                            // Return to waiting display
                            RedrawWaitingDisplay(agentType, branch);

                            // Restart the ENTER key listener for next use
                            StartEnterKeyListener(agentType, branch);
                            break; // Exit this instance of the listener
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch
                    {
                        break;
                    }
                }
            }
        );
    }

    private async Task LaunchManualClaudeSession(string agentType, string branch)
    {
        var agentWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, agentType);

        var manualArgs = new List<string>
        {
            "--continue",
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions"
        };

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
        await Task.Delay(500); // Let file write complete
        var taskContent = await File.ReadAllTextAsync(requestFile);
        var firstLine = taskContent.Split('\n').FirstOrDefault()?.Trim() ?? "Task";

        AnsiConsole.MarkupLine($"[dim]Task: {Markup.Escape(firstLine)}[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[dim]Launching Claude Code in 3 seconds...[/]");
        await Task.Delay(3000);

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

        // Always setup workspace with latest rules and commands
        await WorkerMcpTools.SetupWorkerPrimingAsync(agentWorkspaceDirectory, agentType);

        // Extract request file name components for response file
        var requestFileName = Path.GetFileName(requestFile);
        var match = Regex.Match(requestFileName, @"^(\d+)\.([^.]+)\.request\.(.+)\.md$");
        var counter = match.Groups[1].Value;
        var shortTitle = match.Groups[3].Value;
        var responseFileName = $"{counter}.{agentType}.response.{shortTitle}.md";

        // Configure args based on agent type
        var isCoordinator = agentType == "coordinator";

        if (isCoordinator)
        {
            // Coordinator launches directly with /coordinator-mode (no request file needed)
            var coordinatorArgs = new List<string>
            {
                "--continue",
                "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                "--add-dir", Configuration.SourceCodeFolder,
                "--permission-mode", "default",
                "/coordinator-mode"
            };

            var coordinatorProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = string.Join(" ", coordinatorArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                    WorkingDirectory = Configuration.SourceCodeFolder,
                    UseShellExecute = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                }
            };

            coordinatorProcess.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
            coordinatorProcess.Start();
            await coordinatorProcess.WaitForExitAsync();

            // If --continue failed, try fresh coordinator session
            if (coordinatorProcess.ExitCode != 0)
            {
                AnsiConsole.MarkupLine("[yellow]No existing conversation found, starting fresh coordinator session...[/]");
                await Task.Delay(TimeSpan.FromSeconds(1));

                var freshCoordinatorArgs = new List<string>
                {
                    "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                    "--add-dir", Configuration.SourceCodeFolder,
                    "--permission-mode", "default",
                    "/coordinator-mode"
                };

                coordinatorProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "claude",
                        Arguments = string.Join(" ", freshCoordinatorArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                        WorkingDirectory = Configuration.SourceCodeFolder,
                        UseShellExecute = false,
                        RedirectStandardInput = false,
                        RedirectStandardOutput = false,
                        RedirectStandardError = false
                    }
                };

                coordinatorProcess.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
                coordinatorProcess.Start();
            }

            return coordinatorProcess;
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
                var reviewPrdPath = ExtractPathAfterKey(taskContent, "PRD:");
                var reviewProductIncrementPath = ExtractPathAfterKey(taskContent, "Product Increment:");
                var reviewTaskNumber = ExtractTextAfterKey(taskContent, "Task:");
                var reviewRequestFilePath = ExtractPathAfterKey(taskContent, "Request:");
                var reviewResponseFilePath = ExtractPathAfterKey(taskContent, "Response:");

                finalPrompt = $"/review-task {reviewPrdPath} {reviewProductIncrementPath} {reviewTaskNumber} {reviewRequestFilePath} {reviewResponseFilePath}";
            }
            else
            {
                // Extract paths for engineers
                var implPrdPath = ExtractPathAfterKey(taskContent, "PRD:");
                var implProductIncrementPath = ExtractPathAfterKey(taskContent, "from ");
                var implTaskNumber = ExtractTextBetweenQuotes(taskContent, "task ");

                finalPrompt = $"/implement-task {implPrdPath} {implProductIncrementPath} {implTaskNumber}";
            }
        }
        else
        {
            finalPrompt = $"Read {requestFile} and follow your {workflowName} exactly";
        }

        var systemPrompt = $"You are a {agentType} Worker. When done, create response file: {messagesDirectory}/{responseFileName}.tmp then rename to {messagesDirectory}/{responseFileName}";

        // Try --continue first, fallback to fresh session if no conversation found
        var continueArgs = new List<string>
        {
            "--continue",
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--permission-mode", "bypassPermissions",
            "--append-system-prompt", systemPrompt,
            finalPrompt
        };

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", continueArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                WorkingDirectory = agentWorkspaceDirectory,
                UseShellExecute = false,
                RedirectStandardInput = false,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            }
        };

        process.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");

        process.Start();

        // Give --continue a moment to fail if no conversation exists
        await Task.Delay(1000);

        // If --continue failed (no conversation found), try without --continue
        if (process.HasExited && process.ExitCode != 0)
        {
            AnsiConsole.MarkupLine("[yellow]Starting new session...[/]");
            await Task.Delay(TimeSpan.FromSeconds(1));

            var freshArgs = new List<string>
            {
                "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                "--add-dir", Configuration.SourceCodeFolder,
                "--permission-mode", "bypassPermissions",
                "--append-system-prompt", systemPrompt,
                finalPrompt
            };

            process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = string.Join(" ", freshArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg)),
                    WorkingDirectory = agentWorkspaceDirectory,
                    UseShellExecute = false,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = false
                }
            };

            process.StartInfo.EnvironmentVariables.Remove("CLAUDECODE");
            process.Start();
            // Return the running fresh process for monitoring
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Continuing existing session...[/]");
        }

        return process;
    }

    private async Task WaitForResponseAndKillClaude(string requestFile, string agentType, string branch, Process claudeProcess)
    {
        var messagesDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branch, "messages");

        // Extract request file name components for response file
        var requestFileName = Path.GetFileName(requestFile);
        var match = Regex.Match(requestFileName, @"^(\d+)\.([^.]+)\.request\.(.+)\.md$");
        var counter = match.Groups[1].Value;
        var shortTitle = match.Groups[3].Value;
        var responseFileName = $"{counter}.{agentType}.response.{shortTitle}.md";
        var responseFilePath = Path.Combine(messagesDirectory, responseFileName);

        // Wait for response file to appear (not .tmp, the final renamed file)
        AnsiConsole.MarkupLine($"[grey]Waiting for response file: {responseFilePath}[/]");
        var startTime = DateTime.Now;
        var timeout = TimeSpan.FromMinutes(30);

        while (!File.Exists(responseFilePath))
        {
            if (DateTime.Now - startTime > timeout)
            {
                AnsiConsole.MarkupLine($"[red]Timeout waiting for response file: {responseFilePath}[/]");
                // Timeout - kill Claude anyway
                break;
            }

            await Task.Delay(500); // Check every 500ms
        }

        if (File.Exists(responseFilePath))
        {
            var agentColor = GetAgentColor(agentType);
            AnsiConsole.MarkupLine($"[{agentColor} bold]✓ Response file created[/]");

            // Give Claude 60 seconds to finish up
            AnsiConsole.MarkupLine("[grey]Giving Claude Code 60 seconds to finish...[/]");
            await Task.Delay(TimeSpan.FromSeconds(60));
        }

        // Kill the Claude Code process
        if (claudeProcess != null && !claudeProcess.HasExited)
        {
            AnsiConsole.MarkupLine("[grey]Terminating Claude Code session...[/]");
            try
            {
                claudeProcess.Kill();
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            catch
            {
                // Process might have already exited
            }
        }
    }

    private void RedrawWaitingDisplay(string agentType, string branch)
    {
        AnsiConsole.Clear();

        var displayName = GetAgentDisplayName(agentType);
        var agentBanner = new FigletText(displayName).Color(GetAgentColor(agentType));
        AnsiConsole.Write(agentBanner);

        var agentColor = GetAgentColor(agentType);

        AnsiConsole.WriteLine();

        var rule = new Rule("[bold]WAITING FOR TASKS[/]")
            .RuleStyle($"{agentColor}")
            .LeftJustified();
        AnsiConsole.Write(rule);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Branch: [{agentColor} bold]{branch}[/]");
        AnsiConsole.MarkupLine("Status: [dim]Press [bold white]ENTER[/] for manual control[/]");
        AnsiConsole.WriteLine();

        var bottomRule = new Rule().RuleStyle($"{agentColor} dim");
        AnsiConsole.Write(bottomRule);
        AnsiConsole.WriteLine();
    }

    private static string GetAgentDisplayName(string agentType)
    {
        return agentType switch
        {
            "coordinator" => "Coordinator",
            "backend-engineer" => "Backend Engineer",
            "frontend-engineer" => "Frontend Engineer",
            "backend-reviewer" => "Backend Reviewer",
            "frontend-reviewer" => "Frontend Reviewer",
            "e2e-test-reviewer" => "E2E Test Reviewer",
            _ => agentType
        };
    }

    private static Color GetAgentColor(string agentType)
    {
        return agentType switch
        {
            "coordinator" => Color.Red,
            "backend-engineer" => Color.Green,
            "frontend-engineer" => Color.Blue,
            "backend-reviewer" => Color.Yellow,
            "frontend-reviewer" => Color.Orange3,
            "e2e-test-reviewer" => Color.Purple,
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
        var endIndex = content.IndexOfAny(['\r', '\n', ' '], startIndex);
        if (endIndex == -1) endIndex = content.Length;

        return content.Substring(startIndex, endIndex - startIndex).Trim();
    }

    private static string ExtractTextAfterKey(string content, string key)
    {
        var keyIndex = content.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (keyIndex == -1) return "";

        var startIndex = keyIndex + key.Length;
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
        "coordinator", "backend-engineer", "frontend-engineer",
        "backend-reviewer", "frontend-reviewer", "e2e-test-reviewer"
    };

    [McpServerTool]
    [Description("Delegate a development task to a specialized agent. Use this when you need backend development, frontend work, or code review. The agent will work autonomously and return results.")]
    public static async Task<string> StartWorker(
        [Description("Worker type (backend-engineer, frontend-engineer, backend-reviewer, frontend-reviewer, e2e-test-reviewer)")]
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
        var debugLog = "/Users/thomasjespersen/Developer/PlatformPlatform/.claude/mcp-debug.log";

        try
        {
            File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] StartWorker called: agentType={agentType}, taskTitle={taskTitle}\n");
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

            var branchName = GitHelper.GetCurrentBranch();

            // Setup workspace paths
            var branchWorkspaceDirectory = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName);
            var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDirectory, agentType);
            var messagesDirectory = Path.Combine(branchWorkspaceDirectory, "messages");
            var pidFile = Path.Combine(agentWorkspaceDirectory, ".pid");

            // Debug: Log the PID file path we're checking
            File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Checking for PID file: {pidFile}\n");
            File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] PID file exists: {File.Exists(pidFile)}\n");

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

                            // Log task start
                            LogWorkflowEvent($"[{taskCounter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{taskRequestFileName}]", messagesDirectory);

                            // Wait for the response file to be created (atomic rename from .tmp)
                            var responsePattern = $"{taskCounter:D4}.{agentType}.response.{taskShortTitle}.md";
                            var responseFile = Path.Combine(messagesDirectory, responsePattern);

                            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Waiting for interactive agent to complete: {responsePattern}[/]");

                            // Poll for response file with timeout (30 minutes max)
                            var startTime = DateTime.Now;
                            var timeout = TimeSpan.FromMinutes(30);

                            // Wait for final renamed file (not .tmp)
                            while (!File.Exists(responseFile))
                            {
                                if (DateTime.Now - startTime > timeout)
                                {
                                    throw new TimeoutException($"Interactive {agentType} did not complete task within 30 minutes");
                                }

                                await Task.Delay(500); // Check every 500ms
                            }

                            AnsiConsole.MarkupLine("[grey][[MCP DEBUG]] Response file detected, reading content...[/]");

                            // Read response content immediately - file is complete via atomic rename
                            var responseContent = await File.ReadAllTextAsync(responseFile);

                            // Log task completion
                            LogWorkflowEvent($"[{taskCounter:D4}.{agentType}.response] Completed: '{taskTitle}' -> [{responsePattern}]", messagesDirectory);

                            AnsiConsole.MarkupLine("[grey][[MCP DEBUG]] Interactive agent completed task[/]");
                            return responseContent;
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

            // Check if this is a new workspace
            var isNewWorkspace = !Directory.Exists(agentWorkspaceDirectory);
            Directory.CreateDirectory(agentWorkspaceDirectory);

            // Copy and customize CLAUDE.md for Worker priming
            await SetupWorkerPrimingAsync(agentWorkspaceDirectory, agentType);

            var claudeArgs = new[]
            {
                "--continue",
                "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
                "--add-dir", Configuration.SourceCodeFolder,
                "--permission-mode", "bypassPermissions",
                "--append-system-prompt", $"You are a {agentType} Worker. Process task in shared messages: {requestFile}",
                $"Read {requestFile}"
            };

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
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Environment: CLAUDECODE removed\n");
            }
            catch
            {
            }

            try
            {
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Full arguments array: [{string.Join(", ", claudeArgs.Select(arg => $"'{arg}'"))}]\n");
            }
            catch
            {
            }

            try
            {
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting Claude Code worker in: {agentWorkspaceDirectory}\n");
            }
            catch
            {
            }

            var quotedArgs = string.Join(" ", claudeArgs.Select(arg => arg.Contains(" ") ? $"\"{arg}\"" : arg));
            try
            {
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Command: claude {quotedArgs}\n");
            }
            catch
            {
            }

            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Starting Claude Code worker process in: {agentWorkspaceDirectory}[/]");
            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Claude args: {string.Join(" ", claudeArgs)}[/]");

            process.Start();

            try
            {
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker process started with PID: {process.Id}\n");
            }
            catch
            {
            }

            AnsiConsole.MarkupLine($"[grey][[MCP DEBUG]] Worker process started with PID: {process.Id}[/]");

            // Log what Claude Code actually receives as input
            try
            {
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Claude input should be: {claudeArgs.Last()}\n");
            }
            catch
            {
            }

            // Check if process exits immediately
            await Task.Delay(3000);
            if (process.HasExited)
            {
                File.AppendAllText(debugLog, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] ERROR: Worker process {process.Id} exited immediately with code: {process.ExitCode}\n");
            }
            else
            {
                File.AppendAllText(debugLog, $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] Worker process {process.Id} is running\n");
            }

            // Track active Worker session
            ClaudeWorkerAgentCommand.AddWorkerSession(process.Id, agentType, taskTitle, requestFileName, process);

            LogWorkflowEvent($"[{counter:D4}.{agentType}.request] Started: '{taskTitle}' -> [{requestFileName}]", messagesDirectory);

            try
            {
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting worker monitoring for PID: {process.Id}\n");
                // Monitor for response file creation with FileSystemWatcher
                var result = await WaitForWorkerCompletionAsync(messagesDirectory, counter, agentType, process.Id, taskTitle, requestFileName);
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Worker monitoring completed with result: {result.Substring(0, Math.Min(100, result.Length))}...\n");
                return result;
            }
            finally
            {
                // Remove from active sessions and release workspace lock
                ClaudeWorkerAgentCommand.RemoveWorkerSession(process.Id);
                workspaceMutex.ReleaseMutex();
                workspaceMutex.Dispose();
            }
        }
        catch (Exception ex)
        {
            try
            {
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR in StartWorker: {ex.Message}\n");
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
        return ClaudeWorkerAgentCommand.GetActiveWorkersList();
    }

    [McpServerTool]
    [Description("Stop a development agent that is taking too long or needs to be cancelled. Use when work needs to be interrupted.")]
    public static string KillWorker([Description("Process ID of Worker to terminate")] int processId)
    {
        return ClaudeWorkerAgentCommand.TerminateWorker(processId);
    }

    public static async Task SetupWorkerPrimingAsync(string agentWorkspaceDirectory, string agentType)
    {
        // Copy root repository CLAUDE.md to Worker workspace
        var rootClaudeMd = Path.Combine(Configuration.SourceCodeFolder, "CLAUDE.md");
        var workerClaudeMd = Path.Combine(agentWorkspaceDirectory, "CLAUDE.md");

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

        // Create symlink to .claude directory for always-current commands, agents, and settings (only if doesn't exist)
        var workerClaudeDir = Path.Combine(agentWorkspaceDirectory, ".claude");
        var rootClaudeDir = Path.Combine(Configuration.SourceCodeFolder, ".claude");

        // Only create symlink if it doesn't exist
        if (!Directory.Exists(workerClaudeDir) && !File.Exists(workerClaudeDir) && Directory.Exists(rootClaudeDir))
        {
            try
            {
                AnsiConsole.MarkupLine($"[grey]Creating symlink: {workerClaudeDir} -> {rootClaudeDir}[/]");

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

                AnsiConsole.MarkupLine("[green]Symlink created successfully[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Symlink creation failed: {ex.Message}[/]");
                // Fallback to copying if symlink creation fails
                Directory.CreateDirectory(Path.Combine(workerClaudeDir, "commands"));
                Directory.CreateDirectory(Path.Combine(workerClaudeDir, "agents"));

                // Copy commands
                foreach (var file in Directory.GetFiles(Path.Combine(rootClaudeDir, "commands"), "*.md"))
                {
                    var fileName = Path.GetFileName(file);
                    await File.WriteAllTextAsync(Path.Combine(workerClaudeDir, "commands", fileName), await File.ReadAllTextAsync(file));
                }

                // Copy agents
                foreach (var file in Directory.GetFiles(Path.Combine(rootClaudeDir, "agents"), "*.md"))
                {
                    var fileName = Path.GetFileName(file);
                    await File.WriteAllTextAsync(Path.Combine(workerClaudeDir, "agents", fileName), await File.ReadAllTextAsync(file));
                }
            }
        }
    }

    private static async Task<string> WaitForWorkerCompletionAsync(string messagesDirectory, int counter, string agentType, int processId, string taskTitle, string requestFileName)
    {
        var debugLog = "/Users/thomasjespersen/Developer/PlatformPlatform/.claude/mcp-debug.log";
        File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WaitForWorkerCompletion started for PID: {processId}\n");
        var responsePattern = $"{counter:D4}.{agentType}.response.*.md";
        var responseDetected = false;
        string? responseFilePath = null;
        var currentProcessId = processId;
        var restartCount = 0;

        using var fileSystemWatcher = new FileSystemWatcher(messagesDirectory, responsePattern);
        fileSystemWatcher.Created += (_, e) =>
        {
            File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FileSystemWatcher detected: {e.Name}\n");
            responseDetected = true;
            responseFilePath = e.FullPath;
        };
        fileSystemWatcher.EnableRaisingEvents = true;

        File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] FileSystemWatcher monitoring: {messagesDirectory} for pattern: {responsePattern}\n");

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

        // Example: 0001.backend-engineer.response.Added-JWT-auth-and-4-tests.md
        var responseFileName = Path.GetFileName(responseFilePath);

        var description = Path.GetFileNameWithoutExtension(responseFileName).Split('.').Last().Replace('-', ' ');
        LogWorkflowEvent($"[{counter:D4}.{agentType}.response] Completed: '{description}' -> [{responseFileName}]", messagesDirectory);

        var responseContent = await File.ReadAllTextAsync(responseFilePath);
        return $"Worker completed task '{taskTitle}'.\nRequest: {requestFileName}\nResponse: {responseFileName}\nRestarts: {restartCount}\n\nResponse content:\n{responseContent}";
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
        var branchName = GitHelper.GetCurrentBranch();
        var branchWorkspaceDir = Path.Combine(Configuration.SourceCodeFolder, ".workspace", "agent-workspaces", branchName);
        var agentWorkspaceDirectory = Path.Combine(branchWorkspaceDir, agentType);
        var restartRequestFile = Path.Combine(messagesDirectory, requestFileName);

        var claudeArgs = new[]
        {
            "--continue",
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--add-dir", Configuration.SourceCodeFolder,
            "--append-system-prompt", $"You are a {agentType} Worker. Restart attempt #{attemptNumber}. Process task: {restartRequestFile}"
        };

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
        ClaudeWorkerAgentCommand.RemoveWorkerSession(oldProcessId);
        ClaudeWorkerAgentCommand.AddWorkerSession(newProcessId, agentType, taskTitle, requestFileName, newProcess);
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
