using System.CommandLine;
using System.Diagnostics;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

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
        var excludeCategoryOption = new Option<string?>("--exclude-category") { Description = "Exclude tests by category (e.g., 'Noisy', 'RequiresDocker'). Defaults to 'Noisy'." };

        Options.Add(backendOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(noBuildOption);
        Options.Add(quietOption);
        Options.Add(filterOption);
        Options.Add(excludeCategoryOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(noBuildOption),
                parseResult.GetValue(quietOption),
                parseResult.GetValue(filterOption),
                parseResult.GetValue(excludeCategoryOption)
            )
        );
    }

    private void Execute(string? selfContainedSystem, bool noBuild, bool quiet, string? filter, string? excludeCategory)
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

            var filterArgument = BuildFilterArgument(filter, excludeCategory);
            var testCommand = $"""dotnet test {solutionFile.Name} --no-build --no-restore --logger "console;verbosity=normal"{filterArgument}""";

            if (quiet)
            {
                RunTestsQuietly(testCommand, solutionFile.Directory?.FullName);
            }
            else
            {
                RunTestsWithFilteredOutput(testCommand, solutionFile.Directory?.FullName);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Tests failed: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static void RunTestsWithFilteredOutput(string command, string? workingDirectory)
    {
        if (Configuration.TraceEnabled)
        {
            AnsiConsole.MarkupLine($"[cyan]{Markup.Escape(command)}[/]");
        }

        var stats = new TestStats();
        var stopwatch = Stopwatch.StartNew();

        // Parse command to get executable and arguments
        var parts = command.Split(' ', 2);
        var executable = parts[0];
        var arguments = parts.Length > 1 ? parts[1] : "";

        var processStartInfo = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        if (workingDirectory != null)
        {
            processStartInfo.WorkingDirectory = workingDirectory;
        }

        using var process = Process.Start(processStartInfo)!;

        // Stream stdout in real-time
        while (!process.StandardOutput.EndOfStream)
        {
            var line = process.StandardOutput.ReadLine();
            if (line == null) continue;

            if (ShouldFilterLine(line)) continue;

            var trimmedLine = line.TrimStart();

            // Count test results
            if (trimmedLine.StartsWith("Passed "))
            {
                stats.Passed++;
                Console.WriteLine(line);
            }
            else if (trimmedLine.StartsWith("Failed "))
            {
                stats.Failed++;
                stats.FailedTests.Add(ExtractTestName(line));
                Console.WriteLine(line);
            }
            else if (trimmedLine.StartsWith("Skipped "))
            {
                stats.Skipped++;
                Console.WriteLine(line);
            }
            else if (!string.IsNullOrWhiteSpace(line))
            {
                // Print other non-filtered lines (e.g., error details, stack traces)
                Console.WriteLine(line);
            }
        }

        process.WaitForExit();
        stopwatch.Stop();
        var duration = stopwatch.Elapsed.TotalSeconds;

        // Print our summary
        Console.WriteLine();
        Console.WriteLine($"Test summary: total: {stats.Total}; failed: {stats.Failed}; succeeded: {stats.Passed}; skipped: {stats.Skipped}; duration: {duration:F1}s");

        if (process.ExitCode != 0)
        {
            Environment.Exit(process.ExitCode);
        }
    }

    private static void RunTestsQuietly(string command, string? workingDirectory)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = ProcessHelper.ExecuteQuietly(command, workingDirectory);
        stopwatch.Stop();

        var stats = ParseTestOutput(result.StdOut);
        var duration = stopwatch.Elapsed.TotalSeconds;

        // Print summary
        Console.WriteLine($"Test summary: total: {stats.Total}; failed: {stats.Failed}; succeeded: {stats.Passed}; skipped: {stats.Skipped}; duration: {duration:F1}s");

        // If failures, show failed test names + link to log
        if (stats.Failed > 0)
        {
            Console.WriteLine("Failed tests:");
            foreach (var test in stats.FailedTests)
            {
                Console.WriteLine($"  {test}");
            }

            Console.WriteLine($"Full output: {result.TempFilePathWithSize}");
            Environment.Exit(1);
        }

        if (result.ExitCode != 0)
        {
            Console.WriteLine($"Full output: {result.TempFilePathWithSize}");
            Environment.Exit(result.ExitCode);
        }
    }

    private static TestStats ParseTestOutput(string output)
    {
        var stats = new TestStats();

        foreach (var line in output.Split('\n'))
        {
            var trimmedLine = line.TrimStart();
            if (trimmedLine.StartsWith("Passed "))
            {
                stats.Passed++;
            }
            else if (trimmedLine.StartsWith("Failed "))
            {
                stats.Failed++;
                stats.FailedTests.Add(ExtractTestName(line));
            }
            else if (trimmedLine.StartsWith("Skipped "))
            {
                stats.Skipped++;
            }
        }

        return stats;
    }

    private static bool ShouldFilterLine(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return true;

        var trimmedLine = line.TrimStart();

        // Filter xUnit adapter noise
        if (trimmedLine.StartsWith("[xUnit.net")) return true;

        // Filter VSTest noise
        if (trimmedLine.StartsWith("Test run for ")) return true;
        if (trimmedLine.StartsWith("VSTest version ")) return true;
        if (trimmedLine.StartsWith("Microsoft (R) Test Execution")) return true;
        if (trimmedLine.StartsWith("Copyright (c) Microsoft")) return true;
        if (trimmedLine.StartsWith("Starting test execution")) return true;
        if (trimmedLine.StartsWith("A total of ")) return true;

        // Filter per-assembly summary lines (we generate our own)
        if (trimmedLine.StartsWith("Test Run Successful.")) return true;
        if (trimmedLine.StartsWith("Test Run Failed.")) return true;
        if (Regex.IsMatch(trimmedLine, "^Total tests:")) return true;
        if (Regex.IsMatch(trimmedLine, @"^\s*Passed\s*:")) return true;
        if (Regex.IsMatch(trimmedLine, @"^\s*Failed\s*:")) return true;
        if (Regex.IsMatch(trimmedLine, @"^\s*Skipped\s*:")) return true;
        if (Regex.IsMatch(trimmedLine, "^Total time:")) return true;

        return false;
    }

    private static string ExtractTestName(string line)
    {
        // Line format: "  Failed TestNamespace.TestClass.TestMethod [duration]"
        var trimmed = line.Trim();
        if (trimmed.StartsWith("Failed "))
        {
            var testPart = trimmed[7..]; // Remove "Failed "
            var bracketIndex = testPart.LastIndexOf('[');
            if (bracketIndex > 0)
            {
                return testPart[..(bracketIndex - 1)].Trim();
            }

            return testPart.Trim();
        }

        return trimmed;
    }

    private static string BuildFilterArgument(string? userFilter, string? excludeCategory)
    {
        // By default, exclude "Noisy" category tests unless user explicitly specifies otherwise
        // Use empty string to disable default exclusion
        var categoryToExclude = excludeCategory ?? "Noisy";
        var categoryFilter = string.IsNullOrEmpty(categoryToExclude) ? "" : $"Category!={categoryToExclude}";

        if (userFilter is not null && categoryFilter != "")
        {
            // Combine user filter with category exclusion using AND (&)
            return $""" --filter "({userFilter})&{categoryFilter}" """;
        }

        if (userFilter is not null)
        {
            return $""" --filter "{userFilter}" """;
        }

        if (categoryFilter != "")
        {
            return $""" --filter "{categoryFilter}" """;
        }

        return "";
    }

    private class TestStats
    {
        public int Passed { get; set; }

        public int Failed { get; set; }

        public int Skipped { get; set; }

        public int Total => Passed + Failed + Skipped;

        public List<string> FailedTests { get; } = [];
    }
}
