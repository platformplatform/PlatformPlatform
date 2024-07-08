using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

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
        PrerequisitesChecker.Check("dotnet");

        var solutionFile = SolutionHelper.GetSolution(solutionName);

        ProcessHelper.StartProcess($"dotnet test {solutionFile.Name}", solutionFile.Directory?.FullName);

        return 0;
    }
}
