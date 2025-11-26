using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Xml.Linq;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class FormatCommand : Command
{
    public FormatCommand() : base("format", "Formats code to match code styling rules")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Format backend code" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Format frontend code" };
        var cliOption = new Option<bool>("--cli", "-c") { Description = "Format developer-cli code" };
        var selfContainedSystemOption = new Option<string?>("<self-contained-system>", "--self-contained-system", "-s") { Description = "The name of the self-contained system to format (e.g., account-management, back-office)" };
        var noBuildOption = new Option<bool>("--no-build") { Description = "Skip building and restoring before formatting" };
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
                RunBackendFormat(selfContainedSystem, noBuild, quiet);
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
                RunDeveloperCliFormat(noBuild, quiet);
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

    private static void RunBackendFormat(string? selfContainedSystem, bool noBuild, bool quiet)
    {
        var solutionFile = SelfContainedSystemHelper.GetSolutionFile(selfContainedSystem);

        if (!quiet) AnsiConsole.MarkupLine("[blue]Running backend code format...[/]");

        if (!noBuild)
        {
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
        }

        // .slnx files are not yet supported by JetBrains tools, so we need to create a temporary .slnf file
        var createTemporarySolutionFile = solutionFile.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
        var jetbrainsSupportedSolutionFile = string.Empty;
        try
        {
            jetbrainsSupportedSolutionFile = createTemporarySolutionFile
                ? CreateTemporaryJetBrainsCompatibleSolutionFile(solutionFile)
                : solutionFile.FullName;

            ProcessHelper.Run(
                $"""dotnet jb cleanupcode {jetbrainsSupportedSolutionFile} --profile=".NET only" --no-build""",
                solutionFile.Directory!.FullName,
                "Format",
                quiet
            );
        }
        finally
        {
            if (createTemporarySolutionFile && File.Exists(jetbrainsSupportedSolutionFile))
            {
                File.Delete(jetbrainsSupportedSolutionFile);
            }
        }
    }

    private static void RunFrontendFormat(bool quiet)
    {
        if (!quiet) AnsiConsole.MarkupLine("[blue]Running frontend code format...[/]");
        ProcessHelper.Run("npm run lint", Configuration.ApplicationFolder, "Frontend format", quiet);
    }

    private static void RunDeveloperCliFormat(bool noBuild, bool quiet)
    {
        var solutionFile = new FileInfo(Path.Combine(Configuration.CliFolder, "DeveloperCli.slnx"));

        if (!quiet) AnsiConsole.MarkupLine("[blue]Running developer-cli code format...[/]");

        if (!noBuild)
        {
            ProcessHelper.Run("dotnet tool restore", solutionFile.Directory!.FullName, "Tool restore", quiet);
        }

        // .slnx files are not yet supported by JetBrains tools, so we need to create a temporary .slnf file
        var jetbrainsSupportedSolutionFile = CreateTemporaryJetBrainsCompatibleSolutionFile(solutionFile);
        try
        {
            ProcessHelper.Run(
                $"""dotnet jb cleanupcode {jetbrainsSupportedSolutionFile} --profile=".NET only" --no-build""",
                solutionFile.Directory!.FullName,
                "Format",
                quiet
            );
        }
        finally
        {
            if (File.Exists(jetbrainsSupportedSolutionFile))
            {
                File.Delete(jetbrainsSupportedSolutionFile);
            }
        }
    }

    /// <summary>
    ///     Creates a temporary Solution Filter (.slnf) file that JetBrains tools can work with.
    /// </summary>
    /// <remarks>
    ///     This is a temporary workaround until JetBrains tools support the new .NET 9.2 .slnx format.
    ///     The method extracts all projects from the .slnx file and creates a compatible .slnf file that can be used
    ///     with the JetBrains cleanupcode tool, even if the soluition filter file points to the .slnx file.
    ///     This method can be removed when JetBrains "dotnet jb cleanupcode" adds native support for the .slnx format.
    /// </remarks>
    /// <param name="solutionFile">The .slnx solution file</param>
    /// <returns>Path to the temporary .slnf file</returns>
    private static string CreateTemporaryJetBrainsCompatibleSolutionFile(FileInfo solutionFile)
    {
        // Create content following the official .NET Solution File structure
        var solutionFilterFileContent = new
        {
            solution = new
            {
                path = solutionFile.FullName,
                projects = ExtractProjectPathsFromSlnx(solutionFile)
            }
        };

        // Create the temporary file path
        var temporarySolutionFile = $"{Path.GetTempFileName()}.slnf";

        // Write the .slnf file
        File.WriteAllText(
            temporarySolutionFile,
            JsonSerializer.Serialize(solutionFilterFileContent, new JsonSerializerOptions { WriteIndented = true })
        );

        return temporarySolutionFile;
    }

    private static List<string> ExtractProjectPathsFromSlnx(FileInfo solutionFile)
    {
        var projectPaths = new List<string>();

        try
        {
            var xDocument = XDocument.Load(solutionFile.FullName);
            var projectElements = xDocument.Descendants("Project");

            foreach (var projectElement in projectElements)
            {
                var pathAttribute = projectElement.Attribute("Path");
                if (pathAttribute is not null)
                {
                    projectPaths.Add(Path.Combine(solutionFile.Directory!.FullName, pathAttribute.Value));
                }
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Failed to parse .slnx file: {ex.Message}");
            Environment.Exit(1);
        }

        return projectPaths;
    }
}
