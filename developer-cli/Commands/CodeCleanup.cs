using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class CodeCleanup : Command
{
    public CodeCleanup() :
        base("code-cleanup", "Run JetBrains Code Cleanup")
    {
        Handler = CommandHandler.Create(new Func<int>(Execute));
    }

    private int Execute()
    {
        var workingDirectory = Path.Combine(Environment.SolutionFolder, "..", "application");

        ProcessHelper.StartProcess("dotnet", "tool restore", workingDirectory);
        ProcessHelper.StartProcess(
            "dotnet",
            "jb cleanupcode PlatformPlatform.sln --profile=\".NET only\"",
            workingDirectory
        );

        var resultXml = File.ReadAllText(Path.Combine(workingDirectory, "result.xml"));
        if (resultXml.Contains("<Issues />"))
        {
            AnsiConsole.MarkupLine("[green]No issues found![/]");
        }
        else
        {
            ProcessHelper.StartProcess("code", "result.xml", workingDirectory);
        }

        return 0;
    }
}