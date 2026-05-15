using DeveloperCli.Installation;
using Spectre.Console;

namespace DeveloperCli.Utilities;

// AppGateway is not a self-contained system (no .slnf, no Api/Core/Tests/WebApp layout), so the
// generic SCS helper does not apply. The CLI scopes AppGateway work via this helper: a build/test
// target csproj for dotnet build/test, and an include-glob for JetBrains inspectcode/cleanupcode.
public static class AppGatewayHelper
{
    // Building/testing the test project transitively builds AppGateway itself plus SharedKernel.
    public const string TestProjectRelativePath = "AppGateway.Tests/AppGateway.Tests.csproj";

    // Include glob for JetBrains tools. Covers both the AppGateway project and its test project,
    // expressed relative to the application/ working directory where the slnx lives.
    public const string IncludeGlob = "AppGateway/**/*.cs;AppGateway.Tests/**/*.cs";

    // Prefixes used to filter the changed-file list (which is relative to application/) down to
    // AppGateway scope. Both prefixes are checked because GetChangedCsFilesInDirectory normalizes
    // path separators to the platform's preference.
    private static readonly string[] PathPrefixes =
    [
        "AppGateway" + Path.DirectorySeparatorChar,
        "AppGateway.Tests" + Path.DirectorySeparatorChar
    ];

    public static string GetTestProjectPath()
    {
        return Path.Combine(Configuration.ApplicationFolder, TestProjectRelativePath);
    }

    public static string[] FilterToAppGatewayFiles(string[] changedFilesRelativeToApplication)
    {
        return changedFilesRelativeToApplication
            .Where(file => PathPrefixes.Any(prefix => file.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
    }

    public static void EnsureNotCombinedWithSelfContainedSystem(string? selfContainedSystem)
    {
        if (selfContainedSystem is null) return;

        AnsiConsole.MarkupLine("[red]Error: --gateway/-g and --self-contained-system/-s cannot be combined. Pick one scope.[/]");
        Environment.Exit(1);
    }
}
