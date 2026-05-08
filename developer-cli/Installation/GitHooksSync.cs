using System.Diagnostics;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Installation;

// Copies hooks committed under developer-cli/git-hooks/ into the main repo's .git/hooks/ directory.
// Gated on persisted user consent so contributors are asked once and never again. Called from
// InstallCommand on first install (with forcePrompt: true) and from ChangeDetection after every
// successful CLI rebuild, so hook edits propagate through the existing auto-rebuild flow.
public static class GitHooksSync
{
    private static readonly string SourceHooksDirectory = Path.Combine(Configuration.CliFolder, "git-hooks");

    private static readonly string LegacyHooksPathValue = "developer-cli/git-hooks";

    private static string ConsentFilePath => Path.Combine(Configuration.PublishFolder, $"{Configuration.AliasName}.git-hooks-consent");

    public static int Sync(bool forcePrompt = false)
    {
        try
        {
            return SyncCore(forcePrompt);
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipped git hook sync: {Markup.Escape(e.Message)}[/]");
            return 0;
        }
    }

    public static void RemoveAll()
    {
        try
        {
            RemoveAllCore();
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[yellow]Skipped git hook removal: {Markup.Escape(e.Message)}[/]");
        }
    }

    private static int SyncCore(bool forcePrompt)
    {
        if (!Directory.Exists(SourceHooksDirectory)) return 0;

        var hooks = EnumerateHooks();
        if (hooks.Length == 0) return 0;

        var consent = forcePrompt ? null : ReadConsent();
        if (consent is null)
        {
            consent = PromptForConsent(hooks);
            WriteConsent(consent.Value);
        }

        if (!consent.Value) return 0;

        var hooksDirectory = ResolveMainHooksDirectory();
        if (hooksDirectory is null)
        {
            AnsiConsole.MarkupLine("[yellow]Not a git repository -- skipping git hook sync.[/]");
            return 0;
        }

        HandleExistingHooksPath(hooksDirectory);

        return CopyHooks(hooks, hooksDirectory);
    }

    private static void RemoveAllCore()
    {
        var hooksDirectory = ResolveMainHooksDirectory();
        if (hooksDirectory is null) return;

        if (!Directory.Exists(SourceHooksDirectory)) return;

        var removed = 0;
        foreach (var hookFile in Directory.EnumerateFiles(SourceHooksDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            var name = Path.GetFileName(hookFile);
            var target = Path.Combine(hooksDirectory, name);
            if (File.Exists(target))
            {
                File.Delete(target);
                removed++;
            }
        }

        if (removed > 0)
        {
            AnsiConsole.MarkupLine($"[green]Removed {removed} git hook(s) from [blue].git/hooks/[/].[/]");
        }
    }

    private static (string Name, string Path)[] EnumerateHooks()
    {
        return Directory
            .EnumerateFiles(SourceHooksDirectory, "*", SearchOption.TopDirectoryOnly)
            .OrderBy(p => p, StringComparer.Ordinal)
            .Select(p => (Path.GetFileName(p), p))
            .ToArray();
    }

    private static bool PromptForConsent((string Name, string Path)[] hooks)
    {
        if (Configuration.AutoConfirm) return true;
        if (!AnsiConsole.Profile.Capabilities.Interactive) return false;

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]Git hooks[/]").LeftJustified());
        AnsiConsole.WriteLine();

        AnsiConsole.MarkupLine($"This will copy the hooks under [blue]developer-cli/git-hooks/[/] into the main repo's [blue].git/hooks/[/] directory (shared across all linked worktrees). Hooks are local-only, never pushed, and can be removed any time with [blue]{Configuration.AliasName} uninstall[/].");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Currently shipping:");

        var maxNameLength = hooks.Max(h => h.Name.Length);
        foreach (var (name, path) in hooks)
        {
            var description = ReadDescription(path) ?? "(no description)";
            var padding = new string(' ', maxNameLength - name.Length + 1);
            AnsiConsole.MarkupLine($"  - [blue]{Markup.Escape(name)}[/]:{padding}{Markup.Escape(description)}");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[yellow]Approving applies to the hooks listed above AND any added in future commits.[/]");
        AnsiConsole.WriteLine();
        return AnsiConsole.Confirm("[bold]Install hooks?[/]");
    }

    private static string? ReadDescription(string path)
    {
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("#!")) continue;
            if (!trimmed.StartsWith('#'))
            {
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                return null;
            }

            var description = trimmed.TrimStart('#').Trim();
            if (description.Length > 0) return description;
        }

        return null;
    }

    private static int CopyHooks((string Name, string Path)[] hooks, string hooksDirectory)
    {
        Directory.CreateDirectory(hooksDirectory);

        var updated = 0;
        foreach (var (name, sourcePath) in hooks)
        {
            var targetPath = Path.Combine(hooksDirectory, name);
            if (FilesEqual(sourcePath, targetPath)) continue;

            File.Copy(sourcePath, targetPath, true);

            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(
                    targetPath,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute
                );
            }

            updated++;
        }

        if (updated > 0)
        {
            AnsiConsole.MarkupLine($"[green]Synced {updated} git hook(s) to [blue].git/hooks/[/].[/]");
        }

        return updated;
    }

    private static bool FilesEqual(string source, string target)
    {
        if (!File.Exists(target)) return false;
        var sourceBytes = File.ReadAllBytes(source);
        var targetBytes = File.ReadAllBytes(target);
        return sourceBytes.AsSpan().SequenceEqual(targetBytes);
    }

    private static string? ResolveMainHooksDirectory()
    {
        var output = RunGit("rev-parse --git-common-dir");
        if (string.IsNullOrWhiteSpace(output)) return null;

        var gitDir = Path.IsPathRooted(output)
            ? output
            : Path.GetFullPath(Path.Combine(Configuration.SourceCodeFolder, output));

        return Path.Combine(gitDir, "hooks");
    }

    private static void HandleExistingHooksPath(string defaultHooksDirectory)
    {
        var current = RunGit("config --get core.hooksPath");
        if (string.IsNullOrEmpty(current)) return;

        if (current == LegacyHooksPathValue)
        {
            // Set by old McpSetupCommand.EnableGitHooks. Clean it up so the new copy approach is
            // the single source of truth. The user did not pick this value -- the old setup did.
            RunGit("config --unset core.hooksPath");
            AnsiConsole.MarkupLine($"[grey]Cleared legacy [blue]core.hooksPath = {LegacyHooksPathValue}[/] (replaced by hooks copied into [blue].git/hooks/[/]).[/]");
            return;
        }

        var resolvedCurrent = Path.IsPathRooted(current)
            ? Path.GetFullPath(current)
            : Path.GetFullPath(Path.Combine(Configuration.SourceCodeFolder, current));

        if (string.Equals(resolvedCurrent, Path.GetFullPath(defaultHooksDirectory), StringComparison.OrdinalIgnoreCase)) return;

        // Custom user-set value: leave it alone, but flag that our copies will not run.
        AnsiConsole.MarkupLine($"[yellow]Warning: [blue]core.hooksPath[/] is set to [bold]{Markup.Escape(current)}[/].[/]");
        AnsiConsole.MarkupLine("[yellow]Hooks copied into [blue].git/hooks/[/] will not fire while this redirect is in place.[/]");
        AnsiConsole.MarkupLine("[grey]Run [blue]git config --unset core.hooksPath[/] to remove the redirect.[/]");
    }

    private static string RunGit(string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = Configuration.IsWindows ? "cmd.exe" : "git",
            Arguments = Configuration.IsWindows ? $"/C git {arguments}" : arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Configuration.SourceCodeFolder
        };

        return ProcessHelper.StartProcess(startInfo, exitOnError: false).Trim();
    }

    private static bool? ReadConsent()
    {
        if (!File.Exists(ConsentFilePath)) return null;
        var content = File.ReadAllText(ConsentFilePath).Trim();
        return content switch
        {
            "true" => true,
            "false" => false,
            _ => null
        };
    }

    private static void WriteConsent(bool value)
    {
        Directory.CreateDirectory(Configuration.PublishFolder);
        File.WriteAllText(ConsentFilePath, value ? "true" : "false");
    }
}
