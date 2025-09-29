using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Security.Cryptography;
using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class SyncAiRulesAndWorkflowsCommand : Command
{
    public SyncAiRulesAndWorkflowsCommand() : base("sync-ai-rules", "Sync AI rules and workflows from .claude to .windsurf and .cursor, converting formats appropriately.")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private static void Execute()
    {
        // Source directories from .claude
        var claudeRoot = Path.Combine(Configuration.SourceCodeFolder, ".claude");
        var claudeCommands = Path.Combine(claudeRoot, "commands");
        var claudeAgents = Path.Combine(claudeRoot, "agents");
        var claudeRules = Path.Combine(claudeRoot, "rules");
        var claudeSamples = Path.Combine(claudeRoot, "samples");
        
        // Target directories for Windsurf
        var windsurfWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/workflows");
        var windsurfRules = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/rules");
        var windsurfSamples = Path.Combine(Configuration.SourceCodeFolder, ".windsurf/rules");
        
        // Target directories for Cursor
        var cursorWorkflows = Path.Combine(Configuration.SourceCodeFolder, ".cursor/rules/workflows");
        var cursorRules = Path.Combine(Configuration.SourceCodeFolder, ".cursor/rules");
        var cursorSamples = Path.Combine(Configuration.SourceCodeFolder, ".cursor/samples");

        // Create dictionaries to track file changes
        var initialFileHashes = new Dictionary<string, string>();
        var finalFileHashes = new Dictionary<string, string>();

        // Collect initial file hashes for both .windsurf and .cursor
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".windsurf"), initialFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".cursor"), initialFileHashes);

        try
        {
            // Sync to Windsurf
            // Commands and Agents → workflows
            SyncClaudeToWindsurfWorkflows(claudeCommands, windsurfWorkflows);
            SyncClaudeToWindsurfWorkflows(claudeAgents, windsurfWorkflows);
            // Rules → rules
            SyncClaudeToWindsurfRules(claudeRules, windsurfRules);
            // Samples → rules (same as rules for Windsurf)
            SyncClaudeToWindsurfRules(claudeSamples, windsurfSamples);
            
            // Sync to Cursor
            // Commands and Agents → rules/workflows
            SyncClaudeToCursorWorkflows(claudeCommands, cursorWorkflows);
            SyncClaudeToCursorWorkflows(claudeAgents, cursorWorkflows);
            // Rules → rules
            SyncClaudeToCursorRules(claudeRules, cursorRules, cursorWorkflows);
            // Samples → samples (simple copy for Cursor)
            SyncClaudeToPlainMarkdown(claudeSamples, cursorSamples);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
            Environment.Exit(1);
        }

        // Collect final file hashes
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".windsurf"), finalFileHashes);
        CollectFileHashes(Path.Combine(Configuration.SourceCodeFolder, ".cursor"), finalFileHashes);

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

    private static void SyncClaudeToWindsurfWorkflows(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);
        var targetFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            ConvertClaudeToWindsurfWorkflow(sourceFile, targetFile);
            targetFiles.Add(targetFile);
        }
    }
    
    private static void SyncClaudeToWindsurfRules(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);
        var targetFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            // Rules in Windsurf keep the same format as Claude but need reference conversion
            var lines = File.ReadAllLines(sourceFile);
            var modifiedLines = new string[lines.Length];
            
            // Convert .claude references to .windsurf references
            for (var i = 0; i < lines.Length; i++)
            {
                modifiedLines[i] = lines[i].Replace("](/.claude/", "](/.windsurf/");
            }
            
            var linesList = modifiedLines.ToList();
            // Remove trailing empty lines
            while (linesList.Count > 0 && string.IsNullOrWhiteSpace(linesList[linesList.Count - 1]))
            {
                linesList.RemoveAt(linesList.Count - 1);
            }
            // Use WriteAllText with Join to avoid extra newline at end
            File.WriteAllText(targetFile, string.Join(Environment.NewLine, linesList));
            targetFiles.Add(targetFile);
        }
    }
    
    private static void SyncClaudeToCursorWorkflows(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);
        var targetFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            // Change .md to .mdc for Cursor
            var targetFileName = Path.GetFileNameWithoutExtension(relativePath) + ".mdc";
            var targetFile = Path.Combine(targetDirectory, Path.GetDirectoryName(relativePath) ?? "", targetFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            ConvertClaudeToCursorWorkflow(sourceFile, targetFile);
            targetFiles.Add(targetFile);
        }
    }
    
    private static void SyncClaudeToCursorRules(string sourceDirectory, string targetDirectory, string workflowsDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);
        var targetFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            // Change .md to .mdc for Cursor
            var targetFileName = Path.GetFileNameWithoutExtension(relativePath) + ".mdc";
            var targetFile = Path.Combine(targetDirectory, Path.GetDirectoryName(relativePath) ?? "", targetFileName);
            
            // Skip if this would conflict with a workflow file
            if (targetFile.StartsWith(workflowsDirectory)) continue;
            
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            ConvertClaudeToCursorRule(sourceFile, targetFile);
            targetFiles.Add(targetFile);
        }
    }

    private static void SyncClaudeToPlainMarkdown(string sourceDirectory, string targetDirectory)
    {
        if (!Directory.Exists(sourceDirectory)) return;
        Directory.CreateDirectory(targetDirectory);

        var sourceFiles = Directory.GetFiles(sourceDirectory, "*.md", SearchOption.AllDirectories);
        var targetFiles = new List<string>();

        foreach (var sourceFile in sourceFiles)
        {
            var relativePath = Path.GetRelativePath(sourceDirectory, sourceFile);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile) ?? "");

            // Simple copy - no format conversion needed for samples
            var lines = File.ReadAllLines(sourceFile).ToList();
            // Remove trailing empty lines
            while (lines.Count > 0 && string.IsNullOrWhiteSpace(lines[lines.Count - 1]))
            {
                lines.RemoveAt(lines.Count - 1);
            }
            // Use WriteAllText with Join to avoid extra newline at end
            File.WriteAllText(targetFile, string.Join(Environment.NewLine, lines));
            targetFiles.Add(targetFile);
        }
    }
    
    private static void ConvertClaudeToWindsurfWorkflow(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);
        
        // Only add frontmatter if source file has frontmatter
        if (frontmatterLines.Count > 0)
        {
            var frontmatterDict = ParseFrontmatter(frontmatterLines);
            
            // Convert frontmatter for Windsurf workflows
            var newFrontmatter = new List<string> { "---" };
            
            // Get description from Claude frontmatter
            if (frontmatterDict.TryGetValue("description", out var description))
            {
                // For workflows, prepend "Workflow for" if not already present
                if (!description.StartsWith("Workflow for", StringComparison.OrdinalIgnoreCase))
                {
                    description = $"Workflow for {description.ToLower()}";
                }
                newFrontmatter.Add($"description: {description}");
            }
            
            // Add auto_execution_mode for workflows
            newFrontmatter.Add("auto_execution_mode: 1");
            newFrontmatter.Add("---");
            
            // Keep content as-is (including $ARGUMENTS)
            var allLines = newFrontmatter.Concat(contentLines).ToList();

            // Convert .claude references to .windsurf references
            for (var i = 0; i < allLines.Count; i++)
            {
                allLines[i] = allLines[i].Replace("](/.claude/", "](/.windsurf/");
            }

            // Remove trailing empty lines
            while (allLines.Count > 0 && string.IsNullOrWhiteSpace(allLines[allLines.Count - 1]))
            {
                allLines.RemoveAt(allLines.Count - 1);
            }
            // Use WriteAllText with Join to avoid extra newline at end
            File.WriteAllText(targetFile, string.Join(Environment.NewLine, allLines));
        }
        else
        {
            // No frontmatter in source, just copy content as-is
            var allLines = contentLines.ToList();

            // Convert .claude references to .windsurf references
            for (var i = 0; i < allLines.Count; i++)
            {
                allLines[i] = allLines[i].Replace("](/.claude/", "](/.windsurf/");
            }

            // Remove trailing empty lines
            while (allLines.Count > 0 && string.IsNullOrWhiteSpace(allLines[allLines.Count - 1]))
            {
                allLines.RemoveAt(allLines.Count - 1);
            }
            // Use WriteAllText with Join to avoid extra newline at end
            File.WriteAllText(targetFile, string.Join(Environment.NewLine, allLines));
        }
    }
    
    private static void ConvertClaudeToCursorWorkflow(string sourceFile, string targetFile)
    {
        var lines = File.ReadAllLines(sourceFile);
        var (frontmatterLines, contentLines) = SplitFrontmatter(lines);
        
        // Keep content as-is (including $ARGUMENTS) but replace references
        var modifiedContent = contentLines.ToArray();
        ReplaceclaudeReferencesWithCursor(modifiedContent);
        
        // Only add frontmatter if source file has frontmatter
        if (frontmatterLines.Count > 0)
        {
            var frontmatterDict = ParseFrontmatter(frontmatterLines);
            
            // Convert frontmatter for Cursor workflows
            var newFrontmatter = new List<string> { "---" };
            
            // Get description from Claude frontmatter
            if (frontmatterDict.TryGetValue("description", out var description))
            {
                // For workflows, prepend "Workflow for" if not already present
                if (!description.StartsWith("Workflow for", StringComparison.OrdinalIgnoreCase))
                {
                    description = $"Workflow for {description.ToLower()}";
                }
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
            // Remove trailing empty lines
            while (allLines.Count > 0 && string.IsNullOrWhiteSpace(allLines[allLines.Count - 1]))
            {
                allLines.RemoveAt(allLines.Count - 1);
            }
            File.WriteAllLines(targetFile, allLines);
        }
        else
        {
            // No frontmatter in source, just copy content with reference replacements
            var allLines = modifiedContent.ToList();
            // Remove trailing empty lines
            while (allLines.Count > 0 && string.IsNullOrWhiteSpace(allLines[allLines.Count - 1]))
            {
                allLines.RemoveAt(allLines.Count - 1);
            }
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
        
        // Convert trigger patterns
        if (frontmatterDict.TryGetValue("trigger", out var trigger))
        {
            switch (trigger)
            {
                case "always_on":
                    if (frontmatterDict.TryGetValue("description", out var desc))
                        newFrontmatter.Add($"description: {desc}");
                    if (frontmatterDict.TryGetValue("globs", out var globs) && !string.IsNullOrWhiteSpace(globs))
                        newFrontmatter.Add($"globs: {globs}");
                    else
                        newFrontmatter.Add("globs: ");
                    newFrontmatter.Add("alwaysApply: true");
                    break;
                    
                case "glob":
                    if (frontmatterDict.TryGetValue("description", out desc))
                        newFrontmatter.Add($"description: {desc}");
                    if (frontmatterDict.TryGetValue("globs", out globs))
                        newFrontmatter.Add($"globs: {globs}");
                    newFrontmatter.Add("alwaysApply: false");
                    break;
                    
                case "model_decision":
                    if (frontmatterDict.TryGetValue("description", out desc))
                        newFrontmatter.Add($"description: {desc}");
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
                        newFrontmatter.Add($"description: {desc}");
                    if (frontmatterDict.TryGetValue("globs", out globs))
                        newFrontmatter.Add($"globs: {globs}");
                    newFrontmatter.Add("alwaysApply: false");
                    break;
            }
        }
        else
        {
            // No trigger field, use defaults
            if (frontmatterDict.TryGetValue("description", out var desc))
                newFrontmatter.Add($"description: {desc}");
            if (frontmatterDict.TryGetValue("globs", out var globs))
                newFrontmatter.Add($"globs: {globs}");
            newFrontmatter.Add("alwaysApply: false");
        }
        
        newFrontmatter.Add("---");
        
        // Replace .claude references with .cursor references
        var modifiedContent = contentLines.ToArray();
        ReplaceclaudeReferencesWithCursor(modifiedContent);
        
        // Combine frontmatter and content
        // Skip first line of content if it's empty (to avoid double blank line after frontmatter)
        var contentToWrite = modifiedContent.ToList();
        if (contentToWrite.Count > 0 && string.IsNullOrWhiteSpace(contentToWrite[0]))
        {
            contentToWrite.RemoveAt(0);
        }
        var allLines = newFrontmatter.Concat(contentToWrite).ToList();
        // Remove trailing empty lines
        while (allLines.Count > 0 && string.IsNullOrWhiteSpace(allLines[allLines.Count - 1]))
        {
            allLines.RemoveAt(allLines.Count - 1);
        }
        File.WriteAllLines(targetFile, allLines);
    }
    
    
    private static void ReplaceclaudeReferencesWithCursor(string[] lines)
    {
        for (var i = 0; i < lines.Length; i++)
        {
            // Handle markdown links - convert paths to use mdc: prefix for Cursor
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], 
                @"\[([^\]]+)\]\(([^)]+)\)", 
                m => 
                {
                    var linkText = m.Groups[1].Value;
                    var linkPath = m.Groups[2].Value;
                    
                    // Also convert .md to .mdc in link text if it's a filename
                    if (linkText.EndsWith(".md"))
                    {
                        linkText = linkText.Replace(".md", ".mdc");
                    }
                    
                    // Skip if already has mdc: prefix or is external URL
                    if (linkPath.StartsWith("mdc:") || linkPath.StartsWith("http"))
                    {
                        // But still update the link text if needed
                        return $"[{linkText}]({linkPath})";
                    }
                    
                    // Handle absolute paths - add mdc: prefix
                    if (linkPath.StartsWith("/"))
                    {
                        // Convert absolute paths to mdc: format
                        // Remove leading slash and change .md to .mdc
                        var mdcPath = linkPath.Substring(1).Replace(".md", ".mdc");
                        
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
                            // Move commands and agents to rules/workflows
                            if (mdcPath.Contains("/commands/"))
                            {
                                mdcPath = mdcPath.Replace(".cursor/commands/", ".cursor/rules/workflows/");
                            }
                            else if (mdcPath.Contains("/agents/"))
                            {
                                mdcPath = mdcPath.Replace(".cursor/agents/", ".cursor/rules/workflows/");
                            }
                        }
                        
                        return $"[{linkText}](mdc:{mdcPath})";
                    }
                    
                    // Handle relative paths - also need to convert .md to .mdc
                    if (linkPath.EndsWith(".md"))
                    {
                        linkPath = linkPath.Replace(".md", ".mdc");
                    }
                    return $"[{linkText}]({linkPath})";
                });
            
            // Replace any remaining .claude references with .cursor
            lines[i] = lines[i].Replace(".claude", ".cursor");
            lines[i] = lines[i].Replace(".windsurf", ".cursor");
            
            // Handle directory mappings for non-link references
            lines[i] = lines[i].Replace(".cursor/commands", ".cursor/rules/workflows");
            lines[i] = lines[i].Replace(".cursor/agents", ".cursor/rules/workflows");
            lines[i] = lines[i].Replace(".cursor/workflows", ".cursor/rules/workflows");
            
            // Replace any remaining .md references to .mdc in plain text (not in links)
            // This handles cases like "Step 2: Inspect all *.md files"
            lines[i] = System.Text.RegularExpressions.Regex.Replace(lines[i], 
                @"(\*\.md\b|\.md\s|\.md$)", 
                m => m.Value.Replace(".md", ".mdc"));
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
