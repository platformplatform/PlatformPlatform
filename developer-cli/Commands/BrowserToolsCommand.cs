using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace PlatformPlatform.DeveloperCli.Commands;

public class BrowserToolsCommand : Command
{
    public BrowserToolsCommand() : base("browser-tools", "Start the browser MCP server for debugging")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private static void Execute()
    {
        Prerequisite.Ensure(Prerequisite.Node);

        ProcessHelper.StartProcessWithSystemShell("npx @agentdeskai/browser-tools-server@1.2.0", Configuration.SourceCodeFolder);
    }
}
