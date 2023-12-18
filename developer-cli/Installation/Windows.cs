using System.Diagnostics;

namespace PlatformPlatform.DeveloperCli.Installation;

public static class Windows
{
    internal static bool IsAliasRegisteredWindows(string publishFolder)
    {
        var path = Environment.GetEnvironmentVariable("PATH")!;
        var paths = path.Split(';');
        return paths.Contains(publishFolder);
    }

    public static void RegisterAliasWindows(string publishFolder)
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c setx PATH \"%PATH%;{publishFolder}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        })!;
        process.WaitForExit();
    }
}