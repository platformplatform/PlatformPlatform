using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class JetBrainsCodeInspections : Command
{
    public JetBrainsCodeInspections() :
        base("jetbrains-code-inspections", "Run JetBrains Code Inspections")
    {
        Handler = CommandHandler.Create(new Func<int>(Execute));
    }

    private int Execute()
    {
        var workingDirectory = Path.Combine(AliasRegistration.SolutionFolder, "..", "application");

        ProcessHelpers.StartProcess("dotnet", "tool restore", workingDirectory);
        ProcessHelpers.StartProcess(
            "dotnet",
            "jb inspectcode PlatformPlatform.sln --build --output=result.xml --severity=SUGGESTION",
            workingDirectory
        );

        var resultXml = File.ReadAllText(Path.Combine(workingDirectory, "result.xml"));
        if (resultXml.Contains("<Issues />"))
        {
            AnsiConsole.MarkupLine("[green]No issues found![/]");
        }
        else
        {
            ProcessHelpers.StartProcess("code", "result.xml", workingDirectory);
        }

        return 0;
    }
}