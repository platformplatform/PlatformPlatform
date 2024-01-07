using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class CodeCleanup : Command
{
    public CodeCleanup() : base("code-cleanup", "Run JetBrains Code Cleanup")
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
            Arguments = "jb cleanupcode PlatformPlatform.sln --profile=\".NET only\"",
            WorkingDirectory = workingDirectory
        });

        AnsiConsole.MarkupLine("[green]Code cleanup completed. Check Git to see any changes![/]");

        return 0;
    }
}