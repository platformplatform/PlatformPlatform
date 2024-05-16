using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class CodeInspectionsCommand : Command
{
    public CodeInspectionsCommand() : base("code-inspections", "Run JetBrains Code Inspections")
    {
        var solutionNameOption = new Option<string?>(
            ["<solution-name>", "--solution-name", "-s"],
            "The name of the self-contained system to build"
        );

        AddOption(solutionNameOption);

        Handler = CommandHandler.Create(Execute);
    }

    private int Execute(string? solutionName)
    {
        PrerequisitesChecker.Check("dotnet");

        var solutionFile = SolutionHelper.GetSolution(solutionName);

        ProcessHelper.StartProcess("dotnet tool restore", solutionFile.Directory!.FullName);

        ProcessHelper.StartProcess(
            $"dotnet jb inspectcode {solutionFile.Name} --build --output=result.json --severity=SUGGESTION",
            solutionFile.Directory!.FullName
        );

        var resultXml = File.ReadAllText(Path.Combine(solutionFile.Directory!.FullName, "result.json"));
        if (resultXml.Contains("\"results\": [],"))
        {
            AnsiConsole.MarkupLine("[green]No issues found![/]");
        }
        else
        {
            ProcessHelper.StartProcess("code result.json", solutionFile.Directory!.FullName);
        }

        return 0;
    }
}
