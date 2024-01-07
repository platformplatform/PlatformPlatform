using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class CodeInspections : Command
{
    public CodeInspections() : base("code-inspections", "Run JetBrains Code Inspections")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private int Execute()
    {
        var workingDirectory = Path.Combine(Environment.SolutionFolder, "..", "application");

        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "tool restore",
            WorkingDirectory = workingDirectory
        });

        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "jb inspectcode PlatformPlatform.sln --build --output=result.xml --severity=SUGGESTION",
            WorkingDirectory = workingDirectory
        });

        var resultXml = File.ReadAllText(Path.Combine(workingDirectory, "result.xml"));
        if (resultXml.Contains("<Issues />"))
        {
            AnsiConsole.MarkupLine("[green]No issues found![/]");
        }
        else
        {
            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "code",
                Arguments = "result.xml",
                WorkingDirectory = workingDirectory
            });
        }

        return 0;
    }
}