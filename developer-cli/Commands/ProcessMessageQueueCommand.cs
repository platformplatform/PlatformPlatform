using System.CommandLine;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ProcessMessageQueueCommand : Command
{
    public ProcessMessageQueueCommand() : base("claude-agent-process-message-queue", "Internal command used by Claude Code agent system - do not use directly")
    {
        var workspaceArgument = new Argument<string>("workspace-path", "Path to the agent workspace");

        AddArgument(workspaceArgument);

        this.SetHandler(ExecuteAsync, workspaceArgument);
    }

    private async Task ExecuteAsync(string workspacePath)
    {
        var agentName = Path.GetFileName(workspacePath);
        var featureName = Path.GetFileName(Path.GetDirectoryName(workspacePath)) ?? "unknown";
        var messageQueueDir = Path.Combine(workspacePath, "message-queue");

        // Minimal startup output

        var startTime = DateTime.Now;
        var endTime = startTime.AddHours(2);
        var parentProcessId = Environment.ProcessId;

        while (DateTime.Now < endTime)
        {
            try
            {
                // Check if parent process still exists
                try
                {
                    Process.GetProcessById(parentProcessId);
                }
                catch (ArgumentException)
                {
                    // Parent process is dead, exit
                    return;
                }

                Directory.CreateDirectory(messageQueueDir);
                var allFiles = Directory.GetFiles(messageQueueDir, "*.md")
                    .Where(f => !Path.GetFileName(f).StartsWith("keepalive_"))
                    .ToArray();

                if (allFiles.Length > 1)
                {
                    AnsiConsole.MarkupLine($"[red]⚠️ MULTIPLE TASKS DETECTED: {allFiles.Length} files found[/]");
                    AnsiConsole.MarkupLine("[red]SINGLE-TASK RULE VIOLATED[/]");
                }

                if (allFiles.Length > 0)
                {
                    await ProcessAllFilesAsync(allFiles, featureName, agentName);

                    // Exit immediately after showing files so Claude can process them
                    AnsiConsole.MarkupLine("[yellow]🔄 Exiting so Claude can process files...[/]");
                    return;
                }

                await Task.Delay(2000); // Check every 2 seconds
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]❌ Error: {ex.Message}[/]");
                await Task.Delay(5000);
            }
        }

        AnsiConsole.MarkupLine("[yellow]⏰ 2-hour cycle completed[/]");
    }

    private Task ProcessAllFilesAsync(string[] files, string featureName, string agentName)
    {
        // Just output the file paths for Claude to handle
        foreach (var file in files)
        {
            Console.WriteLine(file);
        }
        return Task.CompletedTask;
    }


}