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
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Docker, Prerequisite.Aspire, Prerequisite.Node);

        var workingDirectory = Path.Combine(Configuration.GetSourceCodeFolder(), "..", "application", "AppHost");

        StartDockerIfNotRunning();

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

    private static void StartDockerIfNotRunning()
    {
        var dockerProcessName = Configuration.IsWindows ? "Docker Desktop" : "Docker";

        if (!ProcessHelper.IsProcessRunning(dockerProcessName))
        {
            StartDocker();
        }

        WaitUntilDockerIsResponsive();

        void StartDocker()
        {
            AnsiConsole.MarkupLine("[green]Starting Docker Desktop[/]");
            if (Configuration.IsWindows)
            {
                // Docker Desktop folder is not registered in the path by default, and cannot be found using "where" command.
                // The default install location is the best guess.
                var dockerDesktopPath = @"C:\Program Files\Docker\Docker\Docker Desktop.exe";
                if (!File.Exists(dockerDesktopPath))
                {
                    AnsiConsole.MarkupLine("[red]Docker Desktop is not found in default location.[/]");
                    Environment.Exit(1);
                }

                ProcessHelper.StartProcess(new ProcessStartInfo { FileName = dockerDesktopPath, CreateNoWindow = true });
            }
            else
            {
                ProcessHelper.StartProcess("open -a Docker", waitForExit: true);
            }
        }

        void WaitUntilDockerIsResponsive()
        {
            var maxRetries = 30;
            var retriesLeft = maxRetries;
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps",
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
            };

            while (retriesLeft > 0)
            {
                var output = ProcessHelper.StartProcess(processStartInfo, waitForExit: true);
                if (output.Contains("CONTAINER ID")) return;

                retriesLeft--;
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }

            AnsiConsole.MarkupLine($"[red]Docker Desktop is not responsive after {maxRetries} seconds.[/]");
            Environment.Exit(1);
        }
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
