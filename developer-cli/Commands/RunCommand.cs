using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class RunCommand : Command
{
    public RunCommand() : base("run", "Run the Aspire AppHost with all self-contained systems")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private int Execute()
    {
        var workingDirectory = Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application", "AppHost");

        ProcessHelper.StartProcess($"dotnet run {workingDirectory}", workingDirectory);

        return 0;
    }
}