using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class UpdatePackagesCommand : Command
{
    private static readonly string[] RestrictedNuGetPackages = ["MediatR", "FluentAssertions"];

    public UpdatePackagesCommand() : base("update-packages", "Updates packages to their latest versions while preserving major versions for restricted packages")
    {
        AddOption(new Option<bool>(["--backend", "-b"], "Update only backend packages (NuGet)"));
        AddOption(new Option<bool>(["--frontend", "-f"], "Update only frontend packages (npm)"));
        AddOption(new Option<bool>(["--dry-run", "-d"], "Show what would be updated without making changes"));
        AddOption(new Option<bool>(["--build"], "Run build command after successful package updates"));
        AddOption(new Option<string?>(["--exclude", "-e"], "Comma-separated list of packages to exclude from updates"));
        Handler = CommandHandler.Create<bool, bool, bool, bool, string?>(Execute);
    }

    private static async Task Execute(bool backend, bool frontend, bool dryRun, bool build, string? exclude)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        // Always update unless --dry-run is specified
        var performUpdate = !dryRun;

        var excludedPackages = exclude?.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrWhiteSpace(p)).ToArray() ?? [];
        if (excludedPackages.Length > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Excluding packages: {string.Join(", ", excludedPackages)}[/]");
        }

        if (dryRun)
        {
            AnsiConsole.MarkupLine("[blue]Running in dry-run mode - no changes will be made[/]");
        }

        var updateBackend = backend || (!backend && !frontend);
        var updateFrontend = frontend || (!backend && !frontend);

        if (updateBackend)
        {
            await UpdateNuGetPackagesAsync(performUpdate, excludedPackages);

            if (build && performUpdate)
            {
                await RunBuildCommand(true, false);
            }
        }

        if (updateFrontend)
        {
            UpdateNpmPackages(performUpdate, excludedPackages);

            if (build && performUpdate)
            {
                await RunBuildCommand(false, true);
            }
        }
    }

    private static async Task UpdateNuGetPackagesAsync(bool performUpdate, string[] excludedPackages)
    {
        var directoryPackagesPath = Path.Combine(Configuration.ApplicationFolder, "Directory.Packages.props");
        if (!File.Exists(directoryPackagesPath))
        {
            AnsiConsole.MarkupLine($"[red]Directory.Packages.props not found at {directoryPackagesPath}[/]");
            return;
        }

        AnsiConsole.MarkupLine("Analyzing NuGet packages in Directory.Packages.props...");

        var xDocument = XDocument.Load(directoryPackagesPath);
        var packageElements = xDocument.Descendants("PackageVersion").ToArray();

        var outdatedPackagesJson = await GetOutdatedPackagesJsonAsync();
        if (outdatedPackagesJson is null)
        {
            AnsiConsole.MarkupLine("[red]Failed to get outdated packages information[/]");
            return;
        }

        var table = new Table();
        table.AddColumn("Package");
        table.AddColumn("Current Version");
        table.AddColumn("Latest Version");
        table.AddColumn("Status");

        var updates = new List<PackageUpdate>();
        var warningCount = 0;

        foreach (var packageElement in packageElements)
        {
            var packageName = packageElement.Attribute("Include")?.Value;
            var currentVersion = packageElement.Attribute("Version")?.Value;
            if (packageName is null || currentVersion is null)
            {
                continue;
            }

            // Skip excluded packages
            if (excludedPackages.Contains(packageName))
            {
                table.AddRow(packageName, currentVersion, "-", "[blue]Excluded[/]");
                continue;
            }

            var versionResolution = await ResolvePackageVersionAsync(outdatedPackagesJson, packageName, currentVersion);
            
            if (versionResolution.HasWarning)
            {
                warningCount++;
                table.AddRow(packageName, currentVersion, "[yellow]Unknown[/]", $"[yellow]{versionResolution.WarningMessage}[/]");
                continue;
            }
            
            if (versionResolution.LatestVersion is null)
            {
                // Package is up to date or couldn't be resolved
                continue;
            }

            var status = GetNuGetUpdateStatus(packageName, currentVersion, versionResolution.LatestVersion);
            var statusColor = status.IsRestricted ? "[red]Restricted[/]" : performUpdate ? "[green]Updated[/]" : "[yellow]Will update[/]";

            table.AddRow(packageName, currentVersion, versionResolution.LatestVersion, statusColor);

            if (status.CanUpdate)
            {
                updates.Add(new PackageUpdate(packageElement, packageName, currentVersion, status.TargetVersion));
            }
        }

        if (warningCount > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: {warningCount} package(s) could not be properly resolved.[/]");
        }

        if (table.Rows.Count > 0)
        {
            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All NuGet packages are up to date![/]");
        }

        if (updates.Count > 0 && performUpdate)
        {
            foreach (var update in updates)
            {
                update.Element.SetAttributeValue("Version", update.NewVersion);
                AnsiConsole.MarkupLine($"[green]Updated {update.PackageName} from {update.CurrentVersion} to {update.NewVersion}[/]");
            }

            xDocument.Save(directoryPackagesPath);
            AnsiConsole.MarkupLine("[green]Directory.Packages.props updated successfully![/]");
            
            // Check for package downgrades and resolve them
            await FixPackageDowngradesAsync(directoryPackagesPath);
        }
        else if (updates.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {updates.Count} NuGet package(s) (dry-run mode)[/]");
        }
    }

    private static async Task FixPackageDowngradesAsync(string directoryPackagesPath)
    {
        AnsiConsole.MarkupLine("[blue]Checking for package downgrades...[/]");
        
        // Run a build to see if there are any package downgrade errors
        var output = await Task.Run(() => ProcessHelper.StartProcess(
            "dotnet build /Users/thomasjespersen/Developer/PlatformPlatform/application/AppHost/AppHost.csproj -c Release -v q",
            Configuration.ApplicationFolder,
            exitOnError: false,
            redirectOutput: true,
            throwOnError: false
        ));
        
        var downgradePattern = new Regex(@"Detected package downgrade: (.*?) from ([\d\.]+) to centrally defined ([\d\.]+)");
        var matches = downgradePattern.Matches(output);
        
        if (matches.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]No package downgrades detected.[/]");
            return;
        }
        
        AnsiConsole.MarkupLine($"[yellow]Found {matches.Count} package downgrade issues. Attempting to fix...[/]");
        
        var xDocument = XDocument.Load(directoryPackagesPath);
        var packageElements = xDocument.Descendants("PackageVersion").ToDictionary(
            x => x.Attribute("Include")?.Value ?? string.Empty, 
            x => x
        );
        
        var fixedCount = 0;
        foreach (Match match in matches)
        {
            var packageName = match.Groups[1].Value;
            var requiredVersion = match.Groups[2].Value;
            var currentVersion = match.Groups[3].Value;
            
            if (packageElements.TryGetValue(packageName, out var element))
            {
                element.SetAttributeValue("Version", requiredVersion);
                AnsiConsole.MarkupLine($"[green]Fixed downgrade: {packageName} from {currentVersion} to {requiredVersion}[/]");
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            xDocument.Save(directoryPackagesPath);
            AnsiConsole.MarkupLine($"[green]Fixed {fixedCount} package downgrade issues.[/]");
        }
    }

    private static async Task<VersionResolution> ResolvePackageVersionAsync(JsonDocument outdatedPackagesJson, string packageName, string currentVersion)
    {
        // First try to get the latest version from the outdated packages JSON
        var latestVersion = GetLatestVersionFromJson(outdatedPackagesJson, packageName);
        var isCurrentPrerelease = IsPreReleaseVersion(currentVersion);
        
        // Check if it's a "Not found at the sources" case
        if (latestVersion == "Not found at the sources")
        {
            if (isCurrentPrerelease)
            {
                var prereleaseVersion = await GetLatestPreReleaseVersionAsync(packageName, currentVersion);
                if (prereleaseVersion is null || IsIncompatiblePrerelease(prereleaseVersion))
                {
                    return new VersionResolution(null, true, $"Could not resolve compatible prerelease version for '{packageName}'");
                }
                
                // Check if the prerelease version is different from current
                if (prereleaseVersion != currentVersion)
                {
                    return new VersionResolution(prereleaseVersion);
                }
                
                return new VersionResolution(null);
            }
            
            return new VersionResolution(null, true, $"Package '{packageName}' reported as not found at the sources");
        }
        
        // Filter out incompatible prerelease versions (e.g., .NET 10 previews when using .NET 9)
        if (latestVersion is not null && IsIncompatiblePrerelease(latestVersion))
        {
            // Try to find a compatible stable version instead
            latestVersion = null;
        }
        
        // If no latest version was found, check if it's a prerelease package
        if (latestVersion is null && isCurrentPrerelease)
        {
            var prereleaseVersion = await GetLatestPreReleaseVersionAsync(packageName, currentVersion);
            if (prereleaseVersion is null || IsIncompatiblePrerelease(prereleaseVersion))
            {
                return new VersionResolution(null, true, $"Could not resolve compatible prerelease version for '{packageName}'");
            }
            
            // Compare versions to determine if we should update
            if (prereleaseVersion == currentVersion)
            {
                // Up to date
                return new VersionResolution(null);
            }
            
            return new VersionResolution(prereleaseVersion);
        }
        
        // If we have a prerelease but a stable version is available, use the stable version
        if (isCurrentPrerelease && latestVersion is not null && !IsPreReleaseVersion(latestVersion))
        {
            return new VersionResolution(latestVersion);
        }
        
        return new VersionResolution(latestVersion);
    }

    private static bool IsIncompatiblePrerelease(string version)
    {
        // Filter out .NET 10 preview versions when using .NET 9
        // These typically contain "10.0.0-preview" in the version string
        return version.Contains("10.0.0-preview") || version.Contains("11.0.0-preview");
    }

    private static async Task<JsonDocument?> GetOutdatedPackagesJsonAsync()
    {
        var output = "";
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            output = ProcessHelper.StartProcess(
                "dotnet list package --outdated --include-prerelease --format json",
                Configuration.ApplicationFolder,
                exitOnError: false,
                redirectOutput: true
            );

            if (!string.IsNullOrEmpty(output)) break;
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        if (string.IsNullOrEmpty(output)) Environment.Exit(1);

        return JsonDocument.Parse(output);
    }

    private static string? GetLatestVersionFromJson(JsonDocument jsonDocument, string packageName)
    {
        var projects = jsonDocument.RootElement.GetProperty("projects");

        foreach (var project in projects.EnumerateArray())
        {
            if (!project.TryGetProperty("frameworks", out var frameworks))
            {
                continue;
            }

            foreach (var framework in frameworks.EnumerateArray())
            {
                if (!framework.TryGetProperty("topLevelPackages", out var packages))
                {
                    continue;
                }

                foreach (var package in packages.EnumerateArray())
                {
                    if (package.GetProperty("id").GetString() == packageName)
                    {
                        return package.GetProperty("latestVersion").GetString();
                    }
                }
            }
        }

        return null;
    }

    private static async Task<string?> GetLatestPreReleaseVersionAsync(string packageName, string currentVersion)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
            var json = JsonDocument.Parse(response);
            var versions = json.RootElement.GetProperty("versions");

            var matchingVersions = new List<string>();
            foreach (var version in versions.EnumerateArray())
            {
                var versionString = version.GetString();
                if (versionString is not null && IsPreReleaseVersion(versionString))
                {
                    matchingVersions.Add(versionString);
                }
            }

            return matchingVersions.LastOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsPreReleaseVersion(string version)
    {
        return version.Contains("-") || version.Contains("alpha") || version.Contains("beta") || version.Contains("rc");
    }

    private static void UpdateNpmPackages(bool performUpdate, string[] excludedPackages)
    {
        var packageJsonPath = Path.Combine(Configuration.ApplicationFolder, "package.json");

        if (!File.Exists(packageJsonPath))
        {
            AnsiConsole.MarkupLine($"[red]package.json not found at {packageJsonPath}[/]");
            return;
        }

        AnsiConsole.MarkupLine("Analyzing npm packages in package.json...");

        var output = ProcessHelper.StartProcess("npm outdated --json", Configuration.ApplicationFolder, true, exitOnError: false, throwOnError: false);

        if (string.IsNullOrWhiteSpace(output))
        {
            AnsiConsole.MarkupLine("[green]All npm packages are up to date![/]");
            return;
        }

        var outdatedPackages = JsonDocument.Parse(output);
        var table = new Table();
        table.AddColumn("Package");
        table.AddColumn("Current Version");
        table.AddColumn("Latest Version");
        table.AddColumn("Status");

        var updates = new List<string>();

        foreach (var package in outdatedPackages.RootElement.EnumerateObject())
        {
            var packageName = package.Name;
            
            // Skip excluded packages
            if (excludedPackages.Contains(packageName))
            {
                if (package.Value.TryGetProperty("current", out var packageCurrentElement))
                {
                    var packageCurrentVersion = packageCurrentElement.GetString() ?? "unknown";
                    table.AddRow(packageName, packageCurrentVersion, "-", "[blue]Excluded[/]");
                }
                continue;
            }
            
            var packageInfo = package.Value;

            if (!packageInfo.TryGetProperty("current", out var currentElement) ||
                !packageInfo.TryGetProperty("latest", out var latestElement))
            {
                continue;
            }

            var currentVersion = currentElement.GetString();
            var latestVersion = latestElement.GetString();

            if (currentVersion is null || latestVersion is null)
            {
                continue;
            }

            var statusColor = performUpdate ? "[green]Updated[/]" : "[yellow]Will update[/]";
            table.AddRow(packageName, currentVersion, latestVersion, statusColor);
            updates.Add($"{packageName}@{latestVersion}");
        }

        if (table.Rows.Count > 0)
        {
            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All npm packages are up to date![/]");
            return;
        }

        if (updates.Count > 0 && performUpdate)
        {
            var updateCommand = $"npm install {string.Join(" ", updates)}";
            ProcessHelper.StartProcess(updateCommand, Configuration.ApplicationFolder);
            AnsiConsole.MarkupLine("[green]npm packages updated successfully![/]");
        }
        else if (updates.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {updates.Count} npm package(s) (dry-run mode)[/]");
        }
    }

    private static UpdateStatus GetNuGetUpdateStatus(string packageName, string currentVersion, string latestVersion)
    {
        if (!RestrictedNuGetPackages.Contains(packageName))
        {
            return new UpdateStatus(true, false, latestVersion);
        }

        var currentMajor = GetMajorVersion(currentVersion);
        var latestMajor = GetMajorVersion(latestVersion);

        if (currentMajor == latestMajor)
        {
            return new UpdateStatus(true, false, latestVersion);
        }

        // Find the latest version within the same major version
        var targetVersion = FindLatestVersionInMajor(packageName, currentMajor).Result;
        return new UpdateStatus(targetVersion is not null, true, targetVersion ?? currentVersion);
    }

    private static async Task<string?> FindLatestVersionInMajor(string packageName, int majorVersion)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
            var json = JsonDocument.Parse(response);
            var versions = json.RootElement.GetProperty("versions");

            var matchingVersions = new List<string>();
            foreach (var version in versions.EnumerateArray())
            {
                var versionString = version.GetString();
                if (versionString is not null && !versionString.Contains("-") && GetMajorVersion(versionString) == majorVersion)
                {
                    matchingVersions.Add(versionString);
                }
            }

            return matchingVersions.LastOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static int GetMajorVersion(string version)
    {
        var match = Regex.Match(version, @"^(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static async Task RunBuildCommand(bool backend, bool frontend)
    {
        if (!backend && !frontend)
        {
            return;
        }

        AnsiConsole.MarkupLine("[blue]Running build command...[/]");

        try
        {
            var buildCommand = new BuildCommand();
            var args = new List<string>();

            if (backend && !frontend)
            {
                args.Add("--backend");
            }
            else if (frontend && !backend)
            {
                args.Add("--frontend");
            }
            // If both are true, run without flags (builds both)

            await buildCommand.InvokeAsync(args.ToArray());
            AnsiConsole.MarkupLine("[green]Build completed successfully![/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Build failed: {ex.Message}[/]");
        }
    }

    private sealed record PackageUpdate(XElement Element, string PackageName, string CurrentVersion, string NewVersion);

    private sealed record UpdateStatus(bool CanUpdate, bool IsRestricted, string TargetVersion);
    
    private sealed record VersionResolution(string? LatestVersion, bool HasWarning = false, string? WarningMessage = null);
}
