using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class End2EndCommand : Command
{
    private static readonly string[] ValidBrowsers = ["chromium", "firefox", "webkit", "safari", "all"];

    // Get available self-contained systems
    private static readonly string[] AvailableSelfContainedSystems = SelfContainedSystemHelper.GetAvailableSelfContainedSystems();

    public End2EndCommand() : base("e2e", "Run end-to-end tests using Playwright")
    {
        // Add argument for search term or test file patterns
        AddArgument(new Argument<string[]>("search-terms", () => [], "Search terms for test filtering (e.g., 'user management', '@smoke', 'smoke', 'comprehensive', 'user-management-flows.spec.ts')"));

        // All options in alphabetical order
        AddOption(new Option<string>(["--browser", "-b"], () => "all", "Browser to use for tests (chromium, firefox, webkit, safari, all)"));
        AddOption(new Option<bool>(["--debug"], () => false, "Start with Playwright Inspector for debugging (automatically enables headed mode)"));
        AddOption(new Option<bool>(["--debug-timing"], () => false, "Show step timing output with color coding during test execution"));
        AddOption(new Option<bool>(["--headed"], () => false, "Show browser UI while running tests (automatically enables sequential execution)"));
        AddOption(new Option<bool>(["--include-slow"], () => false, "Include tests marked as @slow"));
        AddOption(new Option<bool>(["--last-failed"], () => false, "Only re-run the failures"));
        AddOption(new Option<bool>(["--only-changed"], () => false, "Only run test files that have uncommitted changes"));
        AddOption(new Option<bool>(["--quiet"], () => false, "Suppress all output including terminal output and automatic report opening"));
        AddOption(new Option<int?>(["--repeat-each"], "Number of times to repeat each test"));
        AddOption(new Option<bool>(["--delete-artifacts"], () => false, "Delete all test artifacts and exit"));
        AddOption(new Option<int?>(["--retries"], "Maximum retry count for flaky tests, zero for no retries"));
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], $"The name of the self-contained system to test ({string.Join(", ", AvailableSelfContainedSystems)}, etc.)"));
        AddOption(new Option<bool>(["--show-report"], () => false, "Always show HTML report after test run"));
        AddOption(new Option<bool>(["--slow-mo"], () => false, "Run tests in slow motion (automatically enables headed mode)"));
        AddOption(new Option<bool>(["--smoke"], () => false, "Run only smoke tests"));
        AddOption(new Option<bool>(["--stop-on-first-failure", "-x"], () => false, "Stop after the first failure"));
        AddOption(new Option<bool>(["--ui"], () => false, "Run tests in interactive UI mode with time-travel debugging"));
        AddOption(new Option<int?>(["--workers", "-w"], "Number of worker processes to use for running tests"));

        Handler = CommandHandler.Create(Execute);
    }

    private static string BaseUrl => Environment.GetEnvironmentVariable("PUBLIC_URL") ?? "https://localhost:9000";

    private static void Execute(
        string[] searchTerms,
        string browser,
        bool debug,
        bool debugTiming,
        bool headed,
        bool includeSlow,
        bool lastFailed,
        bool onlyChanged,
        bool quiet,
        int? repeatEach,
        bool deleteArtifacts,
        int? retries,
        string? selfContainedSystem,
        bool showReport,
        bool slowMo,
        bool smoke,
        bool stopOnFirstFailure,
        bool ui,
        int? workers)
    {
        Prerequisite.Ensure(Prerequisite.Node);

        if (deleteArtifacts)
        {
            DeleteAllTestArtifacts();
            AnsiConsole.MarkupLine("[yellow]Note: --delete-artifacts is a standalone operation and exits after cleaning artifacts.[/]");
            Environment.Exit(0);
        }

        AnsiConsole.MarkupLine("[blue]Checking server availability...[/]");
        CheckWebsiteAccessibility();

        PlaywrightInstaller.EnsurePlaywrightBrowsers();

        // Convert search terms to test patterns and grep patterns
        var (testPatterns, searchGrep) = ProcessSearchTerms(searchTerms);

        // Determine which self-contained systems to test based on the provided patterns or grep
        string[] selfContainedSystemsToTest;
        if (selfContainedSystem is not null)
        {
            if (!AvailableSelfContainedSystems.Contains(selfContainedSystem))
            {
                AnsiConsole.MarkupLine($"[red]Invalid self-contained system '{selfContainedSystem}'. Available systems: {string.Join(", ", AvailableSelfContainedSystems)}[/]");
                Environment.Exit(1);
            }

            selfContainedSystemsToTest = [selfContainedSystem];
        }
        else
        {
            selfContainedSystemsToTest = DetermineSystemsToTest(testPatterns, searchGrep, AvailableSelfContainedSystems);
        }

        // If debug or UI mode is enabled, we need a specific self-contained system
        if ((debug || ui) && selfContainedSystem is null)
        {
            if (selfContainedSystemsToTest.Length == 1)
            {
                selfContainedSystem = selfContainedSystemsToTest[0];
                selfContainedSystemsToTest = [selfContainedSystem];
            }
            else
            {
                selfContainedSystem = SelfContainedSystemHelper.PromptForSelfContainedSystem(
                    selfContainedSystemsToTest.Length > 0 ? selfContainedSystemsToTest : AvailableSelfContainedSystems
                );
                selfContainedSystemsToTest = [selfContainedSystem];
            }
        }

        // Validate browser option
        if (!ValidBrowsers.Contains(browser.ToLower()))
        {
            AnsiConsole.MarkupLine($"[red]Invalid browser '{browser}'. Valid options are: {string.Join(", ", ValidBrowsers)}[/]");
            Environment.Exit(1);
        }

        var stopwatch = Stopwatch.StartNew();
        var overallSuccess = true;
        var failedSelfContainedSystems = new List<string>();

        foreach (var currentSelfContainedSystem in selfContainedSystemsToTest)
        {
            var selfContainedSystemSuccess = RunTestsForSystem(currentSelfContainedSystem, testPatterns, browser, debug, debugTiming, searchGrep, headed, includeSlow, lastFailed,
                onlyChanged, repeatEach, retries, showReport, slowMo, smoke, stopOnFirstFailure, ui, workers
            );

            if (!selfContainedSystemSuccess)
            {
                overallSuccess = false;
                failedSelfContainedSystems.Add(currentSelfContainedSystem);

                // If stop on first failure is enabled, exit the loop after the first failure
                if (stopOnFirstFailure)
                {
                    break;
                }
            }
        }

        stopwatch.Stop();

        AnsiConsole.MarkupLine(overallSuccess
            ? $"[green]All tests completed in {stopwatch.Elapsed.TotalSeconds:F1} seconds[/]"
            : $"[red]Some tests failed in {stopwatch.Elapsed.TotalSeconds:F1} seconds[/]"
        );

        if (!quiet)
        {
            if (showReport)
            {
                foreach (var currentSelfContainedSystem in selfContainedSystemsToTest)
                {
                    OpenHtmlReport(currentSelfContainedSystem);
                }
            }
            else if (!overallSuccess)
            {
                foreach (var currentSelfContainedSystem in failedSelfContainedSystems)
                {
                    OpenHtmlReport(currentSelfContainedSystem);
                }
            }
        }

        if (!overallSuccess) Environment.Exit(1);
    }

    private static (string[] testPatterns, string? grep) ProcessSearchTerms(string[] searchTerms)
    {
        if (searchTerms.Length == 0)
        {
            return ([], null);
        }

        var testPatterns = new List<string>();
        var grepTerms = new List<string>();

        foreach (var term in searchTerms)
        {
            var processedTerm = term;
            var wasAtSymbol = false;

            // Handle escaped @tag syntax from CommandLineArgumentsPreprocessor
            if (term.StartsWith(CommandLineArgumentsPreprocessor.EscapedAtSymbolMarker))
            {
                processedTerm = term.Substring(CommandLineArgumentsPreprocessor.EscapedAtSymbolMarker.Length);
                wasAtSymbol = true;
            }

            // If the term ends with .spec.ts or looks like a file, treat it as a test pattern
            if (processedTerm.EndsWith(".spec.ts") || processedTerm.Contains('/') || processedTerm.Contains('\\'))
            {
                testPatterns.Add(processedTerm);
            }
            else
            {
                // For grep terms, preserve the @ if it was originally there
                var grepTerm = wasAtSymbol ? $"@{processedTerm}" : processedTerm;
                grepTerms.Add(grepTerm);
            }
        }

        // Combine search terms
        var finalGrep = grepTerms.Count > 0 ? string.Join(" ", grepTerms) : null;

        return (testPatterns.ToArray(), finalGrep);
    }

    private static bool RunTestsForSystem(
        string selfContainedSystem,
        string[] testPatterns,
        string browser,
        bool debug,
        bool debugTiming,
        string? searchGrep,
        bool headed,
        bool includeSlow,
        bool lastFailed,
        bool onlyChanged,
        int? repeatEach,
        int? retries,
        bool showReport,
        bool slowMo,
        bool smoke,
        bool stopOnFirstFailure,
        bool ui,
        int? workers)
    {
        var systemPath = Path.Combine(Configuration.ApplicationFolder, selfContainedSystem, "WebApp");
        var e2ETestsPath = Path.Combine(systemPath, "tests/e2e");

        if (!Directory.Exists(e2ETestsPath))
        {
            AnsiConsole.MarkupLine($"[yellow]No e2e tests found for {selfContainedSystem}. Skipping...[/]");
            return true;
        }

        AnsiConsole.MarkupLine($"[blue]Running tests for {selfContainedSystem}...[/]");

        // Clean up report directory if we're going to show it
        if (showReport)
        {
            var reportDirectory = Path.Combine(systemPath, "tests", "test-results", "playwright-report");
            if (Directory.Exists(reportDirectory))
            {
                AnsiConsole.MarkupLine("[blue]Cleaning up previous test report...[/]");
                Directory.Delete(reportDirectory, true);
            }
        }

        var showBrowser = headed || debug || slowMo;
        var runSequential = showBrowser || debugTiming;
        var isLocalhost = BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        var playwrightArgs = BuildPlaywrightArgs(
            testPatterns, browser, debug, searchGrep, showBrowser, includeSlow, lastFailed, onlyChanged, repeatEach,
            retries, runSequential, smoke, stopOnFirstFailure, ui, workers
        );

        var processStartInfo = new ProcessStartInfo
        {
            FileName = Configuration.IsWindows ? "cmd.exe" : "npx",
            Arguments = $"{(Configuration.IsWindows ? "/C npx" : string.Empty)} playwright test --config=./tests/playwright.config.ts {playwrightArgs}",
            WorkingDirectory = systemPath,
            UseShellExecute = false
        };

        AnsiConsole.MarkupLine($"[cyan]Running command in {selfContainedSystem}: npx playwright test --config=./tests/playwright.config.ts {playwrightArgs}[/]");

        processStartInfo.EnvironmentVariables["PUBLIC_URL"] = BaseUrl;

        if (slowMo) processStartInfo.EnvironmentVariables["PLAYWRIGHT_SLOW_MO"] = "500";
        if (isLocalhost) processStartInfo.EnvironmentVariables["PLAYWRIGHT_VIDEO_MODE"] = "on";
        if (debugTiming) processStartInfo.EnvironmentVariables["PLAYWRIGHT_SHOW_DEBUG_TIMING"] = "true";

        // Prevent HTML report from opening automatically
        processStartInfo.EnvironmentVariables["PLAYWRIGHT_HTML_OPEN"] = "never";

        var testsFailed = false;
        try
        {
            ProcessHelper.StartProcess(processStartInfo, throwOnError: true);
            AnsiConsole.MarkupLine(testsFailed
                ? $"[red]Tests for {selfContainedSystem} failed[/]"
                : $"[green]Tests for {selfContainedSystem} completed successfully[/]"
            );
        }
        catch (Exception)
        {
            testsFailed = true;
            AnsiConsole.MarkupLine($"[red]Tests for {selfContainedSystem} failed[/]");
        }

        return !testsFailed;
    }

    private static void CheckWebsiteAccessibility()
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(5);

            var response = httpClient.Send(new HttpRequestMessage(HttpMethod.Head, BaseUrl));

            if (response.IsSuccessStatusCode)
            {
                AnsiConsole.MarkupLine($"[green]Server is accessible at {BaseUrl}[/]");
                return;
            }
        }
        catch
        {
            // Fall through to error handling
        }

        AnsiConsole.MarkupLine($"[red]Server is not accessible at {BaseUrl}[/]");
        AnsiConsole.MarkupLine($"[yellow]Please start AppHost in your IDE before running '{Configuration.AliasName} e2e'[/]");
        Environment.Exit(1);
    }

    private static string[] DetermineSystemsToTest(string[] testPatterns, string? grep, string[] availableSystems)
    {
        if ((testPatterns.Length == 0 || testPatterns[0] == "*") && string.IsNullOrEmpty(grep))
        {
            return availableSystems;
        }

        var matchingSystems = new HashSet<string>();

        foreach (var pattern in testPatterns.Where(p => p != "*"))
        {
            var normalizedPattern = pattern.EndsWith(".spec.ts") ? pattern : $"{pattern}.spec.ts";
            normalizedPattern = Path.GetFileName(normalizedPattern);

            foreach (var system in availableSystems)
            {
                var e2ETestsPath = Path.Combine(Configuration.ApplicationFolder, system, "WebApp", "tests", "e2e");
                if (!Directory.Exists(e2ETestsPath)) continue;

                var testFiles = Directory.GetFiles(e2ETestsPath, "*.spec.ts", SearchOption.AllDirectories)
                    .Select(Path.GetFileName)
                    .Where(f => f is not null)
                    .Select(f => f!);

                if (testFiles.Any(file => file.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase)))
                {
                    matchingSystems.Add(system);
                }
            }
        }

        if (matchingSystems.Count > 0) return matchingSystems.ToArray();

        if (!string.IsNullOrEmpty(grep))
        {
            foreach (var system in availableSystems)
            {
                var e2ETestsPath = Path.Combine(Configuration.ApplicationFolder, system, "WebApp", "tests", "e2e");
                if (!Directory.Exists(e2ETestsPath)) continue;

                var testFiles = Directory.GetFiles(e2ETestsPath, "*.spec.ts", SearchOption.AllDirectories);
                foreach (var testFile in testFiles)
                {
                    // For filename search, remove @ if present for comparison
                    var filenameSearchTerm = grep.StartsWith("@") ? grep.Substring(1) : grep;
                    var fileName = Path.GetFileNameWithoutExtension(testFile);
                    if (fileName.Contains(filenameSearchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingSystems.Add(system);
                        break;
                    }

                    // For content search, use the grep term as-is (with @ if present)
                    if (File.ReadAllText(testFile).Contains(grep, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingSystems.Add(system);
                        break;
                    }
                }
            }

            return matchingSystems.ToArray();
        }

        return availableSystems;
    }

    private static string BuildPlaywrightArgs(
        string[] testPatterns,
        string browser,
        bool debug,
        string? grep,
        bool showBrowser,
        bool includeSlow,
        bool lastFailed,
        bool onlyChanged,
        int? repeatEach,
        int? retries,
        bool runSequential,
        bool smoke,
        bool stopOnFirstFailure,
        bool ui,
        int? workers)
    {
        var args = new List<string>();

        // Handle browser project first as it affects test selection
        if (!browser.Equals("all", StringComparison.CurrentCultureIgnoreCase))
        {
            var playwrightBrowser = browser.ToLower() == "safari" ? "webkit" : browser.ToLower();
            args.Add($"--project={playwrightBrowser}");
        }

        // Handle test patterns - they should be relative to the tests/e2e directory
        if (testPatterns.Length > 0)
        {
            args.AddRange(testPatterns.Select(pattern =>
                    pattern.StartsWith("./") || pattern.StartsWith("tests/e2e/") ? pattern : $"./tests/e2e/{pattern}"
                )
            );
        }

        // Handle test filtering
        if (grep != null)
        {
            args.Add($"--grep=\"{grep}\"");
        }

        if (smoke) args.Add("--grep=\"@smoke\"");
        if (!includeSlow) args.Add("--grep-invert=\"@slow\"");

        // Handle test execution options
        if (ui) args.Add("--ui");
        if (debug) args.Add("--debug");
        if (showBrowser) args.Add("--headed");
        if (lastFailed) args.Add("--last-failed");
        if (onlyChanged) args.Add("--only-changed");
        if (repeatEach.HasValue) args.Add($"--repeat-each={repeatEach.Value}");
        if (retries.HasValue) args.Add($"--retries={retries.Value}");
        if (workers.HasValue)
        {
            args.Add($"--workers={workers.Value}");
        }
        else if (runSequential) args.Add("--workers=1");

        if (stopOnFirstFailure) args.Add("-x");

        return string.Join(" ", args);
    }

    private static void OpenHtmlReport(string selfContainedSystem)
    {
        var reportPath = Path.Combine(Configuration.ApplicationFolder, selfContainedSystem, "WebApp", "tests", "test-results", "playwright-report", "index.html");

        if (File.Exists(reportPath))
        {
            AnsiConsole.MarkupLine($"[green]Opening test report for '{selfContainedSystem}'...[/]");
            ProcessHelper.OpenBrowser(reportPath);
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]No test report found for '{selfContainedSystem}' at '{reportPath}'[/]");
        }
    }

    private static void DeleteAllTestArtifacts()
    {
        AnsiConsole.MarkupLine("[blue]Deleting test artifacts...[/]");

        var totalDeleted = 0;

        foreach (var selfContainedSystemName in AvailableSelfContainedSystems)
        {
            var testResultsDirectory = Path.Combine(Configuration.ApplicationFolder, selfContainedSystemName, "WebApp", "tests", "test-results");

            if (!Directory.Exists(testResultsDirectory)) continue;

            Directory.Delete(testResultsDirectory, true);
            totalDeleted++;
            AnsiConsole.MarkupLine($"[green]Deleted test-results directory for {selfContainedSystemName}[/]");
        }

        AnsiConsole.MarkupLine(totalDeleted > 0
            ? $"[green]Successfully deleted test artifacts from {totalDeleted} system(s)[/]"
            : "[yellow]No test artifacts found to delete[/]"
        );
    }
}
