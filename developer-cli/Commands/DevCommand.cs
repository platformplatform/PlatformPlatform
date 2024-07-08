using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class DevCommand : Command
{
    public DevCommand() : base("dev", "Run the Aspire AppHost with all self-contained systems")
    {
        Handler = CommandHandler.Create(Execute);
    }

    private void Execute()
    {
        PrerequisitesChecker.Check("dotnet", "docker", "aspire", "node");

        var workingDirectory = Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application", "AppHost");

        if (!ProcessHelper.IsProcessRunning("Docker"))
        {
            AnsiConsole.MarkupLine("[green]Starting Docker Desktop[/]");
            ProcessHelper.StartProcess("open -a Docker", waitForExit: true);
        }

        AnsiConsole.MarkupLine("\n[green]Ensuring Docker image for SQL Server is up to date ...[/]");
        ProcessHelper.StartProcessWithSystemShell("docker pull mcr.microsoft.com/mssql/server:2022-latest");

        AnsiConsole.MarkupLine("\n[green]Ensuring Docker image for Azure Blob Storage Emulator ...[/]");
        ProcessHelper.StartProcessWithSystemShell("docker pull mcr.microsoft.com/azure-storage/azurite:latest");

        AnsiConsole.MarkupLine("\n[green]Ensuring Docker image for Mail Server and Web Client is up to date ...[/]");
        ProcessHelper.StartProcessWithSystemShell("docker pull axllent/mailpit:latest");

        Task.Run(async () =>
            {
                // Start a background task that monitors the websites and opens the browser when ready
                const int aspireDashboardPort = 9001;
                await StartBrowserWhenSiteIsReady(aspireDashboardPort);
                const int appPort = 9000;
                await StartBrowserWhenSiteIsReady(appPort);
            }
        );

        AnsiConsole.MarkupLine("\n[green]Starting the Aspire AppHost...[/]");
        ProcessHelper.StartProcess("dotnet run", workingDirectory);
    }

    private static async Task StartBrowserWhenSiteIsReady(int port)
    {
        var url = $"https://localhost:{port}";

        HttpClient httpClient = new();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PlatformPlatform Developer CLI");

        while (true)
        {
            try
            {
                var response = await httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch (HttpRequestException) // DNS issues, server down etc.
            {
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
        }

        ProcessHelper.StartProcess(new ProcessStartInfo("open", url), waitForExit: false);
    }
}
