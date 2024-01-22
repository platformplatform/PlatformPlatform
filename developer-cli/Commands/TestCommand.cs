using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class TestCommand : Command
{
    public TestCommand() : base("test", "Runs tests from a solution")
    {
        var solutionNameOption = new Option<string?>(
            ["<solution-name>", "--solution-name", "-s"],
            "The name of the solution file containing the tests to run"
        );

        AddOption(solutionNameOption);

        Handler = CommandHandler.Create<string?>(Execute);
    }

    private int Execute(string? solutionName)
    {
        var workingDirectory = Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application");

        var solutionsFiles = Directory
            .GetFiles(workingDirectory, "*.sln", SearchOption.AllDirectories)
            .ToDictionary(s => new FileInfo(s).Name.Replace(".sln", ""), s => s);

        if (solutionName is not null && !solutionsFiles.ContainsKey(solutionName))
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Solution [yellow]{solutionName}[/] not found.");
            return 1;
        }

        if (solutionName is null)
        {
            var prompt = new SelectionPrompt<string>()
                .Title("Please select an option")
                .AddChoices(solutionsFiles.Keys);

            solutionName = AnsiConsole.Prompt(prompt);
        }

        ProcessHelper.StartProcess($"dotnet test {solutionsFiles[solutionName]}");

        return 0;
    }
}