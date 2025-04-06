using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

public class BuildCommand : Command
{
    public BuildCommand() : base("build", "Builds a self-contained system")
    {
        AddOption(new Option<string?>(["<solution-name>", "--solution-name", "-s"], "The name of the self-contained system to build"));

        Handler = CommandHandler.Create<string?>(Execute);
    }

    private static void Execute(string? solutionName)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var solutionFile = SolutionHelper.GetSolution(solutionName);

        ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
    }
}
