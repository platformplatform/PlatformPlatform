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

        var end2EndProjectPath = Path.Combine(Configuration.ApplicationFolder, "End2EndTests");

        // Clean up report directory if we're going to show it
        if (showReport)
        {
            var reportPath = Path.Combine(end2EndProjectPath, "playwright-report");
            if (Directory.Exists(reportPath))
            {
                AnsiConsole.MarkupLine("[blue]Cleaning up previous test report...[/]");
                Directory.Delete(reportPath, true);
            }
        }

        // Validate browser option
        if (!ValidBrowsers.Contains(browser.ToLower()))
        {
            AnsiConsole.MarkupLine($"[red]Invalid browser '{browser}'. Valid options are: {string.Join(", ", ValidBrowsers)}[/]");
            Environment.Exit(1);
        }

        // Validate self-contained system if provided
        if (selfContainedSystem is not null)
        {
            var availableSystems = GetAvailableSelfContainedSystems();
            if (!availableSystems.Contains(selfContainedSystem))
            {
                AnsiConsole.MarkupLine($"[red]Invalid self-contained system '{selfContainedSystem}'. Available systems: {string.Join(", ", availableSystems)}[/]");
                Environment.Exit(1);
            }
        }

        AnsiConsole.MarkupLine("[blue]Checking server availability...[/]");
        CheckWebsiteAccessibility();

        var stopwatch = Stopwatch.StartNew();

        AnsiConsole.MarkupLine("[blue]Starting Playwright tests...[/]");

        var showBrowser = headed || debug || slowMotion;
        var runSequential = showBrowser;
        var isLocalhost = BaseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        var playwrightArgs = BuildPlaywrightArgs(
            testPatterns, browser, debug, grep, showBrowser, includeSlow, lastFailed, onlyChanged, quiet, repeatEach,
            retries, runSequential, selfContainedSystem, smoke, stopOnFirstFailure, ui
        );

        var processStartInfo = new ProcessStartInfo
        {
            FileName = Configuration.IsWindows ? "cmd.exe" : "npx",
            Arguments = $"{(Configuration.IsWindows ? "/C npx" : string.Empty)} playwright test {playwrightArgs}",
            WorkingDirectory = end2EndProjectPath,
            UseShellExecute = false
        };

        AnsiConsole.MarkupLine($"[cyan]Running command: npx playwright test {playwrightArgs}[/]");

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
        }
        catch (Exception)
        {
            testsFailed = true;
        }

        stopwatch.Stop();

        AnsiConsole.MarkupLine(testsFailed
            ? $"[red]Tests failed in {stopwatch.Elapsed.TotalSeconds:F1} seconds[/]"
            : $"[green]Tests completed in {stopwatch.Elapsed.TotalSeconds:F1} seconds[/]"
        );

        if (quiet) return;

        if (showReport || testsFailed)
        {
            OpenHtmlReport(end2EndProjectPath);
        }

        if (testsFailed) Environment.Exit(1);
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

    // Process grep pattern to make it compatible with Playwright's expectations
    private static string ProcessGrepPattern(string grep)
    {
        // Check if the grep pattern contains our special marker and restore the @ symbol
        if (grep.StartsWith(CommandLineArgumentsPreprocessor.EscapedAtSymbolMarker))
        {
            return "@" + grep.Substring(CommandLineArgumentsPreprocessor.EscapedAtSymbolMarker.Length);
        }

        // If the pattern is a tag (starts with @), return it as-is without any additional processing
        // This ensures tags like @slow, @smoke, @comprehensive are passed directly to Playwright
        if (grep.StartsWith("@"))
        {
            return grep;
        }

        // For non-tag patterns, use a simpler approach that doesn't rely on complex escaping
        // Just double-quote the pattern to handle spaces and special characters
        return $"\"{grep}\"";
    }

    private static string[] GetAvailableSelfContainedSystems()
    {
        var testsPath = Path.Combine(Configuration.ApplicationFolder, "End2EndTests", "tests");
        return Directory.GetDirectories(testsPath).Select(Path.GetFileName).ToArray()!;
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
        string? selfContainedSystem,
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

        // Handle test patterns
        if (testPatterns.Length > 0)
        {
            args.AddRange(NormalizeTestPatterns(testPatterns, selfContainedSystem));
        }
        else if (selfContainedSystem is not null)
        {
            // If no test patterns but self-contained system is specified, use its tests directory
            args.Add(Path.Combine("tests", selfContainedSystem));
        }
        else
        {
            // If no patterns and no self-contained system, use the tests directory
            args.Add("tests");
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

    private static string[] NormalizeTestPatterns(string[] testPatterns, string? selfContainedSystem)
    {
        var end2EndProjectPath = Path.Combine(Configuration.ApplicationFolder, "End2EndTests");

        if (testPatterns.Length == 0)
        {
            // If no test patterns provided but self-contained system is specified, use its tests directory
            if (selfContainedSystem is not null)
            {
                return [Path.Combine("tests", selfContainedSystem)];
            }

            return [];
        }

        // Process each test pattern
        return testPatterns.Select(pattern =>
            {
                // If pattern already starts with 'tests/', return as is
                if (pattern.StartsWith("tests/", StringComparison.OrdinalIgnoreCase))
                {
                    return pattern;
                }

                // If self-contained system is specified, prepend its tests directory
                if (selfContainedSystem is not null)
                {
                    return Path.Combine("tests", selfContainedSystem, pattern);
                }

                // Try to infer the self-contained system from the pattern
                var availableSystems = GetAvailableSelfContainedSystems();
                foreach (var system in availableSystems)
                {
                    var testPath = Path.Combine("tests", system, pattern);
                    if (File.Exists(Path.Combine(end2EndProjectPath, testPath)))
                    {
                        return testPath;
                    }
                }

                // If no match found in any self-contained system, just prepend 'tests/'
                return Path.Combine("tests", pattern);
            }
        ).ToArray();
    }

    private static void OpenHtmlReport(string end2EndProjectPath)
    {
        var reportPath = Path.Combine(end2EndProjectPath, "playwright-report", "index.html");
        if (File.Exists(reportPath))
        {
            AnsiConsole.MarkupLine("[green]Opening test report...[/]");
            ProcessHelper.OpenBrowser(reportPath);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]No test report found at playwright-report/index.html[/]");
        }
    }
}
