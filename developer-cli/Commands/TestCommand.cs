using System.CommandLine;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Runs tests from a solution")
    {
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to test (e.g., account-management, back-office)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring the solution before running tests" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };

        Options.Add(selfContainedSystemOption);
        Options.Add(noBuildOption);
        Options.Add(quietOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(noBuildOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private void Execute(string? selfContainedSystem, bool noBuild, bool quiet)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        try
        {
            var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

            if (!noBuild)
            {
                ProcessHelper.Run($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName, "Build", quiet);
            }

            var testCommand = quiet
                ? $"""dotnet test {solutionFile.Name} --no-build --no-restore --logger "console;verbosity=detailed" """
                : $"dotnet test {solutionFile.Name} --no-build --no-restore";

            ProcessHelper.Run(testCommand, solutionFile.Directory?.FullName, "Tests", quiet);

            if (quiet)
            {
                Console.WriteLine("Tests passed.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tests failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
