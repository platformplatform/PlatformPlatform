using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Security.Cryptography;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class SyncWindsurfAiRulesAndWorkflowsCommand : Command
{
    public SyncWindsurfAiRulesAndWorkflowsCommand() : base("sync-windsurf-ai-rules", "Sync Windsurf AI rules from .cursor/rules to .windsurf/rules and .windsurf/workflows, converting frontmatter and deleting orphans.")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private static void Execute()
    {
        var sourceRoot = Path.Combine(Configuration.SourceCodeFolder, ".cursor/rules");
        var sourceWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".cursor/rules/workflows");
        var targetRoot = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/rules");
        var targetWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/workflows");

        // Create dictionaries to track file changes
        var initialFileHashes = new Dictionary<string, string>();
        var finalFileHashes = new Dictionary<string, string>();

        // Collect initial file hashes
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".windsurf"), initialFileHashes);

        try
        {
            // Sync rules (excluding workflows)
            SyncAiRules(sourceRoot, targetRoot, sourceWorkflows);
            // Sync workflows
            SyncAiWorkflows(sourceWorkflows, targetWorkflows);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Environment.Exit(1);
        }

        // Collect final file hashes
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".windsurf"), finalFileHashes);

        // Display results
        DisplayFileChangeResults(initialFileHashes, finalFileHashes);
    }

    private static void CollectFileHashes(string directory, Dictionary<string, string> fileHashes)
    {
        if (!Directory.Exists(directory)) return;

        foreach (var filePath in Directory.GetFiles(directory, "*.md", SearchOption.AllDirectories))
        {
            var fileBytes = File.ReadAllBytes(filePath);
            fileHashes[filePath] = Convert.ToHexString(MD5.HashData(fileBytes));
        }
    }

    private static void DisplayFileChangeResults(Dictionary<string, string> initialFileHashes, Dictionary<string, string> finalFileHashes)
    {
        var newFilesCount = 0;
        var updatedFilesCount = 0;
        var unmodifiedFilesCount = 0;

        // Find new and updated files
        foreach (var fileEntry in finalFileHashes)
        {
            var filePath = fileEntry.Key;
            var fileHash = fileEntry.Value;

            if (!initialFileHashes.TryGetValue(filePath, out var hash))
            {
                newFilesCount++;
            }
            else if (hash != fileHash)
            {
                updatedFilesCount++;
            }
            else
            {
                unmodifiedFilesCount++;
            }
        }

        var deletedFilesCount = initialFileHashes.Count(fileEntry => !finalFileHashes.ContainsKey(fileEntry.Key));

        AnsiConsole.MarkupLine("[green]Sync completed:[/]");
        AnsiConsole.MarkupLine($"  [green]{newFilesCount}[/] new files created");
        AnsiConsole.MarkupLine($"  [yellow]{updatedFilesCount}[/] files updated");
        AnsiConsole.MarkupLine($"  [red]{deletedFilesCount}[/] files deleted");
        AnsiConsole.MarkupLine($"  [blue]{unmodifiedFilesCount}[/] files unmodified");
    }

    private static void SyncAiRules(string sourceDirectory, string targetDirectory, string? excludedSubdirectory = null)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        // Get all .md files in the target directory and subdirectories
        var existingFiles = Directory.GetFiles(targetDirectory, "*.md", SearchOption.AllDirectories);

        // Get all .mdc files from the source directory and subdirectories except the excluded subdirectory
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.mdc", SearchOption.AllDirectories)
            .Where(f => !f.StartsWith(excludedSubdirectory ?? "-"))
            .ToArray();

        var targetFiles = new List<string>();
        foreach (var sourceFile in sourceFiles)
        {
            var targetFile = GetTargetFilePath(sourceFile, sourceDirectory, targetDirectory);
            // Convert and copy the file
            ConvertAndCopyRule(sourceFile, targetFile);

            targetFiles.Add(targetFile);
        }

        // Delete any orphaned files that don't correspond to a source file
        foreach (var orphanFile in existingFiles.Except(targetFiles.ToArray()))
        {
            File.Delete(orphanFile);
        }
    }

    private static string GetTargetFilePath(string sourceFile, string sourceDirectory, string targetDirectory)
    {
        var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);

        string fileName;
        // Check if we have a duplicate folder/file name pattern (folder-folder.md)
        var segments = relativePath.Split(Path.DirectorySeparatorChar);
        if (segments.Length == 2 && segments[0] == Path.GetFileNameWithoutExtension(segments[1]))
        {
            fileName = Path.GetFileNameWithoutExtension(relativePath) + ".md";
        }
        else
        {
            fileName = relativePath.Replace(Path.DirectorySeparatorChar, '-');
        }

        return Path.Combine(targetDirectory,  Path.GetFileNameWithoutExtension(fileName) + ".md");
    }

    private static void SyncAiWorkflows(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        // Get all .md files in the target directory and subdirectories
        var existingFiles = Directory.GetFiles(targetDirectory, "*.md", SearchOption.AllDirectories);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.mdc", SearchOption.TopDirectoryOnly);

        var targetFiles = new List<string>();
        foreach (var sourceFile in sourceFiles)
        {
            var targetFile = Path.Combine(targetDirectory, Path.GetFileNameWithoutExtension(sourceFile) + ".md");
            ConvertAndCopyWorkflow(sourceFile, targetFile);
            targetFiles.Add(targetFile);
        }

        // Delete any orphaned files that don't correspond to a source file
        foreach (var orphanFile in existingFiles.Except(targetFiles.ToArray()))
        {
            File.Delete(orphanFile);
        }
    }

    private static void ConvertAndCopyRule(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);
        var newFrontmatterLines = ConvertFrontmatter(frontmatterLines);
        File.WriteAllLines(targetFile, newFrontmatterLines.Concat(contentLines));
    }

    private static void ConvertAndCopyWorkflow(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);
        var frontmatterDictionary = ParseFrontmatter(frontmatterLines);
        if (frontmatterDictionary.TryGetValue("alwaysApply", out var alwaysApply) && alwaysApply == "false" &&
            (frontmatterDictionary["globs"] == "" || !frontmatterDictionary.ContainsKey("globs") || frontmatterDictionary["globs"].Trim() == ""))
        {
            // Valid
            var newFrontmatterLines = new List<string> { "---" };
            if (frontmatterDictionary.TryGetValue("description", out var description))
            {
                newFrontmatterLines.Add($"description: {description}");
            }

            newFrontmatterLines.Add("---");
            File.WriteAllLines(targetFile, newFrontmatterLines.Concat(contentLines));
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]Error: Workflow file {sourceFile} must have alwaysApply: false and no globs.[/]");
            Environment.Exit(1);
        }
    }

    private static (List<string> frontmatterLines, List<string> contentLines) SplitFrontmatter(string[] lines)
    {
        var frontmatterLines = new List<string>();
        var contentLines = new List<string>();
        bool inFrontmatter = false, doneFrontmatter = false;
        foreach (var line in lines)
        {
            if (!inFrontmatter && line.Trim() == "---")
            {
                inFrontmatter = true;
                frontmatterLines.Add(line);
                continue;
            }

            if (inFrontmatter && !doneFrontmatter)
            {
                if (line.Trim() == "---")
                {
                    frontmatterLines.Add(line);
                    doneFrontmatter = true;
                    continue;
                }

                frontmatterLines.Add(line);
            }
            else
            {
                contentLines.Add(line);
            }
        }

        return (frontmatterLines, contentLines);
    }

    private static List<string> ConvertFrontmatter(List<string> frontmatterLines)
    {
        var frontmatterDictionary = ParseFrontmatter(frontmatterLines);
        var result = new List<string> { "---" };

        // Pattern #1: Always (alwaysApply: true)
        // Cursor: alwaysApply: true -> Windsurf: trigger: always_on
        if (frontmatterDictionary.TryGetValue("alwaysApply", out var alwaysApply) && alwaysApply == "true")
        {
            result.Add("trigger: always_on");
        }
        // Pattern #2: Agent Requested (alwaysApply: false with description)
        // Cursor: description: [value], alwaysApply: false -> Windsurf: trigger: model_decision, description: [value]
        else if (frontmatterDictionary.TryGetValue("alwaysApply", out alwaysApply) && alwaysApply == "false" &&
            frontmatterDictionary.TryGetValue("description", out var description) && !string.IsNullOrWhiteSpace(description) &&
            (!frontmatterDictionary.TryGetValue("globs", out var globs) || string.IsNullOrWhiteSpace(globs)))
        {
            result.Add("trigger: model_decision");
            result.Add($"description: {description}");
        }
        // Pattern #3: Auto Attached (has globs)
        // Cursor: globs: [value], alwaysApply: false -> Windsurf: trigger: manual, glob: [value]
        else if (frontmatterDictionary.TryGetValue("globs", out globs) && !string.IsNullOrWhiteSpace(globs) &&
            frontmatterDictionary.TryGetValue("alwaysApply", out alwaysApply) && alwaysApply == "false")
        {
            result.Add("trigger: manual");
            result.Add($"glob: {globs}");

            // Include description if it exists
            if (frontmatterDictionary.TryGetValue("description", out description) && !string.IsNullOrWhiteSpace(description))
            {
                result.Add($"description: {description}");
            }
            else
            {
                result.Add("description:");
            }
        }
        // Pattern #4: Manual (alwaysApply: false, no globs, no description)
        // Cursor: alwaysApply: false -> Windsurf: trigger: manual
        else if (frontmatterDictionary.TryGetValue("alwaysApply", out alwaysApply) && alwaysApply == "false" &&
            (!frontmatterDictionary.TryGetValue("globs", out globs) || string.IsNullOrWhiteSpace(globs)) &&
            (!frontmatterDictionary.TryGetValue("description", out description) || string.IsNullOrWhiteSpace(description)))
        {
            result.Add("trigger: manual");
        }
        // Fallback: copy as-is but in Windsurf format
        else
        {
            foreach (var entry in frontmatterDictionary)
            {
                if (entry.Key == "alwaysApply")
                {
                    result.Add(entry.Value == "true" ? "trigger: always_on" : "trigger: manual");
                }
                else
                {
                    result.Add($"{entry.Key}: {entry.Value}");
                }
            }
        }

        result.Add("---");
        result.Add("");
        
        return result;
    }

    private static Dictionary<string, string> ParseFrontmatter(List<string> frontmatterLines)
    {
        var frontmatterDictionary = new Dictionary<string, string>();
        foreach (var line in frontmatterLines)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine == "---" || string.IsNullOrWhiteSpace(trimmedLine)) continue;
            var colonIndex = trimmedLine.IndexOf(':');
            if (colonIndex > 0)
            {
                var key = trimmedLine[..colonIndex].Trim();
                var value = trimmedLine[(colonIndex + 1)..].Trim();
                frontmatterDictionary[key] = value;
            }
        }

        return frontmatterDictionary;
    }
}
