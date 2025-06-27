using System.Diagnostics;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class PlaywrightInstaller
{
    public static void EnsurePlaywrightBrowsers()
    {
        AnsiConsole.MarkupLine("[blue]Ensuring Playwright browsers are installed...[/]");

        var processStartInfo = new ProcessStartInfo
        {
            FileName = Configuration.IsWindows ? "cmd.exe" : "npx",
            Arguments = $"{(Configuration.IsWindows ? "/C npx" : string.Empty)} --yes playwright install --with-deps",
            WorkingDirectory = Configuration.ApplicationFolder,
            UseShellExecute = false
        };

        ProcessHelper.StartProcess(processStartInfo, throwOnError: true);
    }
}
