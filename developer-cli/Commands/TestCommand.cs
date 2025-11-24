using System.CommandLine;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Runs tests from a solution")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "This command is always only backend. The option is only here for consistency." };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to test (e.g., account-management, back-office)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring the solution before running tests" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };
        var filterOption = new Option<string?>("--filter") { Description = "Filter tests by name (dotnet test --filter)" };

        Options.Add(backendOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(noBuildOption);
        Options.Add(quietOption);
        Options.Add(filterOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(noBuildOption),
                parseResult.GetValue(quietOption),
                parseResult.GetValue(filterOption)
            )
        );
    }

    private void Execute(string? selfContainedSystem, bool noBuild, bool quiet, string? filter)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        try
        {
            var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

            if (!noBuild)
            {
                var buildCommand = quiet
                    ? $"dotnet build {solutionFile.Name}"
                    : $"dotnet build {solutionFile.Name} --verbosity quiet";

                ProcessHelper.Run(buildCommand, solutionFile.Directory?.FullName, "Build", quiet);
            }

            var filterArgument = filter is not null ? $""" --filter "({filter})" """ : "";
            var testCommand = $"""dotnet test {solutionFile.Name} --no-build --no-restore --logger "console;verbosity=normal"{filterArgument}""";

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
