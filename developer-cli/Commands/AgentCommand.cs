using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class AgentCommand : Command
{
    public AgentCommand() : base("claude-agent", "Start a Claude AI agent with multi-agent communication capabilities")
    {
        var agentNameArgument = new Argument<string>("agent-name", "Name of the agent (backend, frontend, coordinator, etc.)");
        var featureNameOption = new Option<string>("--feature", "Name of the feature (defaults to current git branch)");
        var bypassPermissionsOption = new Option<bool>("--bypass-permissions", "Start Claude with bypassed permissions for autonomous operation");
        var colorOption = new Option<bool>("--color", "Enable terminal background colors for agent identification");

        AddArgument(agentNameArgument);
        AddOption(featureNameOption);
        AddOption(bypassPermissionsOption);
        AddOption(colorOption);

        this.SetHandler(ExecuteAsync, agentNameArgument, featureNameOption, bypassPermissionsOption, colorOption);
    }

    private async Task ExecuteAsync(string agentName, string? featureName, bool bypassPermissions, bool useColor)
    {
        // Available agent profiles with role-specific rules
        var agentProfiles = new Dictionary<string, (string Role, string Description, string RulesPath, string AgentProfilePath)>
        {
            ["backend"] = ("Senior Backend Engineer", "Expert in system architecture, API design, and backend optimization", "backend", ""),
            ["frontend"] = ("Frontend Engineer", "Specialist in UI/UX development and frontend best practices", "frontend", ""),
            ["coordinator"] = ("Project Coordinator", "Multi-agent orchestration and task management", "", "coordinator.md"),
            ["security"] = ("Security Expert", "Cybersecurity specialist focused on application security", "", ""),
            ["backend-reviewer"] = ("Backend Code Reviewer", "Specialist in backend code quality and architecture review", "backend", "backend-code-reviewer.md"),
            ["frontend-reviewer"] = ("Frontend Code Reviewer", "Specialist in frontend code quality and UI/UX review", "frontend", "frontend-code-reviewer.md"),
            ["devops"] = ("DevOps Engineer", "Infrastructure and deployment specialist", "", "")
        };

        // Normalize agent name (case insensitive)
        var normalizedAgentName = NormalizeAgentName(agentName);

        // Use git branch if no feature specified
        var normalizedFeatureName = string.IsNullOrEmpty(featureName)
            ? GetCurrentGitBranch()
            : featureName.ToLowerInvariant().Replace(" ", "-");

        if (!agentProfiles.ContainsKey(normalizedAgentName))
        {
            AnsiConsole.MarkupLine($"[red]Unknown agent: {agentName}[/]");
            AnsiConsole.MarkupLine("[yellow]Available agents:[/]");
            foreach (var (name, (role, desc, _, _)) in agentProfiles)
            {
                AnsiConsole.MarkupLine($"  [green]{name}[/] - {role}");
                AnsiConsole.MarkupLine($"    [dim]{desc}[/]");
            }

            return;
        }

        var (agentRole, agentDescription, rulesPath, agentProfilePath) = agentProfiles[normalizedAgentName];

        AnsiConsole.MarkupLine($"[dim]Feature: {normalizedFeatureName}[/]");
        AnsiConsole.MarkupLine($"[dim]Agent: {normalizedAgentName}[/]");

        // Create feature-scoped agent workspace
        var agentWorkspace = Path.Combine(Configuration.SourceCodeFolder, ".claude", "agent-workspaces", normalizedFeatureName, normalizedAgentName);
        Directory.CreateDirectory(agentWorkspace);

        // Create agent workspace and configuration
        Directory.CreateDirectory(agentWorkspace);

        // Kill any existing CLI processes for this agent
        KillExistingProcesses(normalizedFeatureName, normalizedAgentName);

        // Copy global commands and agent-specific commands
        await CopyCommandsAndRulesAsync(Configuration.SourceCodeFolder, agentWorkspace);

        // Create agent priming file with role-specific rules
        await CreateAgentPrimingAsync(agentWorkspace, normalizedAgentName, agentRole, rulesPath, agentProfilePath);

        // Copy coordinator-specific files
        if (normalizedAgentName == "coordinator")
        {
            await CopyCoordinatorFilesAsync(agentWorkspace);
        }

        // Create message queue directory for this agent
        var messageQueueDir = Path.Combine(agentWorkspace, "message-queue");
        Directory.CreateDirectory(messageQueueDir);

        // Create agent configuration file
        var configPath = Path.Combine(agentWorkspace, "config.json");
        await File.WriteAllTextAsync(configPath, JsonSerializer.Serialize(new
                {
                    name = normalizedAgentName,
                    role = agentRole,
                    workspace = agentWorkspace,
                    feature = normalizedFeatureName
                }, new JsonSerializerOptions { WriteIndented = true }
            )
        );

        AnsiConsole.MarkupLine($"[dim]Agent workspace created: {agentWorkspace}[/]");

        // Start Claude with auto-monitor command (coordinator gets startup check)
        var claudeArgs = normalizedAgentName == "coordinator"
            ? new List<string> { "--continue", "Check message queue and follow coordinator workflow" }
            : new List<string> { "--continue", "/monitor" };
        if (bypassPermissions)
        {
            claudeArgs.Add("--permission-mode bypassPermissions");
        }

        var claudeProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "claude",
                Arguments = string.Join(" ", claudeArgs),
                WorkingDirectory = agentWorkspace,
                UseShellExecute = false
            }
        };

        // Set terminal title
        Console.Title = $"{normalizedAgentName} - {normalizedFeatureName}";

        // Set background colors if enabled
        if (useColor)
        {
            var bgColor = normalizedAgentName switch
            {
                "backend" => "\x1b[48;5;17m", // Very dark blue
                "backend-reviewer" => "\x1b[48;5;18m", // Dark blue (lighter than backend)
                "frontend" => "\x1b[48;5;22m", // Very dark green
                "frontend-reviewer" => "\x1b[48;5;28m", // Dark green (lighter than frontend)
                "coordinator" => "\x1b[48;5;55m", // Dark purple
                "security" => "\x1b[48;5;52m", // Dark red
                "devops" => "\x1b[48;5;240m", // Dark gray
                _ => "\x1b[48;5;0m" // Black (default)
            };

            Console.Write($"{bgColor}\x1b[2J\x1b[H");
        }

        // Start keep-alive background worker
        var cts = new CancellationTokenSource();
        var keepAliveTask = Task.Run(() => KeepAliveWorker(agentWorkspace, cts.Token));

        claudeProcess.Start();

        await claudeProcess.WaitForExitAsync();

        // Stop keep-alive when Claude exits
        cts.Cancel();

        // If Claude exited quickly, it might have failed to continue - try starting fresh
        if (claudeProcess.ExitCode != 0)
        {
            var freshArgs = normalizedAgentName == "coordinator"
                ? new List<string>()
                : new List<string> { "/monitor" };
            if (bypassPermissions)
            {
                freshArgs.Add("--permission-mode bypassPermissions");
            }

            var freshProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "claude",
                    Arguments = string.Join(" ", freshArgs),
                    WorkingDirectory = agentWorkspace,
                    UseShellExecute = false
                }
            };

            freshProcess.Start();
            await freshProcess.WaitForExitAsync();
        }
    }

    private static string NormalizeAgentName(string input)
    {
        return input.ToLowerInvariant().Replace(" ", "-");
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

    private static void KillExistingProcesses(string featureName, string agentName)
    {
        try
        {
            var workspacePath = $".claude/agent-workspaces/{featureName}/{agentName}";

            var processes = Process.GetProcesses()
                .Where(p =>
                    {
                        try
                        {
                            return p.ProcessName.Contains("pp") &&
                                   p.StartInfo.Arguments.Contains("claude-agent-process-message-queue") &&
                                   p.StartInfo.Arguments.Contains(workspacePath);
                        }
                        catch
                        {
                            return false;
                        }
                    }
                )
                .ToList();

            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                    AnsiConsole.MarkupLine($"[yellow]Killed existing process: {process.Id}[/]");
                }
                catch
                {
                    // Process might already be dead
                }
            }

            // Alternative: Kill by command line match
            var killProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pkill",
                    Arguments = $"-f \"claude-agent-process-message-queue.*{featureName}/{agentName}$\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            killProcess.Start();
            killProcess.WaitForExit();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[dim]Cleanup warning: {ex.Message}[/]");
        }
    }

    private static async Task KeepAliveWorker(string agentWorkspace, CancellationToken cancellationToken)
    {
        var messageQueue = Path.Combine(agentWorkspace, "message-queue");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);

                if (cancellationToken.IsCancellationRequested) break;

                // Create keep-alive message in own queue to prevent timeout
                Directory.CreateDirectory(messageQueue);

                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var keepAliveFile = Path.Combine(messageQueue, $"keepalive_{timestamp}.md");

                var content = $"""
                               ---
                               # Keep-alive - {DateTime.Now}
                               Stand by
                               ---
                               """;

                await File.WriteAllTextAsync(keepAliveFile, content, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancelled
        }
        catch
        {
            // Ignore errors
        }
    }

    private static async Task CopyCoordinatorFilesAsync(string agentWorkspace)
    {
        var agentClaudeDir = Path.Combine(agentWorkspace, ".claude");
        var agentsTargetDir = Path.Combine(agentClaudeDir, "agents");
        Directory.CreateDirectory(agentsTargetDir);

        // Copy quality-gate-committer.md specifically for coordinator
        var qualityGateFile = Path.Combine(Configuration.SourceCodeFolder, ".claude", "agents", "quality-gate-committer.md");
        var targetFile = Path.Combine(agentsTargetDir, "quality-gate-committer.md");

        if (File.Exists(qualityGateFile))
        {
            await File.WriteAllTextAsync(targetFile, await File.ReadAllTextAsync(qualityGateFile));
        }
    }

    private static async Task CreateAgentPrimingAsync(string agentWorkspace, string agentName, string agentRole, string rulesPath, string agentProfilePath)
    {
        var primingContent = $"""
                              # Agent: {agentName}
                              Role: {agentRole}

                              You are an autonomous agent in a multi-agent system. You MUST continuously monitor for messages and process them.

                              """;

        // Add role-specific rules reference
        if (!string.IsNullOrEmpty(rulesPath))
        {
            primingContent += $"""
                               ## Your Rules
                               You MUST follow ALL rules in the `.claude/rules/{rulesPath}/` directory.
                               These rules are critical for your role as {agentRole}.

                               """;
        }

        // Add agent profile reference
        if (!string.IsNullOrEmpty(agentProfilePath))
        {
            primingContent += $"""
                               ## Your Profile
                               You MUST follow the guidelines in `.claude/agents/{agentProfilePath}`.
                               This defines your specific responsibilities and standards.

                               """;
        }

        primingContent += """
                          ## Your Workflow
                          1. Run `/monitor` to start autonomous operation
                          2. Process incoming requests with full context awareness
                          3. Apply your role-specific expertise and rules
                          4. Send detailed responses based on your specialization

                          You are monitoring continuously for messages. Stay in character as this specific agent.
                          """;

        await File.WriteAllTextAsync(Path.Combine(agentWorkspace, "AGENT_PROFILE.md"), primingContent);
    }

    private static async Task CopyCommandsAndRulesAsync(string platformPlatformRoot, string agentWorkspace)
    {
        var agentClaudeDir = Path.Combine(agentWorkspace, ".claude");

        // ALWAYS refresh - delete existing .claude directory and recreate
        if (Directory.Exists(agentClaudeDir))
        {
            Directory.Delete(agentClaudeDir, true);
        }

        Directory.CreateDirectory(agentClaudeDir);

        // Copy global commands from .claude/commands/ to agent workspace
        var globalCommandsSource = Path.Combine(platformPlatformRoot, ".claude", "commands");
        var agentCommandsTarget = Path.Combine(agentClaudeDir, "commands");

        if (Directory.Exists(globalCommandsSource))
        {
            await CopyDirectoryAsync(globalCommandsSource, agentCommandsTarget);
        }

        // Copy agent-specific commands from .claude/agent-commands/ to agent workspace
        var agentCommandsSource = Path.Combine(platformPlatformRoot, ".claude", "agent-commands");

        if (Directory.Exists(agentCommandsSource))
        {
            await CopyDirectoryAsync(agentCommandsSource, agentCommandsTarget, true);
        }

        // Copy rules if they exist
        var rulesSource = Path.Combine(platformPlatformRoot, ".claude", "rules");
        var rulesTarget = Path.Combine(agentClaudeDir, "rules");

        if (Directory.Exists(rulesSource))
        {
            await CopyDirectoryAsync(rulesSource, rulesTarget);
        }

        // Copy hooks if they exist
        var hooksSource = Path.Combine(platformPlatformRoot, ".claude", "hooks");
        var hooksTarget = Path.Combine(agentClaudeDir, "hooks");

        if (Directory.Exists(hooksSource))
        {
            await CopyDirectoryAsync(hooksSource, hooksTarget);

            // Make hook files executable (cross-platform)
            foreach (var hookFile in Directory.GetFiles(hooksTarget, "*", SearchOption.AllDirectories))
            {
                if (OperatingSystem.IsWindows())
                {
                    // Windows doesn't need explicit execute permissions
                }
                else
                {
                    File.SetUnixFileMode(hookFile, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
                }
            }
        }

        // Copy settings if they exist
        var settingsSource = Path.Combine(platformPlatformRoot, ".claude", "settings.json");
        var settingsTarget = Path.Combine(agentClaudeDir, "settings.json");

        if (File.Exists(settingsSource))
        {
            File.Copy(settingsSource, settingsTarget, true);
        }
    }

    private static async Task CopyDirectoryAsync(string sourceDir, string targetDir, bool overwrite = false)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(sourceDir, file);
            var targetFile = Path.Combine(targetDir, relativePath);

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            if (overwrite || !File.Exists(targetFile))
            {
                await File.WriteAllTextAsync(targetFile, await File.ReadAllTextAsync(file));
            }
        }
    }
}
