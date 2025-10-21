using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

public class TestCommand : Command
{
    public TestCommand() : base("test", "Runs tests from a solution")
    {
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], "The name of the self-contained system to test (e.g., account-management, back-office)"));
        AddOption(new Option<bool>(["--no-build"], () => false, "Skip building and restoring the solution before running tests"));
        AddOption(new Option<bool>(["--backend", "-b"], "Run only backend tests"));
        AddOption(new Option<bool>(["--frontend", "-f"], "Run only frontend tests"));
        AddOption(new Option<bool>(["--quiet", "-q"], "Minimal output mode"));
        AddOption(new Option<string?>(["--filter"], "Filter tests by name (dotnet test --filter)"));

        Handler = CommandHandler.Create<string?, bool, bool, bool, bool, string?>(Execute);
    }

    private void Execute(string? selfContainedSystem, bool noBuild, bool backend, bool frontend, bool quiet, string? filter)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var testBackend = backend || !frontend;
        var testFrontend = frontend || !backend;

        if (quiet)
        {
            ExecuteQuiet(selfContainedSystem, noBuild, testBackend, testFrontend, filter);
        }
        else
        {
            ExecuteVerbose(selfContainedSystem, noBuild, testBackend, testFrontend, filter);
        }
    }

    private void ExecuteVerbose(string? selfContainedSystem, bool noBuild, bool testBackend, bool testFrontend, string? filter)
    {
        if (testBackend)
        {
            var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

            if (!noBuild)
            {
                ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
            }

            var filterArg = filter != null ? $" --filter \"{filter}\"" : "";
            ProcessHelper.StartProcess($"dotnet test {solutionFile.Name} --no-build --no-restore{filterArg}", solutionFile.Directory?.FullName);
        }

        if (testFrontend)
        {
            ProcessHelper.StartProcess("npm test", Configuration.ApplicationFolder);
        }
    }

    private void ExecuteQuiet(string? selfContainedSystem, bool noBuild, bool testBackend, bool testFrontend, string? filter)
    {
        try
        {
            if (testBackend)
            {
                var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

                if (!noBuild)
                {
                    var buildResult = ProcessHelper.ExecuteQuietly($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                    if (!buildResult.Success)
                    {
                        Console.WriteLine(buildResult.GetErrorSummary("Build"));
                        Environment.Exit(1);
                    }
                }

                var filterArg = filter != null ? $" --filter \"{filter}\"" : "";
                // Add detailed console logger to capture EF Core and ASP.NET diagnostic logs (fail: messages)
                var loggerArg = " --logger \"console;verbosity=detailed\"";
                var testResult = ProcessHelper.ExecuteQuietly($"dotnet test {solutionFile.Name} --no-build --no-restore{filterArg}{loggerArg}", solutionFile.Directory?.FullName);
                if (!testResult.Success)
                {
                    Console.WriteLine(testResult.GetErrorSummary("Tests"));
                    Environment.Exit(1);
                }
            }

            if (testFrontend)
            {
                var result = ProcessHelper.ExecuteQuietly("npm test", Configuration.ApplicationFolder);
                if (!result.Success)
                {
                    Console.WriteLine(result.GetErrorSummary("Frontend tests"));
                    Environment.Exit(1);
                }
            }

            Console.WriteLine("Tests passed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tests failed: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
