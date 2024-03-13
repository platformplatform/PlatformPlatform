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
            "dotnet jb inspectcode PlatformPlatform.sln --build --output=result.xml --severity=SUGGESTION",
            workingDirectory
        );

        var resultXml = File.ReadAllText(Path.Combine(workingDirectory, "result.xml"));
        if (resultXml.Contains("<Issues />"))
        {
            AnsiConsole.MarkupLine("[green]No issues found![/]");
        }
        else
        {
            ProcessHelper.StartProcess("code result.xml", workingDirectory);
        }

        return 0;
    }
}