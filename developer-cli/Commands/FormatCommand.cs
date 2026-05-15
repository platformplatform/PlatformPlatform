using System.CommandLine;
using System.Diagnostics;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class FormatCommand : Command
{
    public FormatCommand() : base("format", "Formats code to match code styling rules")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Format backend code" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Format frontend code" };
        var cliOption = new Option<bool>("--cli", "-c") { Description = "Format developer-cli code" };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to format (e.g., main, account, back-office)" };
        var gatewayOption = new Option<bool>("--gateway", "-g") { Description = "Scope backend formatting to AppGateway and AppGateway.Tests" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring before formatting" };
        var allFilesOption = new Option<bool>("--all-files") { Description = "Format every file in the solution. Default is to format only .cs files changed against origin/main." };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Minimal output mode" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(cliOption);
        Options.Add(selfContainedSystemOption);
        Options.Add(gatewayOption);
        Options.Add(noBuildOption);
        Options.Add(allFilesOption);
        Options.Add(quietOption);

        SetAction(parseResult => Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(cliOption),
                parseResult.GetValue(selfContainedSystemOption),
                parseResult.GetValue(gatewayOption),
                parseResult.GetValue(noBuildOption),
                parseResult.GetValue(allFilesOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private static void Execute(bool backend, bool frontend, bool developerCli, string? selfContainedSystem, bool gateway, bool noBuild, bool allFiles, bool quiet)
    {
        if (gateway) AppGatewayHelper.EnsureNotCombinedWithSelfContainedSystem(selfContainedSystem);

        var noFlags = !backend && !frontend && !developerCli;
        var formatBackend = backend || noFlags;
        var formatFrontend = frontend || noFlags;
        var formatDeveloperCli = developerCli || noFlags;

        try
        {
            var initialUncommittedFiles = quiet ? null : GitHelper.GetChangedFiles();
            if (!quiet && initialUncommittedFiles!.Count > 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: You have unstaged changes in your working directory.[/]");
            }

            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;
            var developerCliTime = TimeSpan.Zero;

            if (formatBackend)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                RunBackendFormat(selfContainedSystem, gateway, noBuild, allFiles, quiet);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (formatFrontend)
            {
                Prerequisite.Ensure(Prerequisite.Node);
                RunFrontendFormat(quiet);
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            if (formatDeveloperCli)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                RunDeveloperCliFormat(noBuild, allFiles, quiet);
                developerCliTime = Stopwatch.GetElapsedTime(startTime) - backendTime - frontendTime;
            }

            if (quiet)
            {
                Console.WriteLine("Code formatted successfully.");
            }
            else
            {
                var uncommittedFilesAfterFormat = GitHelper.GetChangedFiles();
                var modifiedFiles = uncommittedFilesAfterFormat
                    .Where(kvp => !initialUncommittedFiles!.TryGetValue(kvp.Key, out var hash) || hash != kvp.Value)
                    .Select(kvp => kvp.Key)
                    .ToArray();

                if (modifiedFiles.Length > 0)
                {
                    AnsiConsole.MarkupLine("[yellow]Warning: Code format modified the following files:[/]");
                    AnsiConsole.MarkupLine($"[blue]{string.Join(Environment.NewLine, modifiedFiles)}[/]");
                }

                AnsiConsole.MarkupLine($"[green]Code format completed in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");

                var multipleTargets = (formatBackend ? 1 : 0) + (formatFrontend ? 1 : 0) + (formatDeveloperCli ? 1 : 0) > 1;
                if (multipleTargets)
                {
                    var timingLines = new List<string>();
                    if (formatBackend) timingLines.Add($"Backend:       [green]{backendTime.Format()}[/]");
                    if (formatFrontend) timingLines.Add($"Frontend:      [green]{frontendTime.Format()}[/]");
                    if (formatDeveloperCli) timingLines.Add($"Developer CLI: [green]{developerCliTime.Format()}[/]");
                    AnsiConsole.MarkupLine(string.Join(Environment.NewLine, timingLines));
                }
            }
        }
        catch (Exception ex)
        {
            if (quiet)
            {
                Console.WriteLine($"Format failed: {ex.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]Error during code format: {ex.Message}[/]");
            }

            Environment.Exit(1);
        }
    }

    private static void RunBackendFormat(string? selfContainedSystem, bool gateway, bool noBuild, bool allFiles, bool quiet)
    {
        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(gateway ? null : selfContainedSystem);

        if (!quiet) AnsiConsole.MarkupLine("[blue]Running backend code format...[/]");

        var includeArgument = string.Empty;
        if (gateway)
        {
            if (allFiles)
            {
                includeArgument = $""" --include="{AppGatewayHelper.IncludeGlob}" """.TrimEnd();
            }
            else
            {
                var changedCsFiles = AppGatewayHelper.FilterToAppGatewayFiles(GitHelper.GetChangedCsFilesInDirectory(solutionFile.Directory!.FullName));
                if (changedCsFiles.Length == 0)
                {
                    if (!quiet) AnsiConsole.MarkupLine("[green]No changed AppGateway C# files found, skipping backend format.[/]");
                    return;
                }

                includeArgument = $""" --include="{string.Join(";", changedCsFiles)}" """.TrimEnd();
                if (!quiet) AnsiConsole.MarkupLine($"[blue]Formatting {changedCsFiles.Length} changed AppGateway file(s)...[/]");
            }
        }
        else if (!allFiles)
        {
            var changedCsFiles = GitHelper.GetChangedCsFilesInDirectory(solutionFile.Directory!.FullName);
            if (changedCsFiles.Length == 0)
            {
                if (!quiet) AnsiConsole.MarkupLine("[green]No changed C# files found, skipping backend format.[/]");
                return;
            }

            includeArgument = $""" --include="{string.Join(";", changedCsFiles)}" """.TrimEnd();
            if (!quiet) AnsiConsole.MarkupLine($"[blue]Formatting {changedCsFiles.Length} changed file(s)...[/]");
        }

        if (!noBuild)
        {
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
        }

        ProcessHelper.Run(
            $"""dotnet jb cleanupcode {solutionFile.FullName} --profile=".NET only" --no-build{includeArgument}""",
            solutionFile.Directory!.FullName,
            "Format",
            quiet
        );
    }

    private static void RunFrontendFormat(bool quiet)
    {
        if (!quiet) AnsiConsole.MarkupLine("[blue]Running frontend code format...[/]");
        ProcessHelper.Run("npm run format", Configuration.ApplicationFolder, "Frontend format", quiet);
    }

    private static void RunDeveloperCliFormat(bool noBuild, bool allFiles, bool quiet)
    {
        var solutionFile = new FileInfo(Path.Combine(Configuration.CliFolder, "DeveloperCli.slnx"));

        if (!quiet) AnsiConsole.MarkupLine("[blue]Running developer-cli code format...[/]");

        var includeArgument = string.Empty;
        if (!allFiles)
        {
            var changedCsFiles = GitHelper.GetChangedCsFilesInDirectory(solutionFile.Directory!.FullName);
            if (changedCsFiles.Length == 0)
            {
                if (!quiet) AnsiConsole.MarkupLine("[green]No changed C# files found, skipping developer-cli format.[/]");
                return;
            }

            includeArgument = $""" --include="{string.Join(";", changedCsFiles)}" """.TrimEnd();
            if (!quiet) AnsiConsole.MarkupLine($"[blue]Formatting {changedCsFiles.Length} changed file(s)...[/]");
        }

        if (!noBuild)
        {
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
        }

        ProcessHelper.Run(
            $"""dotnet jb cleanupcode {solutionFile.FullName} --profile=".NET only" --no-build{includeArgument}""",
            solutionFile.Directory!.FullName,
            "Format",
            quiet
        );
    }
}
