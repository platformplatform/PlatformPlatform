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

        Handler = CommandHandler.Create<string?, bool, bool, bool, bool>(Execute);
    }

    private void Execute(string? selfContainedSystem, bool noBuild, bool backend, bool frontend, bool quiet)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var testBackend = backend || !frontend;
        var testFrontend = frontend || !backend;

        if (quiet)
        {
            ExecuteQuiet(selfContainedSystem, noBuild, testBackend, testFrontend);
        }
        else
        {
            ExecuteVerbose(selfContainedSystem, noBuild, testBackend, testFrontend);
        }
    }

    private void ExecuteVerbose(string? selfContainedSystem, bool noBuild, bool testBackend, bool testFrontend)
    {
        if (testBackend)
        {
            if (selfContainedSystem is null)
            {
                // Test all self-contained systems
                var systems = SelfContainedSystemHelper.GetAvailableSelfContainedSystems();
                foreach (var system in systems)
                {
                    var solutionFile = SelfContainedSystemHelper.GetSolutionFile(system);

                    if (!noBuild)
                    {
                        ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                    }

                    ProcessHelper.StartProcess($"dotnet test {solutionFile.Name} --no-build --no-restore", solutionFile.Directory?.FullName);
                }
            }
            else
            {
                // Test specific system
                var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

                if (!noBuild)
                {
                    ProcessHelper.StartProcess($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                }

                ProcessHelper.StartProcess($"dotnet test {solutionFile.Name} --no-build --no-restore", solutionFile.Directory?.FullName);
            }
        }

        if (testFrontend)
        {
            ProcessHelper.StartProcess("npm test", Configuration.ApplicationFolder);
        }
    }

    private void ExecuteQuiet(string? selfContainedSystem, bool noBuild, bool testBackend, bool testFrontend)
    {
        try
        {
            if (testBackend)
            {
                if (selfContainedSystem is null)
                {
                    // Test all self-contained systems
                    var systems = SelfContainedSystemHelper.GetAvailableSelfContainedSystems();
                    foreach (var system in systems)
                    {
                        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(system);

                        if (!noBuild)
                        {
                            var buildResult = ProcessHelper.ExecuteQuietly($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                            if (!buildResult.Success)
                            {
                                Console.WriteLine($"Build failed for {system}.");
                                Console.WriteLine(buildResult.CombinedOutput);
                                Environment.Exit(1);
                            }
                        }

                        var testResult = ProcessHelper.ExecuteQuietly($"dotnet test {solutionFile.Name} --no-build --no-restore", solutionFile.Directory?.FullName);
                        if (!testResult.Success)
                        {
                            Console.WriteLine($"Tests failed for {system}.");
                            Console.WriteLine(testResult.CombinedOutput);
                            Environment.Exit(1);
                        }
                    }
                }
                else
                {
                    // Test specific system
                    var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

                    if (!noBuild)
                    {
                        var buildResult = ProcessHelper.ExecuteQuietly($"dotnet build {solutionFile.Name}", solutionFile.Directory?.FullName);
                        if (!buildResult.Success)
                        {
                            Console.WriteLine("Build failed.");
                            Console.WriteLine(buildResult.CombinedOutput);
                            Environment.Exit(1);
                        }
                    }

                    var testResult = ProcessHelper.ExecuteQuietly($"dotnet test {solutionFile.Name} --no-build --no-restore", solutionFile.Directory?.FullName);
                    if (!testResult.Success)
                    {
                        Console.WriteLine("Tests failed.");
                        Console.WriteLine(testResult.CombinedOutput);
                        Environment.Exit(1);
                    }
                }
            }

            if (testFrontend)
            {
                var result = ProcessHelper.ExecuteQuietly("npm test", Configuration.ApplicationFolder);
                if (!result.Success)
                {
                    Console.WriteLine("Frontend tests failed.");
                    Console.WriteLine(result.CombinedOutput);
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
