using System.CommandLine;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;

namespace DeveloperCli.Commands;

public class BrowserToolsCommand : Command
{
    public BrowserToolsCommand() : base(name: "browser-tools", description: "Start the browser tools server for debugging")
    {
        this.AddAlias("bt");
        this.SetHandler(Execute);
    }

    private void Execute()
    {
        ProcessHelper.StartProcessWithSystemShell(
            "npx @agentdeskai/browser-tools-server@1.2.0",
            Configuration.SourceCodeFolder
        );
    }
}
