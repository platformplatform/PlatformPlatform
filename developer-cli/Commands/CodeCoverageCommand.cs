using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class CodeCoverageCommand : Command
{
    public CodeCoverageCommand() : base("code-coverage", "Run JetBrains Code Coverage")
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
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var solutionFile = SolutionHelper.GetSolution(solutionName);

        ProcessHelper.StartProcess("dotnet tool restore", solutionFile.Directory!.FullName);

        ProcessHelper.StartProcess("dotnet build", solutionFile.Directory!.FullName);

        var solutionFileWithoutExtension = solutionFile.Name.Replace(solutionFile.Extension, "");

        ProcessHelper.StartProcess(
            $"dotnet dotcover test {solutionFile.Name} --no-build --dcOutput=coverage/dotCover.html --dcReportType=HTML --dcFilters=\"+:{solutionFileWithoutExtension}.*;-:*.Tests;-:type=*.AppHost.*\"",
            Configuration.ApplicationFolder
        );

        var codeCoverageReport = Path.Combine(Configuration.ApplicationFolder, "coverage", "dotCover.html");
        AnsiConsole.MarkupLine($"[green]Code Coverage Report.[/] {codeCoverageReport}");
        ProcessHelper.StartProcess($"open {codeCoverageReport}", Configuration.ApplicationFolder);

        return 0;
    }
}
