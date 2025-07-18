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
    private static readonly string[] RestrictedNuGetPackages = ["MediatR", "FluentAssertions", "Microsoft.OpenApi", "Microsoft.OpenApi.Readers"];
    private static readonly Dictionary<string, string?> NuGetApiCache = new();

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
            await UpdateNuGetPackagesAsync(dryRun, excludedPackages);
            UpdateAspireSdkVersion(dryRun);

            if (build && !dryRun)
            {
                await RunBuildCommand(true, false);
            }
        }

        if (updateFrontend)
        {
            UpdateNpmPackages(dryRun, excludedPackages);

            if (build && !dryRun)
            {
                await RunBuildCommand(false, true);
            }
        }
    }

    private static async Task UpdateNuGetPackagesAsync(bool dryRun, string[] excludedPackages)
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
        var upToDateCount = 0;
        var totalPackages = packageElements.Length;

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
                upToDateCount++;
                continue;
            }

            var status = GetNuGetUpdateStatus(packageName, currentVersion, versionResolution.LatestVersion);
            var statusColor = status.IsRestricted ? "[red]Restricted[/]" : dryRun ? "[yellow]Will update[/]" : "[green]Updated[/]";

            table.AddRow(packageName, currentVersion, versionResolution.LatestVersion, statusColor);

            if (status.CanUpdate)
            {
                updates.Add(new PackageUpdate(packageElement, packageName, currentVersion, status.TargetVersion));
            }
        }
        
        // Check for dependency conflicts and update Microsoft.Extensions.* packages to 9.0.7 if needed
        var hasResilienceUpdate = updates.Any(u => u.PackageName == "Microsoft.Extensions.Http.Resilience" && u.NewVersion == "9.7.0");
        if (hasResilienceUpdate)
        {
            var microsoftExtensionsPackages = packageElements
                .Where(p => p.Attribute("Include")?.Value?.StartsWith("Microsoft.Extensions.") == true || 
                           p.Attribute("Include")?.Value?.StartsWith("Microsoft.EntityFrameworkCore") == true ||
                           p.Attribute("Include")?.Value?.StartsWith("Microsoft.AspNetCore") == true)
                .Where(p => p.Attribute("Version")?.Value?.StartsWith("9.0.") == true)
                .ToList();
            
            foreach (var packageElement in microsoftExtensionsPackages)
            {
                var packageName = packageElement.Attribute("Include")?.Value;
                var currentVersion = packageElement.Attribute("Version")?.Value;
                if (packageName is null || currentVersion is null) continue;
                
                // Ensure all Microsoft.Extensions.* packages are at 9.0.7
                if (currentVersion == "9.0.4" && !updates.Any(u => u.PackageName == packageName))
                {
                    updates.Add(new PackageUpdate(packageElement, packageName, currentVersion, "9.0.7"));
                    table.AddRow(packageName, currentVersion, "9.0.7", dryRun ? "[yellow]Will update (dependency)[/]" : "[green]Updated (dependency)[/]");
                }
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

        if (updates.Count > 0 && !dryRun)
        {
            foreach (var update in updates)
            {
                update.Element.SetAttributeValue("Version", update.NewVersion);
                AnsiConsole.MarkupLine($"[green]Updated {update.PackageName} from {update.CurrentVersion} to {update.NewVersion}[/]");
            }

            xDocument.Save(directoryPackagesPath);
            AnsiConsole.MarkupLine("[green]Directory.Packages.props updated successfully![/]");
        }
        else if (updates.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {updates.Count} NuGet package(s) (dry-run mode)[/]");
        }
    }

    private static async Task<VersionResolution> ResolvePackageVersionAsync(JsonDocument outdatedPackagesJson, string packageName, string currentVersion)
    {
        var latestFromOutdated = GetLatestVersionFromJson(outdatedPackagesJson, packageName);

        if (latestFromOutdated is null)
        {
            // Package not found in outdated JSON - check NuGet API directly
            var latestFromApi = await GetLatestVersionFromNuGetApi(packageName);
            if (latestFromApi is null)
            {
                return new VersionResolution(null, true, $"Failed to retrieve version information for '{packageName}'");
            }

            // Compare versions to see if an update is available
            if (latestFromApi != currentVersion && IsNewerVersion(latestFromApi, currentVersion))
            {
                // Filter out incompatible prerelease versions
                if (IsPreReleaseVersion(latestFromApi) && !IsPreReleaseVersion(currentVersion))
                {
                    // Try to find a stable version instead
                    var stableVersion = await GetLatestStableVersionFromNuGetApi(packageName);
                    if (stableVersion is not null && stableVersion != currentVersion && IsNewerVersion(stableVersion, currentVersion))
                    {
                        return new VersionResolution(stableVersion);
                    }
                    return new VersionResolution(null);
                }
                return new VersionResolution(latestFromApi);
            }

            return new VersionResolution(null);
        }

        // Handle "Not found at the sources" case from outdated JSON
        if (latestFromOutdated == "Not found at the sources")
        {
            return new VersionResolution(null, true, $"Package '{packageName}' reported as not found at the sources");
        }

        // Filter out incompatible prerelease versions
        if (IsPreReleaseVersion(latestFromOutdated))
        {
            // Try to find a compatible stable version instead
            var stableVersion = await GetLatestStableVersionAsync(packageName);
            if (stableVersion is not null && stableVersion != currentVersion)
            {
                return new VersionResolution(stableVersion);
            }

            // If current version is also prerelease, allow the prerelease update
            if (IsPreReleaseVersion(currentVersion))
            {
                return new VersionResolution(latestFromOutdated);
            }

            // Otherwise, no update (don't upgrade stable to prerelease)
            return new VersionResolution(null);
        }

        return new VersionResolution(latestFromOutdated);
    }

    private static async Task<JsonDocument?> GetOutdatedPackagesJsonAsync()
    {
        var output = "";
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            output = ProcessHelper.StartProcess("dotnet list package --outdated --include-prerelease --format json", Configuration.ApplicationFolder, true, exitOnError: false, throwOnError: false);

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

    private static async Task<string?> GetLatestPreReleaseVersionAsync(string packageName)
    {
        var outdatedJson = await GetOutdatedPackagesJsonAsync();
        if (outdatedJson is null) return null;

        var latestFromOutdated = GetLatestVersionFromJson(outdatedJson, packageName);
        if (latestFromOutdated is not null && IsPreReleaseVersion(latestFromOutdated))
        {
            return latestFromOutdated;
        }

        return null;
    }

    private static async Task<string?> GetLatestStableVersionAsync(string packageName)
    {
        var outdatedJson = await GetOutdatedPackagesJsonAsync();
        if (outdatedJson is null) return null;

        var latestFromOutdated = GetLatestVersionFromJson(outdatedJson, packageName);
        if (latestFromOutdated is not null && !IsPreReleaseVersion(latestFromOutdated))
        {
            return latestFromOutdated;
        }

        return null;
    }

    private static bool IsPreReleaseVersion(string version)
    {
        return version.Contains("-") || version.Contains("alpha") || version.Contains("beta") || version.Contains("rc");
    }

    private static async Task<string?> GetLatestVersionFromNuGetApi(string packageName)
    {
        // Check cache first
        var cacheKey = $"{packageName}_latest";
        if (NuGetApiCache.TryGetValue(cacheKey, out var cachedVersion))
        {
            return cachedVersion;
        }

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
            var json = JsonDocument.Parse(response);
            var versions = json.RootElement.GetProperty("versions");

            // Get all versions including pre-release
            var allVersions = new List<string>();
            foreach (var version in versions.EnumerateArray())
            {
                var versionString = version.GetString();
                if (versionString is not null)
                {
                    allVersions.Add(versionString);
                }
            }

            // Return the latest version (last in the array as they're sorted)
            var latestVersion = allVersions.LastOrDefault();
            NuGetApiCache[cacheKey] = latestVersion;
            return latestVersion;
        }
        catch
        {
            NuGetApiCache[cacheKey] = null;
            return null;
        }
    }

    private static async Task<string?> GetLatestStableVersionFromNuGetApi(string packageName)
    {
        // Check cache first
        var cacheKey = $"{packageName}_stable";
        if (NuGetApiCache.TryGetValue(cacheKey, out var cachedVersion))
        {
            return cachedVersion;
        }

        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
            var json = JsonDocument.Parse(response);
            var versions = json.RootElement.GetProperty("versions");

            // Get only stable versions (no pre-release)
            var stableVersions = new List<string>();
            foreach (var version in versions.EnumerateArray())
            {
                var versionString = version.GetString();
                if (versionString is not null && !IsPreReleaseVersion(versionString))
                {
                    stableVersions.Add(versionString);
                }
            }

            // Return the latest stable version
            var latestStable = stableVersions.LastOrDefault();
            NuGetApiCache[cacheKey] = latestStable;
            return latestStable;
        }
        catch
        {
            NuGetApiCache[cacheKey] = null;
            return null;
        }
    }

    private static bool IsNewerVersion(string version1, string version2)
    {
        try
        {
            // Parse versions for comparison
            var v1Parts = version1.Split('-')[0].Split('.').Select(int.Parse).ToArray();
            var v2Parts = version2.Split('-')[0].Split('.').Select(int.Parse).ToArray();

            // Compare major, minor, patch
            for (var i = 0; i < Math.Max(v1Parts.Length, v2Parts.Length); i++)
            {
                var v1Part = i < v1Parts.Length ? v1Parts[i] : 0;
                var v2Part = i < v2Parts.Length ? v2Parts[i] : 0;

                if (v1Part > v2Part) return true;
                if (v1Part < v2Part) return false;
            }

            // If base versions are equal, check pre-release
            if (version1.Contains('-') && !version2.Contains('-'))
            {
                // v1 is pre-release, v2 is stable - v2 is considered newer
                return false;
            }
            if (!version1.Contains('-') && version2.Contains('-'))
            {
                // v1 is stable, v2 is pre-release - v1 is considered newer
                return true;
            }

            // Both have same base version, compare pre-release parts if any
            if (version1.Contains('-') && version2.Contains('-'))
            {
                return string.Compare(version1, version2, StringComparison.Ordinal) > 0;
            }

            return false;
        }
        catch
        {
            // Fallback to string comparison if parsing fails
            return string.Compare(version1, version2, StringComparison.Ordinal) > 0;
        }
    }

    private static void UpdateNpmPackages(bool dryRun, string[] excludedPackages)
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

            var statusColor = dryRun ? "[yellow]Will update[/]" : "[green]Updated[/]";
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

        if (updates.Count > 0 && !dryRun)
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
    
    private static void UpdateAspireSdkVersion(bool dryRun)
    {
        var appHostPath = Path.Combine(Configuration.ApplicationFolder, "AppHost", "AppHost.csproj");
        if (!File.Exists(appHostPath))
        {
            return;
        }

        try
        {
            var appHostXml = XDocument.Load(appHostPath);
            var sdkElement = appHostXml.Descendants("Sdk")
                .FirstOrDefault(e => e.Attribute("Name")?.Value == "Aspire.AppHost.Sdk");

            if (sdkElement is null)
            {
                return;
            }

            var currentSdkVersion = sdkElement.Attribute("Version")?.Value;
            if (currentSdkVersion is null)
            {
                return;
            }

            // Get the Aspire.Hosting.AppHost version from Directory.Packages.props
            var directoryPackagesPath = Path.Combine(Configuration.ApplicationFolder, "Directory.Packages.props");
            var packagesXml = XDocument.Load(directoryPackagesPath);
            var appHostPackageElement = packagesXml.Descendants("PackageVersion")
                .FirstOrDefault(e => e.Attribute("Include")?.Value == "Aspire.Hosting.AppHost");

            var targetSdkVersion = appHostPackageElement?.Attribute("Version")?.Value;
            if (targetSdkVersion is null || targetSdkVersion == currentSdkVersion)
            {
                return;
            }

            // Display SDK update information
            AnsiConsole.MarkupLine("\nAnalyzing Aspire SDK version...");
            var table = new Table();
            table.AddColumn("SDK");
            table.AddColumn("Current Version");
            table.AddColumn("Target Version");
            table.AddColumn("Status");

            var statusColor = dryRun ? "[yellow]Will update[/]" : "[green]Updated[/]";
            table.AddRow("Aspire.AppHost.Sdk", currentSdkVersion, targetSdkVersion, statusColor);
            AnsiConsole.Write(table);

            if (!dryRun)
            {
                sdkElement.SetAttributeValue("Version", targetSdkVersion);
                appHostXml.Save(appHostPath);
                AnsiConsole.MarkupLine($"[green]Updated Aspire.AppHost.Sdk from {currentSdkVersion} to {targetSdkVersion}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("[blue]Would update Aspire SDK version (dry-run mode)[/]");
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Could not update Aspire SDK version: {ex.Message}[/]");
        }
    }
}
