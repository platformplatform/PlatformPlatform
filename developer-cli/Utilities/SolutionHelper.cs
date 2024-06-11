using PlatformPlatform.DeveloperCli.Installation;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Utilities;

public class SolutionHelper
{
    public static FileInfo GetSolution(string? solutionName)
    {
        var workingDirectory = Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application");
        
        var solutionsFiles = Directory
            .GetFiles(workingDirectory, "*.sln", SearchOption.AllDirectories)
            .ToDictionary(s => new FileInfo(s).Name.Replace(".sln", ""), s => s);
        
        if (solutionName is not null && !solutionsFiles.ContainsKey(solutionName))
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Solution [yellow]{solutionName}[/] not found.");
            Environment.Exit(1);
        }
        
        if (solutionsFiles.Count == 1)
        {
            solutionName = solutionsFiles.Keys.Single();
        }
        
        if (solutionName is null)
        {
            var prompt = new SelectionPrompt<string>()
                .Title("Please select an option")
                .AddChoices(solutionsFiles.Keys);
            
            solutionName = AnsiConsole.Prompt(prompt);
        }
        
        return new FileInfo(solutionsFiles[solutionName]);
    }
}
