using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public static class SolutionHelper
{
    public static FileInfo GetSolution(string? solutionName)
    {
        if (solutionName is not null)
        {
            var fileInfo = FindSolutionFile(solutionName);
            if (fileInfo is not null)
            {
                return fileInfo;
            }

            AnsiConsole.MarkupLine($"[red]ERROR:[/] Solution file [yellow]{solutionName}[/] not found.");
            Environment.Exit(1);
        }

        var solutionFiles = GetSolutionFiles();

        if (solutionFiles.Count == 1)
        {
            solutionName = solutionFiles.Keys.Single();
        }
        else if (solutionFiles.Count > 1)
        {
            var prompt = new SelectionPrompt<string>()
                .Title("Please select a solution")
                .AddChoices(solutionFiles.Keys);

            solutionName = AnsiConsole.Prompt(prompt);
        }
        else
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] No solution files found.");
            Environment.Exit(1);
        }

        return new FileInfo(solutionFiles[solutionName]);
    }

    private static FileInfo? FindSolutionFile(string solutionPath)
    {
        // Test if it exists as an exact path
        if (File.Exists(solutionPath))
        {
            return new FileInfo(solutionPath);
        }

        // Test if it exists as a relative path to the application folder
        var applicationFolderPath = Path.Combine(Configuration.ApplicationFolder, solutionPath);
        if (File.Exists(applicationFolderPath))
        {
            return new FileInfo(applicationFolderPath);
        }

        // Test if it exists as a relative path to the source code  folder
        var sourceCodeFolderPath = Path.Combine(Configuration.SourceCodeFolder, solutionPath);
        if (File.Exists(sourceCodeFolderPath))
        {
            return new FileInfo(sourceCodeFolderPath);
        }

        // Test if a file with that name exists in any subdirectory
        var fileName = Path.GetFileName(solutionPath);
        var matchingFiles = Directory.GetFiles(Configuration.ApplicationFolder, fileName, SearchOption.AllDirectories);
        return matchingFiles.Length > 0 ? new FileInfo(matchingFiles[0]) : null;
    }

    private static Dictionary<string, string> GetSolutionFiles()
    {
        return Directory
            .GetFiles(Configuration.ApplicationFolder, "*.slnx", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToDictionary(s => new FileInfo(s).Name.Replace(".slnx", ""), s => s);
    }
}
