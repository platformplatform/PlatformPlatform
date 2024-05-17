using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Builds a self-contained system")
    {
        var solutionNameOption = new Option<string?>(
            ["<solution-name>", "--solution-name", "-s"],
            "The name of the self-contained system to build"
        );

        AddOption(solutionNameOption);

        Handler = CommandHandler.Create<string?>(Execute);
    }

    private int Execute(string? solutionName)
    {
        PrerequisitesChecker.Check("dotnet", "aspire", "node", "yarn");

        var solutionFile = SolutionHelper.GetSolution(solutionName);

        ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);

        return 0;
    }
}
