using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Runs tests from a solution")
    {
        AddOption(new Option<string?>(["<solution-name>", "--solution-name", "-s"], "The name of the solution file containing the tests to run"));
        AddOption(new Option<bool>(["--no-build"], () => false, "Skip building and restoring the solution before running tests"));

        Handler = CommandHandler.Create<string?, bool>(Execute);
    }

    private void Execute(string? solutionName, bool noBuild)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var solutionFile = SolutionHelper.GetSolution(solutionName);

        if (!noBuild)
        {
            ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
        }

        ProcessHelper.StartProcess($"dotnet test {solutionFile.Name} --no-build --no-restore", solutionFile.Directory?.FullName);
    }
}
