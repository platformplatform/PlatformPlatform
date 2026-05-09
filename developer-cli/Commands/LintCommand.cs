using System.CommandLine;
using System.Diagnostics;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Karambolo.PO;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class LintCommand : Command
{
    public LintCommand() : base("lint", "Run code linting for frontend and backend code")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Run backend linting" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Run frontend linting" };
        var cliOption = new Option<bool>("--cli", "-c") { Description = "Run developer-cli linting" };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to lint (e.g., main, account, back-office)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring the solution before running linting" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(cliOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(noBuildOption);
        Options.Add(quietOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(cliOption),
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(noBuildOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private static void Execute(bool backend, bool frontend, bool developerCli, string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var noFlags = !backend && !frontend && !developerCli;
        var lintBackend = backend || noFlags;
        var lintFrontend = frontend || noFlags;
        var lintDeveloperCli = developerCli || noFlags;

        try
        {
            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;
            var developerCliTime = TimeSpan.Zero;
            var hasIssues = false;

            if (lintBackend)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                hasIssues = RunBackendLinting(selfContainedSystem, noBuild, quiet);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (lintFrontend)
            {
                Prerequisite.Ensure(Prerequisite.Node);
                var frontendHasIssues = RunFrontendLinting(quiet);
                hasIssues = hasIssues || frontendHasIssues;
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            if (lintDeveloperCli)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                var developerCliHasIssues = RunDeveloperCliLinting(noBuild, quiet);
                hasIssues = hasIssues || developerCliHasIssues;
                developerCliTime = Stopwatch.GetElapsedTime(startTime) - backendTime - frontendTime;
            }

            if (quiet)
            {
                if (hasIssues)
                {
                    Console.WriteLine("Issues found. Check result.json in the project directories.");
                    Environment.Exit(1);
                }

                Console.WriteLine("Linting completed successfully. No issues found.");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]Code linting completed in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");

                var multipleTargets = (lintBackend ? 1 : 0) + (lintFrontend ? 1 : 0) + (lintDeveloperCli ? 1 : 0) > 1;
                if (multipleTargets)
                {
                    var timingLines = new List<string>();
                    if (lintBackend) timingLines.Add($"Backend:       [green]{backendTime.Format()}[/]");
                    if (lintFrontend) timingLines.Add($"Frontend:      [green]{frontendTime.Format()}[/]");
                    if (lintDeveloperCli) timingLines.Add($"Developer CLI: [green]{developerCliTime.Format()}[/]");
                    AnsiConsole.MarkupLine(string.Join(Environment.NewLine, timingLines));
                }

                if (hasIssues)
                {
                    Environment.Exit(1);
                }
            }
        }
        catch (Exception ex)
        {
            if (quiet)
            {
                Console.WriteLine($"Linting failed: {ex.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error during code linting: {ex.Message}[/]");
            }

            Environment.Exit(1);
        }
    }

    private static bool RunBackendLinting(string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

        if (!noBuild)
        {
            if (!quiet) AnsiConsole.MarkupLine("[blue]Running backend code linting...[/]");
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
            ProcessHelper.Run($"dotnet build {solutionFile.Name}", solutionFile.Directory!.FullName, "Build", quiet);
        }

        // Delete existing result.json to prevent reading stale results
        var resultJsonPath = Path.Combine(solutionFile.Directory!.FullName, "result.json");
        if (File.Exists(resultJsonPath))
        {
            File.Delete(resultJsonPath);
        }

        // Exclude rendered email artifacts from inspection. React Email emits Outlook-required table
        // attributes (align, border, cellPadding, cellSpacing) that JetBrains flags as obsolete HTML5,
        // but those attributes are mandatory for cross-client email rendering and cannot be removed.
        // The dist folder is gitignored build output, not source.
        ProcessHelper.Run(
            $"dotnet jb inspectcode {solutionFile.Name} --no-build --no-restore --output=result.json --severity=SUGGESTION --exclude=**/emails/dist/**",
            solutionFile.Directory!.FullName,
            "Linting",
            quiet
        );

        var resultJson = File.ReadAllText(Path.Combine(solutionFile.Directory!.FullName, "result.json"));
        var hasIssues = !resultJson.Contains("\"results\": [],");

        if (!quiet)
        {
            if (hasIssues)
            {
                AnsiConsole.MarkupLine("[yellow]Backend issues found. Opening result.json...[/]");
                ProcessHelper.StartProcess("code result.json", solutionFile.Directory!.FullName);
            }
            else
            {
                AnsiConsole.MarkupLine("[green]No backend issues found![/]");
            }
        }

        return hasIssues;
    }

    private static bool RunFrontendLinting(bool quiet)
    {
        if (!quiet) AnsiConsole.MarkupLine("[blue]Running frontend linting...[/]");
        ProcessHelper.Run("npm run lint", Configuration.ApplicationFolder, "Frontend linting", quiet);

        return CheckMissingTranslations(quiet);
    }

    private static bool CheckMissingTranslations(bool quiet)
    {
        if (!quiet) AnsiConsole.MarkupLine("[blue]Checking for missing translations...[/]");

        var translationFiles = Directory.GetFiles(Configuration.ApplicationFolder, "*.po", SearchOption.AllDirectories)
            .Where(f => !f.Contains("node_modules") && !f.EndsWith("en-US.po"))
            .ToArray();

        var filesWithMissing = new List<(string RelativePath, int MissingCount)>();
        foreach (var translationFile in translationFiles)
        {
            var content = File.ReadAllText(translationFile);
            var parseResult = new POParser().Parse(new StringReader(content));
            if (!parseResult.Success) continue;

            var missingCount = parseResult.Catalog.Values
                .OfType<POSingularEntry>()
                .Count(entry => string.IsNullOrWhiteSpace(entry.Translation));

            if (missingCount > 0)
            {
                var relativePath = translationFile.Replace(Configuration.ApplicationFolder, "").TrimStart(Path.DirectorySeparatorChar);
                filesWithMissing.Add((relativePath, missingCount));
            }
        }

        if (filesWithMissing.Count == 0)
        {
            if (!quiet) AnsiConsole.MarkupLine("[green]No missing translations![/]");
            return false;
        }

        // Translation issues do not land in result.json, so always print which files are affected
        // even in quiet mode. Otherwise the user only sees the generic "Issues found" message.
        if (quiet)
        {
            Console.WriteLine($"Missing translations found in {filesWithMissing.Count} file(s):");
            foreach (var (relativePath, missingCount) in filesWithMissing)
            {
                Console.WriteLine($"  {missingCount} missing in {relativePath}");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]Missing translations found in {filesWithMissing.Count} file(s):[/]");
            foreach (var (relativePath, missingCount) in filesWithMissing)
            {
                AnsiConsole.MarkupLine($"  [red]{missingCount}[/] missing in [cyan]{relativePath}[/]");
            }
        }

        return true;
    }

    private static bool RunDeveloperCliLinting(bool noBuild, bool quiet)
    {
        var solutionFile = new FileInfo(Path.Combine(Configuration.CliFolder, "DeveloperCli.slnx"));

        if (!noBuild)
        {
            if (!quiet) AnsiConsole.MarkupLine("[blue]Running developer-cli code linting...[/]");
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
            ProcessHelper.Run($"dotnet build {solutionFile.Name}", solutionFile.Directory!.FullName, "Build", quiet);
        }

        // Delete existing result.json to prevent reading stale results
        var resultJsonPath = Path.Combine(solutionFile.Directory!.FullName, "result.json");
        if (File.Exists(resultJsonPath))
        {
            File.Delete(resultJsonPath);
        }

        ProcessHelper.Run(
            $"dotnet jb inspectcode {solutionFile.Name} --no-build --no-restore --output=result.json --severity=SUGGESTION",
            solutionFile.Directory!.FullName,
            "Linting",
            quiet
        );

        var resultJson = File.ReadAllText(Path.Combine(solutionFile.Directory!.FullName, "result.json"));
        var hasIssues = !resultJson.Contains("\"results\": [],");

        if (!quiet)
        {
            if (hasIssues)
            {
                AnsiConsole.MarkupLine("[yellow]Developer-cli issues found. Opening result.json...[/]");
                ProcessHelper.StartProcess("code result.json", solutionFile.Directory!.FullName);
            }
            else
            {
                AnsiConsole.MarkupLine("[green]No developer-cli issues found![/]");
            }
        }

        return hasIssues;
    }
}
