using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
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

    private void Execute()
    {
        var workingDirectory = Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application", "AppHost");

        Task.Run(async () =>
        {
            // Start a background task that monitors the websites and opens the browser when ready
            const int aspireDashboardPort = 8001;
            await StartBrowserWhenSiteIsReady(aspireDashboardPort);
            const int accountManagementPort = 8443;
            await StartBrowserWhenSiteIsReady(accountManagementPort);
        });

        ProcessHelper.StartProcess("dotnet run", workingDirectory);
    }

    private static async Task StartBrowserWhenSiteIsReady(int port)
    {
        var url = $"https://localhost:{port}";

        var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
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