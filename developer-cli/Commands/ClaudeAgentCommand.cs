using System.CommandLine;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ClaudeAgentCommand : Command
{
    public ClaudeAgentCommand() : base("claude-agent", "Launch Claude Code with session management")
    {
        SetAction(async _ => await ExecuteAsync());
    }

    private async Task ExecuteAsync()
    {
        try
        {
            // Check for optional LSP prerequisites (non-blocking)
            Prerequisite.Recommend(Prerequisite.TypeScriptLanguageServer);

            var workspace = new Workspace("tech-lead");
            var sessionIdFile = Path.Combine(workspace.AgentWorkspaceDirectory, ".claude-session-id");

            // Create workspace directory
            Directory.CreateDirectory(workspace.AgentWorkspaceDirectory);

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

            // Launch Claude Code (greet on new sessions only)
            var isNewSession = !File.Exists(sessionIdFile);
            await LaunchClaudeCode(workspace, isNewSession);

            // On exit: rename if already saved, else offer to save with name
            if (!File.Exists(sessionIdFile))
            {
            }
            else
            {
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
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Command execution failed: {ex.Message}");
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
        }
    }

    private async Task LaunchClaudeCode(Workspace workspace, bool isNewSession)
    {
        // Load system prompt (REQUIRED - throw if missing)
        if (!File.Exists(workspace.SystemPromptFile))
        {
            throw new FileNotFoundException($"System prompt file not found: {workspace.SystemPromptFile}");
        }

        var systemPromptText = await File.ReadAllTextAsync(workspace.SystemPromptFile);
        systemPromptText = systemPromptText.Replace('\n', ' ').Replace('\r', ' ').Replace("\"", "'").Trim();

        // Build Claude Code arguments
        var claudeArgs = new List<string>
        {
            "--settings", Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.json"),
            "--permission-mode", "bypassPermissions",
            "--append-system-prompt", systemPromptText
        };

        // Greet user on new sessions
        if (isNewSession)
        {
            claudeArgs.Add("--");
            claudeArgs.Add("What can you do?");
        }

        // Launch with session management from source code root
        var process = await StartClaudeCodeProcess(workspace, claudeArgs);

        // Wait for process to exit
        await process.WaitForExitAsync();
    }

    private async Task<Process> StartClaudeCodeProcess(Workspace workspace, List<string> additionalArgs)
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

        var process = new Process
        {
            StartInfo = BuildProcessStartInfo(args, workingDirectory)
        };

        process.Start();

        return process;
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
}

public class Workspace(string agentType, string? branch = null)
{
    // Define which agents are branch-agnostic
    private static readonly HashSet<string> BranchAgnosticAgents = ["tech-lead"];

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

    public string SystemPromptFile => Path.Combine(Configuration.SourceCodeFolder, ".claude", "agentic-workflow", "system-prompts", $"{AgentType}.txt");
}
