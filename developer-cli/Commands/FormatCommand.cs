using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
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
        AddOption(new Option<bool?>(["--backend", "-b"], "Only format backend code"));
        AddOption(new Option<bool?>(["--frontend", "-f"], "Only format frontend code"));
        AddOption(new Option<string?>(["<solution-name>", "--solution-name", "-s"], "The name of the self-contained system to format (only used for backend code)"));

        Handler = CommandHandler.Create<bool, bool, string?>(Execute);
    }

    private static void Execute(bool backend, bool frontend, string? solutionName)
    {
        var formatBackend = backend || !frontend;
        var formatFrontend = frontend || !backend;

        try
        {
            var initialUncommittedFiles = GitHelper.GetChangedFiles();
            if (initialUncommittedFiles.Count > 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: You have unstaged changes in your working directory.[/]");
            }

            var startTime = Stopwatch.GetTimestamp();
            var backendTime = TimeSpan.Zero;
            var frontendTime = TimeSpan.Zero;

            if (formatBackend)
            {
                Prerequisite.Ensure(Prerequisite.Dotnet);
                RunBackendFormat(solutionName);
                backendTime = Stopwatch.GetElapsedTime(startTime);
            }

            if (formatFrontend)
            {
                Prerequisite.Ensure(Prerequisite.Node);
                RunFrontendFormat();
                frontendTime = Stopwatch.GetElapsedTime(startTime) - backendTime;
            }

            var uncommittedFilesAfterFormat = GitHelper.GetChangedFiles();
            var modifiedFiles = uncommittedFilesAfterFormat
                .Where(kvp => !initialUncommittedFiles.TryGetValue(kvp.Key, out var hash) || hash != kvp.Value)
                .Select(kvp => kvp.Key)
                .ToArray();

            if (modifiedFiles.Length > 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning: Code format modified the following files:[/]");
                AnsiConsole.MarkupLine($"[blue]{string.Join(Environment.NewLine, modifiedFiles)}[/]");
            }

            AnsiConsole.MarkupLine($"[green]Code format completed in {Stopwatch.GetElapsedTime(startTime).Format()}[/]");
            if (formatBackend && formatFrontend)
            {
                AnsiConsole.MarkupLine(
                    $"""
                     Backend:     [green]{backendTime.Format()}[/]
                     Frontend:    [green]{frontendTime.Format()}[/]
                     """
                );
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error during code format: {ex.Message}[/]");
            Environment.Exit(1);
        }
    }

    private static void RunBackendFormat(string? solutionName)
    {
        AnsiConsole.MarkupLine("[blue]Running backend code format...[/]");
        var solutionFile = SolutionHelper.GetSolution(solutionName);
        ProcessHelper.StartProcess("dotnet tool restore", solutionFile.Directory!.FullName);

        // .slnx files are not yet supported by JetBrains tools, so we need to create a temporary .slnf file
        var createTemporarySolutionFile = solutionFile.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
        var jetbrainsSupportedSolutionFile = string.Empty;
        try
        {
            jetbrainsSupportedSolutionFile = createTemporarySolutionFile
                ? CreateTemporaryJetBrainsCompatibleSolutionFile(solutionFile)
                : solutionFile.FullName;

            ProcessHelper.StartProcess(
                $"""dotnet jb cleanupcode {jetbrainsSupportedSolutionFile} --profile=".NET only" --no-build""",
                solutionFile.Directory!.FullName
            );
        }
        finally
        {
            if (createTemporarySolutionFile)
            {
                File.Delete(jetbrainsSupportedSolutionFile);
            }
        }
    }

    private static void RunFrontendFormat()
    {
        AnsiConsole.MarkupLine("[blue]Running frontend code format...[/]");
        ProcessHelper.StartProcess("npm run lint", Configuration.ApplicationFolder);
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
