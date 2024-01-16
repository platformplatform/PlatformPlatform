using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class CodeCoverageCommand : Command
{
    public CodeCoverageCommand() : base("code-coverage", "Run JetBrains Code Coverage")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private int Execute()
    {
        var workingDirectory = new DirectoryInfo(Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application"))
            .FullName;

        ProcessHelper.StartProcess("dotnet tool restore", workingDirectory);

        ProcessHelper.StartProcess("dotnet build", workingDirectory);

        ProcessHelper.StartProcess(
            "dotnet dotcover test PlatformPlatform.sln --no-build --dcOutput=coverage/dotCover.html --dcReportType=HTML --dcFilters=\"+:PlatformPlatform.*;-:*.Tests;-:type=*.AppHost.*\"",
            workingDirectory
        );

        var codeCoverageReport = Path.Combine(workingDirectory, "coverage", "dotCover.html");
        AnsiConsole.MarkupLine($"[green]Code Coverage Report[/] {codeCoverageReport}");
        ProcessHelper.StartProcess($"open {codeCoverageReport}", workingDirectory);

        return 0;
    }
}