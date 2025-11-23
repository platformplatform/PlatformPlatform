using System.CommandLine;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Runs tests from a solution")
    {
        var solutionNameOption = new Option<string?>("<solution-name>", "--solution-name", "-s") { Description = "The name of the solution file containing the tests to run" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring the solution before running tests" };

        Options.Add(solutionNameOption);
        Options.Add(noBuildOption);

        this.SetAction(parseResult => Execute(
            parseResult.GetValue(solutionNameOption),
            parseResult.GetValue(noBuildOption)
        ));
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
