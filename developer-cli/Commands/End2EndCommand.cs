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

    public End2EndCommand() : base("e2e", "Run end-to-end tests using Playwright")
    {
        // Add argument for test file patterns
        AddArgument(new Argument<string[]>("test-patterns", () => [], "Test file patterns to run (e.g., my.spec.ts)"));

        // All options in alphabetical order
        AddOption(new Option<string>(["--browser", "-b"], () => "all", "Browser to use for tests (chromium, firefox, webkit, safari, all)"));
        AddOption(new Option<bool>(["--debug"], () => false, "Start with Playwright Inspector for debugging (automatically enables headed mode)"));
        AddOption(new Option<string?>(["--grep", "-g"], "Filter tests by title pattern"));
        AddOption(new Option<bool>(["--headed"], () => false, "Show browser UI while running tests (automatically enables sequential execution)"));
        AddOption(new Option<bool>(["--include-slow"], () => false, "Include tests marked as @slow"));
        AddOption(new Option<bool>(["--last-failed"], () => false, "Only re-run the failures"));
        AddOption(new Option<bool>(["--only-changed"], () => false, "Only run test files that have uncommitted changes"));
        AddOption(new Option<bool>(["--quiet"], () => false, "Suppress all output including terminal output and automatic report opening"));
        AddOption(new Option<int?>(["--repeat-each"], "Number of times to repeat each test"));
        AddOption(new Option<int?>(["--retries"], "Maximum retry count for flaky tests, zero for no retries"));
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], "The name of the self-contained system to test (account-management, back-office, etc.)"));
        AddOption(new Option<bool>(["--show-report"], () => false, "Always show HTML report after test run"));
        AddOption(new Option<bool>(["--slow-motion"], () => false, "Run tests in slow motion (automatically enables headed mode)"));
        AddOption(new Option<bool>(["--smoke"], () => false, "Run only smoke tests"));
        AddOption(new Option<bool>(["--stop-on-first-failure", "-x"], () => false, "Stop after the first failure"));
        AddOption(new Option<bool>(["--ui"], () => false, "Run tests in interactive UI mode with time-travel debugging"));

        Handler = CommandHandler.Create(Execute);
    }

    private static string BaseUrl => Environment.GetEnvironmentVariable("PUBLIC_URL") ?? "https://localhost:9000";

    private static void Execute(
        string[] testPatterns,
        string browser,
        bool debug,
        string? grep,
        bool headed,
        bool includeSlow,
        bool lastFailed,
        bool onlyChanged,
        bool quiet,
        int? repeatEach,
        int? retries,
        string? selfContainedSystem,
        bool showReport,
        bool slowMotion,
        bool smoke,
        bool stopOnFirstFailure,
        bool ui)
    {
        Prerequisite.Ensure(Prerequisite.Node);

        // Get available self-contained systems
        var availableSystems = GetAvailableSelfContainedSystems();

        if (availableSystems.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No self-contained systems with e2e tests found.[/]");
            Environment.Exit(1);
        }

        // If no specific SCS is provided, run tests for all available systems
        var systemsToTest = selfContainedSystem != null ? [selfContainedSystem] : availableSystems;

        // Validate self-contained system if provided
        if (selfContainedSystem is not null && !availableSystems.Contains(selfContainedSystem))
        {
            AnsiConsole.MarkupLine($"[red]Invalid self-contained system '{selfContainedSystem}'. Available systems: {string.Join(", ", availableSystems)}[/]");
            Environment.Exit(1);
        }

        // Validate browser option
        if (!ValidBrowsers.Contains(browser.ToLower()))
        {
            AnsiConsole.MarkupLine($"[red]Invalid browser '{browser}'. Valid options are: {string.Join(", ", ValidBrowsers)}[/]");
            Environment.Exit(1);
        }

        AnsiConsole.MarkupLine("[blue]Checking server availability...[/]");
        CheckWebsiteAccessibility();

        var stopwatch = Stopwatch.StartNew();
        var overallSuccess = true;

        foreach (var system in systemsToTest)
        {
            var systemSuccess = RunTestsForSystem(system, testPatterns, browser, debug, grep, headed, includeSlow,
                lastFailed, onlyChanged, quiet, repeatEach, retries, showReport, slowMotion, smoke, stopOnFirstFailure, ui
            );

            if (!systemSuccess)
            {
                overallSuccess = false;
            }
        }

        stopwatch.Stop();

        AnsiConsole.MarkupLine(overallSuccess
            ? $"[green]All tests completed in {stopwatch.Elapsed.TotalSeconds:F1} seconds[/]"
            : $"[red]Some tests failed in {stopwatch.Elapsed.TotalSeconds:F1} seconds[/]"
        );

        if (!overallSuccess) Environment.Exit(1);
    }

    private static bool RunTestsForSystem(
        string system,
        string[] testPatterns,
        string browser,
        bool debug,
        string? grep,
        bool headed,
        bool includeSlow,
        bool lastFailed,
        bool onlyChanged,
        bool quiet,
        int? repeatEach,
        int? retries,
        bool showReport,
        bool slowMotion,
        bool smoke,
        bool stopOnFirstFailure,
        bool ui)
    {
        var systemPath = Path.Combine(Configuration.ApplicationFolder, system, "WebApp");
        var e2eTestsPath = Path.Combine(systemPath, "tests/e2e");

        if (!Directory.Exists(e2eTestsPath))
        {
            AnsiConsole.MarkupLine($"[yellow]No e2e tests found for {system}, skipping...[/]");
            return true;
        }

        AnsiConsole.MarkupLine($"[blue]Running tests for {system}...[/]");

        // Clean up report directory if we're going to show it
        if (showReport)
        {
            var reportPath = Path.Combine(e2eTestsPath, "playwright-report");
            if (Directory.Exists(reportPath))
            {
                AnsiConsole.MarkupLine("[blue]Cleaning up previous test report...[/]");
                Directory.Delete(reportPath, true);
            }
        }

        var showBrowser = headed || debug || slowMotion;
        var runSequential = showBrowser;
        var isLocalhost = BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        var playwrightArgs = BuildPlaywrightArgs(
            testPatterns, browser, debug, grep, showBrowser, includeSlow, lastFailed, onlyChanged, quiet, repeatEach,
            retries, runSequential, smoke, stopOnFirstFailure, ui
        );

        var processStartInfo = new ProcessStartInfo
        {
            FileName = Configuration.IsWindows ? "cmd.exe" : "npx",
            Arguments = $"{(Configuration.IsWindows ? "/C npx" : string.Empty)} playwright test --config=./tests/playwright.config.ts {playwrightArgs}",
            WorkingDirectory = systemPath,
            UseShellExecute = false
        };

        AnsiConsole.MarkupLine($"[cyan]Running command in {system}: npx playwright test --config=./tests/playwright.config.ts {playwrightArgs}[/]");

        processStartInfo.EnvironmentVariables["PUBLIC_URL"] = BaseUrl;

        if (slowMotion) processStartInfo.EnvironmentVariables["PLAYWRIGHT_SLOW_MO"] = "500";
        if (includeSlow) processStartInfo.EnvironmentVariables["PLAYWRIGHT_TIMEOUT"] = "400000";
        if (isLocalhost) processStartInfo.EnvironmentVariables["PLAYWRIGHT_VIDEO_MODE"] = "on";

        // Prevent HTML report from opening automatically
        processStartInfo.EnvironmentVariables["PLAYWRIGHT_HTML_OPEN"] = "never";

        var testsFailed = false;
        try
        {
            ProcessHelper.StartProcess(processStartInfo, throwOnError: true);
            AnsiConsole.MarkupLine($"[green]Tests for {system} completed successfully[/]");
        }
        catch (Exception)
        {
            testsFailed = true;
            AnsiConsole.MarkupLine($"[red]Tests for {system} failed[/]");
        }

        if (!quiet && (showReport || testsFailed))
        {
            OpenHtmlReport(e2eTestsPath, system);
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

    private static string[] GetAvailableSelfContainedSystems()
    {
        var applicationPath = Configuration.ApplicationFolder;
        var systems = new List<string>();

        // Look for directories that contain WebApp/tests/e2e
        foreach (var directory in Directory.GetDirectories(applicationPath))
        {
            var dirName = Path.GetFileName(directory);

            // Skip known non-SCS directories
            if (dirName is "AppHost" or "AppGateway" or "shared-kernel" or "shared-webapp")
            {
                continue;
            }

            var e2eTestsPath = Path.Combine(directory, "WebApp", "tests/e2e");
            if (Directory.Exists(e2eTestsPath))
            {
                systems.Add(dirName);
            }
        }

        return systems.ToArray();
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
        bool quiet,
        int? repeatEach,
        int? retries,
        bool runSequential,
        bool smoke,
        bool stopOnFirstFailure,
        bool ui)
    {
        var args = new List<string>();

        // Handle browser project first as it affects test selection
        if (!browser.Equals("all", StringComparison.CurrentCultureIgnoreCase))
        {
            var playwrightBrowser = browser.ToLower() == "safari" ? "webkit" : browser.ToLower();
            args.Add($"--project={playwrightBrowser}");
        }

        // Let playwright.config.ts handle reporters
        if (quiet) args.Add("--reporter=list");

        // Handle test patterns - they should be relative to the tests/e2e directory
        if (testPatterns.Length > 0)
        {
            args.AddRange(testPatterns.Select(pattern =>
                    pattern.StartsWith("./") || pattern.StartsWith("tests/e2e/") ? pattern : $"./tests/e2e/{pattern}"
                )
            );
        }

        // Handle test filtering
        if (grep != null) args.Add($"--grep=\"{grep}\"");
        if (smoke) args.Add("--grep=\"@smoke\"");
        if (!includeSlow) args.Add("--grep-invert=\"@slow\"");

        // Handle test execution options
        if (ui) args.Add("--ui");
        if (debug) args.Add("--debug");
        if (showBrowser) args.Add("--headed");
        if (lastFailed) args.Add("--last-failed");
        if (onlyChanged) args.Add("--only-changed");
        if (quiet) args.Add("--quiet");
        if (repeatEach.HasValue) args.Add($"--repeat-each={repeatEach.Value}");
        if (retries.HasValue) args.Add($"--retries={retries.Value}");
        if (runSequential) args.Add("--workers=1");
        if (stopOnFirstFailure) args.Add("-x");

        return string.Join(" ", args);
    }

    private static void OpenHtmlReport(string e2eTestsPath, string system)
    {
        var reportPath = Path.Combine(e2eTestsPath, "playwright-report", "index.html");
        if (File.Exists(reportPath))
        {
            AnsiConsole.MarkupLine($"[green]Opening test report for {system}...[/]");
            ProcessHelper.OpenBrowser(reportPath);
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]No test report found for {system} at playwright-report/index.html[/]");
        }
    }
}
