using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class CodeCleanupCommand : Command
{
    public CodeCleanupCommand() : base("code-cleanup", "Run JetBrains Code Cleanup")
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
        ProcessHelper.StartProcess($"dotnet jb cleanupcode {solutionFile.Name} --profile=\".NET only\"", solutionFile.Directory!.FullName);

        AnsiConsole.MarkupLine("[green]Code cleanup completed. Check Git to see any changes![/]");

        return 0;
    }
}
