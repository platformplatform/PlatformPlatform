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
            SyncAiFiles(sourceRoot, targetRoot, false, sourceWorkflows);
            // Sync workflows
            SyncAiFiles(sourceWorkflows, targetWorkflows, true);
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

        foreach (var filePath in Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories))
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

    private static void SyncAiFiles(string sourceDirectory, string targetDirectory, bool isWorkflow, string? excludedSubdirectory = null)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        // Get all files in the target directory and subdirectories
        var existingFiles = Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories);

        // Get all files from the source directory and subdirectories except the excluded subdirectory
        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
            .Where(f => excludedSubdirectory is null || !f.StartsWith(excludedSubdirectory))
            .ToArray();

        var targetFiles = new List<string>();
        foreach (var sourceFile in sourceFiles)
        {
            var targetFile = GetTargetFilePath(sourceFile, sourceDirectory, targetDirectory);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            ConvertAndCopyFile(sourceFile, targetFile, isWorkflow);

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
        var relativeDirectory = Path.GetDirectoryName(relativePath) ?? "";

        // Convert Cursor .mdc files to Windsurf .md
        if (Path.GetExtension(sourceFile) == ".mdc")
        {
            var fileName = Path.GetFileNameWithoutExtension(relativePath) + ".md";
            return Path.Combine(targetDirectory, relativeDirectory, fileName);
        }

        // For all other files, keep the original extension
        return Path.Combine(targetDirectory, relativeDirectory, Path.GetFileName(relativePath));
    }

    private static void ConvertAndCopyFile(string sourceFile, string targetFile, bool isWorkflow)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);

        // Convert references in content lines
        var contentLinesArray = contentLines.ToArray();
        var fileName = Path.GetFileName(sourceFile);
        if (fileName != "ai-rules.mdc") // The ai-rules.mdc is special case as ist must keep using Cursor references
        {
            // Replace all cursor references with Windsurf references
            ReplaceCursorRuleReferencesWithWindsurfReferences(contentLinesArray);
        }

        // Convert frontmatter based on type
        var newFrontmatterLines = ConvertFrontmatter(frontmatterLines, isWorkflow);

        File.WriteAllLines(targetFile, newFrontmatterLines.Concat(contentLinesArray));
    }

    private static void ReplaceCursorRuleReferencesWithWindsurfReferences(string[] lines)
    {
        // Replace all cursor references with Windsurf references
        for (var i = 0; i < lines.Length; i++)
        {
            lines[i] = lines[i].Replace(".cursor", ".windsurf");
            lines[i] = lines[i].Replace(".windsurf/rules/workflows", ".windsurf/workflows");
            lines[i] = lines[i].Replace("(mdc:", "(/");
            lines[i] = lines[i].Replace(".mdc", ".md");
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

    private static List<string> ConvertFrontmatter(List<string> frontmatterLines, bool isWorkflow)
    {
        var frontmatterDictionary = ParseFrontmatter(frontmatterLines);
        var result = new List<string> { "---" };

        // For workflows, we only want to keep the description
        if (isWorkflow)
        {
            if (frontmatterDictionary.TryGetValue("description", out var description))
            {
                result.Add($"description: {description}");
            }

            result.Add("---");
            result.Add("");
            return result;
        }

        // For rules, apply the full conversion logic
        // Pattern #1: Always Apply (no glob)
        // Cursor: alwaysApply: true -> Windsurf: trigger: always_on
        if (frontmatterDictionary.TryGetValue("alwaysApply", out var alwaysApply) && alwaysApply == "true")
        {
            result.Add("trigger: always_on");

            // Include globs if they exist
            if (frontmatterDictionary.TryGetValue("globs", out var globs) && !string.IsNullOrWhiteSpace(globs))
            {
                result.Add($"globs: {globs}");
            }

            // Include description if it exists
            if (frontmatterDictionary.TryGetValue("description", out var description) && !string.IsNullOrWhiteSpace(description))
            {
                result.Add($"description: {description}");
            }
        }
        // Pattern #2: Auto Attach (glob)
        // Cursor: globs: [value], alwaysApply: false -> Windsurf: trigger: glob, globs: [value]
        else if (frontmatterDictionary.TryGetValue("globs", out var globs) && !string.IsNullOrWhiteSpace(globs) &&
                 frontmatterDictionary.TryGetValue("alwaysApply", out alwaysApply) && alwaysApply == "false")
        {
            result.Add("trigger: glob");
            result.Add($"globs: {globs}");

            // Include description if it exists
            if (frontmatterDictionary.TryGetValue("description", out var description) && !string.IsNullOrWhiteSpace(description))
            {
                result.Add($"description: {description}");
            }
            else
            {
                result.Add("description:");
            }
        }
        // Pattern #3: Agent Requested (description)
        // Cursor: description: [value], alwaysApply: false -> Windsurf: trigger: model_decision, description: [value]
        else if (frontmatterDictionary.TryGetValue("alwaysApply", out alwaysApply) && alwaysApply == "false" &&
                 frontmatterDictionary.TryGetValue("description", out var description) && !string.IsNullOrWhiteSpace(description) &&
                 (!frontmatterDictionary.TryGetValue("globs", out _) || string.IsNullOrWhiteSpace(frontmatterDictionary["globs"])))
        {
            result.Add("trigger: model_decision");
            result.Add($"description: {description}");
            result.Add("globs:");
        }
        // Pattern #4: Manual (no description)
        // Cursor: alwaysApply: false -> Windsurf: trigger: manual
        else if (frontmatterDictionary.TryGetValue("alwaysApply", out alwaysApply) && alwaysApply == "false" &&
                 (!frontmatterDictionary.TryGetValue("description", out _) || string.IsNullOrWhiteSpace(frontmatterDictionary["description"])))
        {
            result.Add("trigger: manual");
            result.Add("description:");
            result.Add("globs:");
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
