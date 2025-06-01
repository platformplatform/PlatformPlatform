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
        AddOption(new Option<string?>(["<self-contained-system>", "--self-contained-system", "-s"], $"The name of the self-contained system to test ({string.Join(", ", AvailableSelfContainedSystems)}, etc.)"));
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

        // If debug or UI mode is enabled, we need a specific self-contained system
        if ((debug || ui) && selfContainedSystem is null)
        {
            selfContainedSystem = SelfContainedSystemHelper.PromptForSelfContainedSystem(AvailableSelfContainedSystems);
        }

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
            selfContainedSystemsToTest = DetermineSystemsToTest(testPatterns, grep, AvailableSelfContainedSystems);
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
        var failedSelfContainedSystems = new List<string>();

        foreach (var currentSelfContainedSystem in selfContainedSystemsToTest)
        {
            var selfContainedSystemSuccess = RunTestsForSystem(currentSelfContainedSystem, testPatterns, browser, debug, grep, headed, includeSlow, lastFailed,
                onlyChanged, quiet, repeatEach, retries, showReport, slowMotion, smoke, stopOnFirstFailure, ui
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

    private static bool RunTestsForSystem(
        string selfContainedSystem,
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
        var systemPath = Path.Combine(Configuration.ApplicationFolder, selfContainedSystem, "WebApp");
        var e2eTestsPath = Path.Combine(systemPath, "e2e-tests");

        if (!Directory.Exists(e2eTestsPath))
        {
            AnsiConsole.MarkupLine($"[yellow]No e2e tests found for {selfContainedSystem}. Skipping...[/]");
            return true;
        }

        AnsiConsole.MarkupLine($"[blue]Running tests for {selfContainedSystem}...[/]");

        // Clean up report directory if we're going to show it
        if (showReport)
        {
            var reportDirectory = Path.Combine(e2eTestsPath, "playwright-report");
            if (Directory.Exists(reportDirectory))
            {
                AnsiConsole.MarkupLine("[blue]Cleaning up previous test report...[/]");
                Directory.Delete(reportDirectory, true);
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
            Arguments = $"{(Configuration.IsWindows ? "/C npx" : string.Empty)} playwright test --config=./e2e-tests/playwright.config.ts {playwrightArgs}",
            WorkingDirectory = systemPath,
            UseShellExecute = false
        };

        AnsiConsole.MarkupLine($"[cyan]Running command in {selfContainedSystem}: npx playwright test --config=./e2e-tests/playwright.config.ts {playwrightArgs}[/]");

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

        foreach (var pattern in testPatterns.Where(p => p != null && p != "*"))
        {
            var normalizedPattern = pattern.EndsWith(".spec.ts") ? pattern : $"{pattern}.spec.ts";
            normalizedPattern = Path.GetFileName(normalizedPattern);

            foreach (var system in availableSystems)
            {
                var e2eTestsPath = Path.Combine(Configuration.ApplicationFolder, system, "WebApp", "e2e-tests");
                if (!Directory.Exists(e2eTestsPath)) continue;

                var testFiles = Directory.GetFiles(e2eTestsPath, "*.spec.ts", SearchOption.AllDirectories)
                    .Select(Path.GetFileName);

                if (testFiles.Any(file => file?.Equals(normalizedPattern, StringComparison.OrdinalIgnoreCase) == true))
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
                var e2eTestsPath = Path.Combine(Configuration.ApplicationFolder, system, "WebApp", "e2e-tests");
                if (!Directory.Exists(e2eTestsPath)) continue;

                var testFiles = Directory.GetFiles(e2eTestsPath, "*.spec.ts", SearchOption.AllDirectories);
                foreach (var testFile in testFiles)
                {
                    if (File.ReadAllText(testFile).Contains(grep, StringComparison.OrdinalIgnoreCase))
                    {
                        matchingSystems.Add(system);
                        break;
                    }
                }
            }

            if (matchingSystems.Count > 0) return matchingSystems.ToArray();
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

        // Handle test patterns - they should be relative to the e2e-tests directory
        if (testPatterns.Length > 0)
        {
            args.AddRange(testPatterns.Select(pattern =>
                    pattern.StartsWith("./") || pattern.StartsWith("e2e-tests/") ? pattern : $"./e2e-tests/{pattern}"
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

    private static string PromptForSelfContainedSystem(string[] availableSystems)
    {
        if (availableSystems.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]No self-contained systems found.[/]");
            Environment.Exit(1);
            return string.Empty; // This line will never be reached but is needed to satisfy the compiler
        }

        var selectedSystem = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a [green]self-contained system[/] to test:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more systems)[/]")
                .AddChoices(availableSystems)
        );

        return selectedSystem;
    }

    private static void OpenHtmlReport(string selfContainedSystem)
    {
        var reportPath = Path.Combine(Configuration.ApplicationFolder, selfContainedSystem, "WebApp", "playwright-report", "index.html");

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
}
