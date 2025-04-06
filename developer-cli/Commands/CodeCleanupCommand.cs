using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using System.Xml.Linq;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class CodeCleanupCommand : Command
{
    public CodeCleanupCommand() : base("code-cleanup", "Run JetBrains Code Cleanup")
    {
        AddOption(new Option<string?>(["<solution-name>", "--solution-name", "-s"], "The name of the self-contained system to build"));

        Handler = CommandHandler.Create(Execute);
    }

    private static void Execute(string? solutionName)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet);

        var solutionFile = SolutionHelper.GetSolution(solutionName);
        ProcessHelper.StartProcess("dotnet tool restore", solutionFile.Directory!.FullName);

        // .slnx files are not yet supported by JetBrains tools, so we need to create a temporary .slnf file
        var createTemporarySolutionFile = solutionFile.Extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase);
        var jetbrainsSupportedSolutionFile = createTemporarySolutionFile
            ? CreateTemporaryJetBrainsCompatibleSolutionFile(solutionFile)
            : solutionFile.FullName;

        ProcessHelper.StartProcess(
            $"""dotnet jb cleanupcode {jetbrainsSupportedSolutionFile} --profile=".NET only" --no-build""",
            solutionFile.Directory!.FullName
        );

        if (createTemporarySolutionFile)
        {
            File.Delete(jetbrainsSupportedSolutionFile);
        }

        AnsiConsole.MarkupLine("[green]Code cleanup completed. Check Git to see any changes![/]");
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
