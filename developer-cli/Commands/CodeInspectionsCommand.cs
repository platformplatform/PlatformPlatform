using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class CodeInspectionsCommand : Command
{
    public CodeInspectionsCommand() : base("code-inspections", "Run JetBrains Code Inspections")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private int Execute()
    {
        var workingDirectory = Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application");

        ProcessHelper.StartProcess("dotnet tool restore", workingDirectory);

        ProcessHelper.StartProcess(
            "dotnet jb inspectcode PlatformPlatform.sln --build --output=result.json --severity=SUGGESTION",
            workingDirectory
        );

        var resultXml = File.ReadAllText(Path.Combine(workingDirectory, "result.json"));
        if (resultXml.Contains("\"results\": [],"))
        {
            AnsiConsole.MarkupLine("[green]No issues found![/]");
        }
        else
        {
            ProcessHelper.StartProcess("code result.json", workingDirectory);
        }

        return 0;
    }
}
