using System.CommandLine;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class SyncAiRulesAndWorkflowsCommand : Command
{
    public SyncAiRulesAndWorkflowsCommand() : base("sync-ai-rules", "Sync AI rules, workflows, and reference docs from .claude to .cursor, .windsurf, .agent (Antigravity), and .github (Copilot symlink).")
    {
        SetAction(_ => Execute());
    }

    private static void Execute()
    {
        // Source directories from .claude
        var claudeRoot = Path.Combine(Configuration.SourceCodeFolder, ".claude");
        var claudeCommands = Path.Combine(claudeRoot, "commands");
        var claudeRules = Path.Combine(claudeRoot, "rules");
        var claudeReference = Path.Combine(claudeRoot, "reference");

        // Target directories for Windsurf
        var windsurfWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/workflows");
        var windsurfRules = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/rules");
        var windsurfReference = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/reference");

        // Target directories for Cursor
        var cursorWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".cursor/rules/workflows");
        var cursorRules = Path.Combine(Configuration.SourceCodeFolder, ".cursor/rules");
        var cursorReference = Path.Combine(Configuration.SourceCodeFolder, ".cursor/reference");

        // Target directories for GitHub Copilot
        var copilotInstructions = Path.Combine(Configuration.SourceCodeFolder, ".github/copilot-instructions.md");
        var copilotRules = Path.Combine(Configuration.SourceCodeFolder, ".github/copilot/rules");
        var copilotWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".github/copilot/workflows");
        var copilotReference = Path.Combine(Configuration.SourceCodeFolder, ".github/copilot/reference");
        var agentsMd = Path.Combine(Configuration.SourceCodeFolder, "AGENTS.md");

        // Target directories for Google Antigravity
        var antigravityWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".agent/workflows");
        var antigravityRules = Path.Combine(Configuration.SourceCodeFolder, ".agent/rules");
        var antigravityReference = Path.Combine(Configuration.SourceCodeFolder, ".agent/reference");

        // Create dictionaries to track file changes
        var initialFileHashes = new Dictionary<string, string>();
        var finalFileHashes = new Dictionary<string, string>();

        // Track expected files in target directories
        var expectedWindsurfFiles = new HashSet<string>();
        var expectedCursorFiles = new HashSet<string>();
        var expectedCopilotFiles = new HashSet<string>();
        var expectedAntigravityFiles = new HashSet<string>();

        // Collect initial file hashes for all target directories
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".windsurf"), initialFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".cursor"), initialFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".github/copilot"), initialFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".agent"), initialFileHashes);
        CollectSymlinkHash(copilotInstructions, initialFileHashes);

        try
        {
            // Sync to Windsurf
            // Commands → workflows
            SyncClaudeToWindsurfWorkflows(claudeCommands, windsurfWorkflows, expectedWindsurfFiles);
            // Rules → rules
            SyncClaudeToWindsurfRules(claudeRules, windsurfRules, expectedWindsurfFiles);
            // Reference → reference
            SyncClaudeToWindsurfRules(claudeReference, windsurfReference, expectedWindsurfFiles);

            // Sync to Cursor
            // Commands → rules/workflows
            SyncClaudeToCursorWorkflows(claudeCommands, cursorWorkflows, expectedCursorFiles);
            // Rules → rules
            SyncClaudeToCursorRules(claudeRules, cursorRules, cursorWorkflows, expectedCursorFiles);
            // Reference → reference (simple copy for Cursor)
            SyncClaudeToPlainMarkdown(claudeReference, cursorReference, expectedCursorFiles);

            // Sync to GitHub Copilot
            CreateOrUpdateSymlink(agentsMd, copilotInstructions);
            // Commands → workflows
            SyncClaudeToCopilotWorkflows(claudeCommands, copilotWorkflows, expectedCopilotFiles);
            // Rules → rules
            SyncClaudeToCopilotRules(claudeRules, copilotRules, expectedCopilotFiles);
            // Reference → reference
            SyncClaudeToCopilotRules(claudeReference, copilotReference, expectedCopilotFiles);

            // Sync to Google Antigravity
            // Commands → workflows
            SyncClaudeToAntigravityWorkflows(claudeCommands, antigravityWorkflows, expectedAntigravityFiles);
            // Rules → rules
            SyncClaudeToAntigravityRules(claudeRules, antigravityRules, expectedAntigravityFiles);
            // Reference → reference
            SyncClaudeToAntigravityRules(claudeReference, antigravityReference, expectedAntigravityFiles);

            // Delete orphaned files in target directories
            DeleteOrphanedFiles(Path.Combine(Configuration.SourceCodeFolder, ".windsurf"), expectedWindsurfFiles);
            DeleteOrphanedFiles(Path.Combine(Configuration.SourceCodeFolder, ".cursor"), expectedCursorFiles);
            DeleteOrphanedFiles(Path.Combine(Configuration.SourceCodeFolder, ".github/copilot"), expectedCopilotFiles);
            DeleteOrphanedFiles(Path.Combine(Configuration.SourceCodeFolder, ".agent"), expectedAntigravityFiles);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Environment.Exit(1);
        }

        // Collect final file hashes
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".windsurf"), finalFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".cursor"), finalFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".github/copilot"), finalFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".agent"), finalFileHashes);
        CollectSymlinkHash(copilotInstructions, finalFileHashes);

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

    private static void CollectSymlinkHash(string symlinkPath, Dictionary<string, string> fileHashes)
    {
        var fileInfo = new FileInfo(symlinkPath);

        // Check if it's a symlink (works even for broken symlinks)
        if (fileInfo.LinkTarget is null) return;

        // For symlinks, hash the link target path itself (not the file content)
        // This way we detect when the symlink target changes
        var linkTargetBytes = Encoding.UTF8.GetBytes(fileInfo.LinkTarget);
        fileHashes[symlinkPath] = Convert.ToHexString(MD5.HashData(linkTargetBytes));
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

    private static void DeleteOrphanedFiles(string targetRoot, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(targetRoot)) return;

        foreach (var existingFile in Directory.GetFiles(targetRoot, "*.*", SearchOption.AllDirectories))
        {
            if (!expectedFiles.Contains(existingFile))
            {
                File.Delete(existingFile);

                // Clean up empty directories
                var directory = Path.GetDirectoryName(existingFile);
                while (!string.IsNullOrEmpty(directory) && directory != targetRoot && Directory.Exists(directory) && !Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                    directory = Path.GetDirectoryName(directory);
                }
            }
        }
    }

    private static void SyncClaudeToWindsurfWorkflows(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);
            ConvertClaudeToWindsurfWorkflow(sourceFile, targetFile);
        }
    }

    private static void SyncClaudeToWindsurfRules(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);

            var lines = File.ReadAllLines(sourceFile);
            var (frontmatterLines, contentLines) = SplitFrontmatter(lines);
            var frontmatterDict = ParseFrontmatter(frontmatterLines);

            // Skip path conversion for update-ai-rules file (it should keep .claude/ references)
            var skipFile = IsUpdateAiRulesFile(sourceFile);

            // Build output with converted frontmatter
            var outputLines = new List<string>();

            if (frontmatterLines.Count > 0)
            {
                outputLines.Add("---");

                // Convert paths to trigger + globs for Windsurf
                if (frontmatterDict.TryGetValue("paths", out var paths))
                {
                    outputLines.Add("trigger: glob");
                    outputLines.Add($"globs: {paths}");
                }
                else if (frontmatterDict.TryGetValue("trigger", out var trigger))
                {
                    outputLines.Add($"trigger: {trigger}");
                    if (frontmatterDict.TryGetValue("globs", out var globs))
                    {
                        outputLines.Add($"globs: {globs}");
                    }
                }

                if (frontmatterDict.TryGetValue("description", out var description))
                {
                    outputLines.Add($"description: {description}");
                }

                outputLines.Add("---");
            }

            outputLines.AddRange(contentLines);

            var outputArray = outputLines.ToArray();
            ReplaceClaudeReferencesWithWindsurf(outputArray, skipFile);

            File.WriteAllLines(targetFile, outputArray);
        }
    }

    private static void SyncClaudeToCursorWorkflows(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            // Change .md to .mdc for Cursor
            var targetFileName = Path.GetFileNameWithoutExtension(relativePath) + ".mdc";
            var targetFile = Path.Combine(targetDirectory, Path.GetDirectoryName(relativePath) ?? "", targetFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);
            ConvertClaudeToCursorWorkflow(sourceFile, targetFile);
        }
    }

    private static void SyncClaudeToCursorRules(string sourceDirectory, string targetDirectory, string workflowsDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            // Change .md to .mdc for Cursor
            var targetFileName = Path.GetFileNameWithoutExtension(relativePath) + ".mdc";
            var targetFile = Path.Combine(targetDirectory, Path.GetDirectoryName(relativePath) ?? "", targetFileName);

            // Skip if this would conflict with a workflow file
            if (targetFile.StartsWith(workflowsDirectory)) continue;

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);
            ConvertClaudeToCursorRule(sourceFile, targetFile);
        }
    }

    private static void SyncClaudeToPlainMarkdown(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);

            // Simple copy - no format conversion needed for samples
            File.Copy(sourceFile, targetFile, true);
        }
    }

    private static void ConvertClaudeToWindsurfWorkflow(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);

        // Skip path conversion for update-ai-rules file (it should keep .claude/ references)
        var skipFile = IsUpdateAiRulesFile(sourceFile);

        // Only add frontmatter if source file has frontmatter
        if (frontmatterLines.Count > 0)
        {
            var frontmatterDict = ParseFrontmatter(frontmatterLines);

            // Convert frontmatter for Windsurf workflows
            var newFrontmatter = new List<string> { "---" };

            if (frontmatterDict.TryGetValue("description", out var description))
            {
                newFrontmatter.Add($"description: {description}");
            }

            // Add auto_execution_mode (3 = turbo mode)
            newFrontmatter.Add("auto_execution_mode: 3");
            newFrontmatter.Add("---");

            // Keep content as-is (including $ARGUMENTS)
            var allLines = newFrontmatter.Concat(contentLines).ToArray();

            // Convert .claude references to .windsurf references
            ReplaceClaudeReferencesWithWindsurf(allLines, skipFile);

            File.WriteAllLines(targetFile, allLines);
        }
        else
        {
            // No frontmatter in source, just copy content as-is
            var allLines = contentLines.ToArray();

            // Convert .claude references to .windsurf references
            ReplaceClaudeReferencesWithWindsurf(allLines, skipFile);

            File.WriteAllLines(targetFile, allLines);
        }
    }

    private static void ConvertClaudeToCursorWorkflow(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);

        // Skip path conversion for update-ai-rules file (it should keep .claude/ references)
        var skipFile = IsUpdateAiRulesFile(sourceFile);

        // Keep content as-is (including $ARGUMENTS) but replace references
        var modifiedContent = contentLines.ToArray();
        ReplaceClaudeReferencesWithCursor(modifiedContent, skipFile);

        // Only add frontmatter if source file has frontmatter
        if (frontmatterLines.Count > 0)
        {
            var frontmatterDict = ParseFrontmatter(frontmatterLines);

            // Convert frontmatter for Cursor workflows
            var newFrontmatter = new List<string> { "---" };

            if (frontmatterDict.TryGetValue("description", out var description))
            {
                newFrontmatter.Add($"description: {description}");
            }

            newFrontmatter.Add("globs: ");
            newFrontmatter.Add("alwaysApply: false");
            newFrontmatter.Add("---");

            // Combine frontmatter and content
            // Skip first line of content if it's empty (to avoid double blank line after frontmatter)
            var contentToWrite = modifiedContent.ToList();
            if (contentToWrite.Count > 0 && string.IsNullOrWhiteSpace(contentToWrite[0]))
            {
                contentToWrite.RemoveAt(0);
            }

            var allLines = newFrontmatter.Concat(contentToWrite).ToList();

            File.WriteAllLines(targetFile, allLines);
        }
        else
        {
            // No frontmatter in source, just copy content with reference replacements
            var allLines = modifiedContent.ToList();

            File.WriteAllLines(targetFile, allLines);
        }
    }

    private static void ConvertClaudeToCursorRule(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);
        var frontmatterDict = ParseFrontmatter(frontmatterLines);

        // Convert frontmatter for Cursor rules
        var newFrontmatter = new List<string> { "---" };

        // Get globs value from either paths or globs field
        var globsValue = frontmatterDict.GetValueOrDefault("paths") ?? frontmatterDict.GetValueOrDefault("globs");

        // Convert trigger patterns
        if (frontmatterDict.TryGetValue("trigger", out var trigger))
        {
            switch (trigger)
            {
                case "always_on":
                    if (frontmatterDict.TryGetValue("description", out var desc))
                    {
                        newFrontmatter.Add($"description: {desc}");
                    }

                    newFrontmatter.Add(!string.IsNullOrWhiteSpace(globsValue)
                        ? $"globs: {ConvertGlobsForCursor(globsValue)}"
                        : "globs: ");

                    newFrontmatter.Add("alwaysApply: true");
                    break;

                case "glob":
                    if (frontmatterDict.TryGetValue("description", out desc))
                    {
                        newFrontmatter.Add($"description: {desc}");
                    }

                    if (globsValue != null)
                    {
                        newFrontmatter.Add($"globs: {ConvertGlobsForCursor(globsValue)}");
                    }

                    newFrontmatter.Add("alwaysApply: false");
                    break;

                case "model_decision":
                    if (frontmatterDict.TryGetValue("description", out desc))
                    {
                        newFrontmatter.Add($"description: {desc}");
                    }

                    newFrontmatter.Add("globs: ");
                    newFrontmatter.Add("alwaysApply: false");
                    break;

                case "manual":
                    newFrontmatter.Add("description: ");
                    newFrontmatter.Add("globs: ");
                    newFrontmatter.Add("alwaysApply: false");
                    break;

                default:
                    // Fallback
                    if (frontmatterDict.TryGetValue("description", out desc))
                    {
                        newFrontmatter.Add($"description: {desc}");
                    }

                    if (globsValue != null)
                    {
                        newFrontmatter.Add($"globs: {ConvertGlobsForCursor(globsValue)}");
                    }

                    newFrontmatter.Add("alwaysApply: false");
                    break;
            }
        }
        else if (frontmatterDict.ContainsKey("paths"))
        {
            // New Claude format: paths field without trigger (infer trigger: glob)
            if (frontmatterDict.TryGetValue("description", out var desc))
            {
                newFrontmatter.Add($"description: {desc}");
            }

            if (globsValue != null)
            {
                newFrontmatter.Add($"globs: {ConvertGlobsForCursor(globsValue)}");
            }

            newFrontmatter.Add("alwaysApply: false");
        }
        else
        {
            // No trigger or paths field, use defaults
            if (frontmatterDict.TryGetValue("description", out var desc))
            {
                newFrontmatter.Add($"description: {desc}");
            }

            if (globsValue != null)
            {
                newFrontmatter.Add($"globs: {ConvertGlobsForCursor(globsValue)}");
            }

            newFrontmatter.Add("alwaysApply: false");
        }

        newFrontmatter.Add("---");

        // Skip path conversion for update-ai-rules file (it should keep .claude/ references)
        var skipFile = IsUpdateAiRulesFile(sourceFile);

        // Replace .claude references with .cursor references
        var modifiedContent = contentLines.ToArray();
        ReplaceClaudeReferencesWithCursor(modifiedContent, skipFile);

        // Combine frontmatter and content
        // Skip first line of content if it's empty (to avoid double blank line after frontmatter)
        var contentToWrite = modifiedContent.ToList();
        if (contentToWrite.Count > 0 && string.IsNullOrWhiteSpace(contentToWrite[0]))
        {
            contentToWrite.RemoveAt(0);
        }

        var allLines = newFrontmatter.Concat(contentToWrite).ToList();

        File.WriteAllLines(targetFile, allLines);
    }

    private static void ReplaceClaudeReferencesWithWindsurf(string[] lines, bool skipFile = false)
    {
        if (skipFile) return;

        for (var i = 0; i < lines.Length; i++)
        {
            // Order matters - specific paths first, then generic
            // Convert .claude/commands/ to .windsurf/workflows/
            lines[i] = lines[i].Replace(".claude/commands/", ".windsurf/workflows/");

            // Convert remaining .claude/ references to .windsurf/
            lines[i] = lines[i].Replace(".claude/", ".windsurf/");
        }
    }

    private static void ReplaceClaudeReferencesWithCursor(string[] lines, bool skipFile = false)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            // Handle markdown links - convert paths to use mdc: prefix for Cursor
            lines[i] = Regex.Replace(lines[i],
                @"\[([^\]]+)\]\(([^)]+)\)",
                m =>
                {
                    var linkText = m.Groups[1].Value;
                    var linkPath = m.Groups[2].Value;

                    // Skip if already has mdc: prefix or is external URL
                    if (linkPath.StartsWith("mdc:") || linkPath.StartsWith("http"))
                    {
                        return $"[{linkText}]({linkPath})";
                    }

                    // Check if this is a path that should NOT have .md → .mdc conversion
                    // .github/ files and samples/ files keep .md extension
                    var shouldConvertExtension = !linkPath.Contains(".github/") && !linkPath.Contains("/samples/");

                    // Convert .md to .mdc in link text only if appropriate
                    if (shouldConvertExtension && linkText.EndsWith(".md"))
                    {
                        linkText = linkText.Replace(".md", ".mdc");
                    }

                    // Handle absolute paths - add mdc: prefix
                    if (linkPath.StartsWith("/"))
                    {
                        // Convert absolute paths to mdc: format
                        var mdcPath = linkPath.Substring(1);

                        // Only convert .md to .mdc for rule/workflow files, not .github or samples
                        if (shouldConvertExtension)
                        {
                            mdcPath = mdcPath.Replace(".md", ".mdc");
                        }

                        // Handle .windsurf paths - convert to .cursor
                        if (mdcPath.StartsWith(".windsurf/"))
                        {
                            mdcPath = mdcPath.Replace(".windsurf/", ".cursor/");
                            // Move workflows to rules/workflows
                            if (mdcPath.Contains("/workflows/"))
                            {
                                mdcPath = mdcPath.Replace(".cursor/workflows/", ".cursor/rules/workflows/");
                            }
                        }
                        // Handle .claude paths - convert to .cursor
                        else if (mdcPath.StartsWith(".claude/"))
                        {
                            mdcPath = mdcPath.Replace(".claude/", ".cursor/");
                            // Move commands to rules/workflows
                            if (mdcPath.Contains("/commands/"))
                            {
                                mdcPath = mdcPath.Replace(".cursor/commands/", ".cursor/rules/workflows/");
                            }
                        }

                        return $"[{linkText}](mdc:{mdcPath})";
                    }

                    // Handle relative paths - only convert .md to .mdc for rule/workflow files
                    if (shouldConvertExtension && linkPath.EndsWith(".md"))
                    {
                        linkPath = linkPath.Replace(".md", ".mdc");
                    }

                    return $"[{linkText}]({linkPath})";
                }
            );

            // Replace *.md glob patterns with *.mdc (e.g., "Inspect all *.md files")
            lines[i] = Regex.Replace(lines[i], @"\*\.md\b", "*.mdc");

            // Convert all .claude/ references to .cursor/ (unless skipping this file)
            if (!skipFile)
            {
                // Order matters - specific paths first, then generic
                // Convert .claude/commands/ to .cursor/rules/workflows/
                lines[i] = lines[i].Replace(".claude/commands/", ".cursor/rules/workflows/");

                // Convert remaining .claude/ references to .cursor/
                lines[i] = lines[i].Replace(".claude/", ".cursor/");

                // Convert plain text .md references to .mdc for .cursor/rules/ paths (but not samples or .github)
                // Match patterns like `.cursor/rules/something.md` or `.cursor/rules/workflows/something.md`
                lines[i] = Regex.Replace(lines[i], @"(\.cursor/rules/[^\s`]*?)\.md\b", "$1.mdc");
            }
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

    private static bool IsUpdateAiRulesFile(string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return fileName.Equals("update-ai-rules", StringComparison.OrdinalIgnoreCase) ||
               fileName.Equals("ai-rules", StringComparison.OrdinalIgnoreCase);
    }

    private static string ConvertGlobsForCursor(string globs)
    {
        // Convert .claude/commands/ to .cursor/rules/workflows/ in globs
        return globs.Replace(".claude/commands/", ".cursor/rules/workflows/")
            .Replace(".claude/", ".cursor/");
    }

    private static Dictionary<string, string> ParseFrontmatter(List<string> frontmatterLines)
    {
        var frontmatterDictionary = new Dictionary<string, string>();
        foreach (var line in frontmatterLines)
        {
            // Skip delimiter lines and empty lines
            var trimmedLine = line.Trim();
            if (trimmedLine == "---" || string.IsNullOrWhiteSpace(trimmedLine)) continue;

            // Only parse top-level keys (lines that don't start with whitespace)
            // This prevents nested YAML values from overwriting top-level keys
            if (line.Length > 0 && char.IsWhiteSpace(line[0])) continue;

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

    private static void CreateOrUpdateSymlink(string targetPath, string linkPath)
    {
        // Calculate relative path from link location to target
        var linkDirectory = Path.GetDirectoryName(linkPath) ?? "";
        var relativePath = Path.GetRelativePath(linkDirectory, targetPath);

        // Check if symlink already exists (works for both valid and broken symlinks)
        var fileInfo = new FileInfo(linkPath);
        if (fileInfo.LinkTarget is not null)
        {
            if (fileInfo.LinkTarget == relativePath)
            {
                return; // Symlink already correct
            }

            // Delete existing symlink (broken or pointing to wrong target)
            File.Delete(linkPath);
        }
        else if (File.Exists(linkPath))
        {
            // It's a regular file, not a symlink - delete it
            File.Delete(linkPath);
        }

        // Create symlink
        Directory.CreateDirectory(linkDirectory);
        File.CreateSymbolicLink(linkPath, relativePath);
    }

    private static void SyncClaudeToCopilotWorkflows(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);
            ConvertClaudeToCopilot(sourceFile, targetFile);
        }
    }

    private static void SyncClaudeToCopilotRules(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);
            ConvertClaudeToCopilot(sourceFile, targetFile);
        }
    }

    private static void ConvertClaudeToCopilot(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (_, contentLines) = SplitFrontmatter(lines);

        // Skip path conversion for update-ai-rules file
        var skipFile = IsUpdateAiRulesFile(sourceFile);

        // Copilot doesn't use frontmatter - just content
        var contentToWrite = contentLines.ToList();
        if (contentToWrite.Count > 0 && string.IsNullOrWhiteSpace(contentToWrite[0]))
        {
            contentToWrite.RemoveAt(0);
        }

        // Replace Claude references with Copilot references
        var outputArray = contentToWrite.ToArray();
        ReplaceClaudeReferencesWithCopilot(outputArray, skipFile);

        File.WriteAllLines(targetFile, outputArray);
    }

    private static void ReplaceClaudeReferencesWithCopilot(string[] lines, bool skipFile = false)
    {
        if (skipFile) return;

        for (var i = 0; i < lines.Length; i++)
        {
            // Order matters - specific paths first, then generic
            // Convert .claude/commands/ to .github/copilot/workflows/
            lines[i] = lines[i].Replace(".claude/commands/", ".github/copilot/workflows/");
            // Convert .claude/rules/ to .github/copilot/rules/
            lines[i] = lines[i].Replace(".claude/rules/", ".github/copilot/rules/");
            // Convert remaining .claude/ references to .github/copilot/
            lines[i] = lines[i].Replace(".claude/", ".github/copilot/");
        }
    }

    private static void SyncClaudeToAntigravityWorkflows(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);
            ConvertClaudeToAntigravity(sourceFile, targetFile);
        }
    }

    private static void SyncClaudeToAntigravityRules(string sourceDirectory, string targetDirectory, HashSet<string> expectedFiles)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            expectedFiles.Add(targetFile);
            ConvertClaudeToAntigravity(sourceFile, targetFile);
        }
    }

    private static void ConvertClaudeToAntigravity(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);

        // Skip path conversion for update-ai-rules file
        var skipFile = IsUpdateAiRulesFile(sourceFile);

        // Build output preserving frontmatter (Antigravity uses same format as Claude)
        var outputLines = new List<string>();

        if (frontmatterLines.Count > 0)
        {
            var frontmatterDict = ParseFrontmatter(frontmatterLines);
            outputLines.Add("---");

            // Convert paths to trigger + globs for Antigravity
            if (frontmatterDict.TryGetValue("paths", out var paths))
            {
                outputLines.Add("trigger: glob");
                outputLines.Add($"globs: {paths}");
            }
            else
            {
                // Preserve trigger field
                if (frontmatterDict.TryGetValue("trigger", out var trigger))
                {
                    outputLines.Add($"trigger: {trigger}");
                }

                // Preserve globs field
                if (frontmatterDict.TryGetValue("globs", out var globs))
                {
                    outputLines.Add($"globs: {globs}");
                }
            }

            // Preserve description field
            if (frontmatterDict.TryGetValue("description", out var description))
            {
                outputLines.Add($"description: {description}");
            }

            outputLines.Add("---");
        }

        // Skip leading empty line if present
        var contentToAdd = contentLines.ToList();
        if (contentToAdd.Count > 0 && string.IsNullOrWhiteSpace(contentToAdd[0]))
        {
            contentToAdd.RemoveAt(0);
        }

        outputLines.AddRange(contentToAdd);

        // Replace Claude references with Antigravity references
        var outputArray = outputLines.ToArray();
        ReplaceClaudeReferencesWithAntigravity(outputArray, skipFile);

        File.WriteAllLines(targetFile, outputArray);
    }

    private static void ReplaceClaudeReferencesWithAntigravity(string[] lines, bool skipFile = false)
    {
        if (skipFile) return;

        for (var i = 0; i < lines.Length; i++)
        {
            // Order matters - specific paths first, then generic
            // Convert .claude/commands/ to .agent/workflows/
            lines[i] = lines[i].Replace(".claude/commands/", ".agent/workflows/");
            // Convert .claude/rules/ to .agent/rules/
            lines[i] = lines[i].Replace(".claude/rules/", ".agent/rules/");
            // Convert remaining .claude/ references to .agent/
            lines[i] = lines[i].Replace(".claude/", ".agent/");
        }
    }
}
