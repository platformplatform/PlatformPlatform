using System.CommandLine;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class CoverageCommand : Command
{
    public CoverageCommand() : base("coverage", "Run JetBrains Code Coverage")
    {
        SetAction(_ => Execute());
    }

    private static void Execute()
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(null);

        ProcessHelper.StartProcess("dotnet tool restore", solutionFile.Directory!.FullName);

        ProcessHelper.StartProcess("dotnet build", solutionFile.Directory!.FullName);

        ProcessHelper.StartProcess(
            $"dotnet dotcover test {solutionFile.Name} --no-build --dcOutput=coverage/dotCover.html --dcReportType=HTML --dcFilters=\"+:*;-:*.Tests;-:type=*.AppHost.*\"",
            Configuration.ApplicationFolder
        );

        var codeCoverageReport = Path.Combine(Configuration.ApplicationFolder, "coverage", "dotCover.html");
        AnsiConsole.MarkupLine($"[green]Code Coverage Report.[/] {codeCoverageReport}");
        ProcessHelper.StartProcess($"open {codeCoverageReport}", Configuration.ApplicationFolder);
    }
}
