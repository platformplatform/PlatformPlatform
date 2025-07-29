using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public sealed class UpdatePackagesCommand : Command
{
    private static readonly string[] RestrictedNuGetPackages = ["MediatR", "FluentAssertions"];
    private static readonly Dictionary<string, string?> NuGetApiCache = new();
    private static readonly UpdateSummary BackendSummary = new();
    private static readonly UpdateSummary FrontendSummary = new();

    public UpdatePackagesCommand() : base("update-packages", "Updates packages to their latest versions while preserving major versions for restricted packages")
    {
        AddOption(new Option<bool>(["--backend", "-b"], "Update only backend packages (NuGet)"));
        AddOption(new Option<bool>(["--frontend", "-f"], "Update only frontend packages (npm)"));
        AddOption(new Option<bool>(["--dry-run", "-d"], "Show what would be updated without making changes"));
        AddOption(new Option<string?>(["--exclude", "-e"], "Comma-separated list of packages to exclude from updates"));
        AddOption(new Option<bool>(["--skip-update-dotnet"], "Skip updating .NET SDK version in global.json"));
        Handler = CommandHandler.Create<bool, bool, bool, string?, bool>(Execute);
    }

    private static async Task Execute(bool backend, bool frontend, bool dryRun, string? exclude, bool skipUpdateDotnet)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var excludedPackages = exclude?.Split(',').Select(p => p.Trim()).Where(p => p != "").ToArray() ?? [];
        if (excludedPackages.Length > 0)
        {
            AnsiConsole.MarkupLine($"[yellow]Excluding packages: {string.Join(", ", excludedPackages)}[/]");
            AnsiConsole.WriteLine();
        }

        if (dryRun)
        {
            AnsiConsole.MarkupLine("[blue]Running in dry-run mode - no changes will be made[/]");
            AnsiConsole.WriteLine();
        }


        var updateBackend = backend || !frontend;
        var updateFrontend = frontend || !backend;
        var updateDotnet = !skipUpdateDotnet && updateBackend;

        // Check .NET SDK version early if updating dotnet (default behavior)
        if (updateDotnet && !dryRun)
        {
            await CheckDotnetSdkVersionAsync(dryRun, true);
        }

        if (updateBackend)
        {
            await UpdateNuGetPackagesAsync(dryRun, excludedPackages);
            UpdateAspireSdkVersion(dryRun);
            await UpdateDotnetToolsAsync(dryRun);
        }

        if (updateFrontend)
        {
            UpdateNpmPackages(dryRun, excludedPackages);
        }

        // Display update summary
        DisplayUpdateSummary(updateBackend, updateFrontend);

        // Show .NET SDK info at the end for backend updates (unless explicitly skipped)
        if (updateDotnet)
        {
            AnsiConsole.WriteLine();
            await CheckDotnetSdkVersionAsync(dryRun, false);
        }
    }

    private static async Task UpdateNuGetPackagesAsync(bool dryRun, string[] excludedPackages)
    {
        var directoryPackagesPath = Path.Combine(Configuration.ApplicationFolder, "Directory.Packages.props");
        if (!File.Exists(directoryPackagesPath))
        {
            AnsiConsole.MarkupLine($"[red]Directory.Packages.props not found at {directoryPackagesPath}[/]");
            Environment.Exit(1);
        }

        AnsiConsole.MarkupLine("Analyzing NuGet packages in Directory.Packages.props...");

        var xDocument = XDocument.Load(directoryPackagesPath);
        var packageElements = xDocument.Descendants("PackageVersion").ToArray();

        var outdatedPackagesJson = await GetOutdatedPackagesJsonAsync();

        var table = new Table();
        table.AddColumn("Package");
        table.AddColumn("Current Version");
        table.AddColumn("Latest Version");
        table.AddColumn("Update Type");

        var packageUpdatesToApply = new List<PackageUpdate>();

        // First pass: collect all potential package updates
        var candidatePackageUpdates = new Dictionary<string, PackageUpdate>();

        foreach (var packageElement in packageElements)
        {
            var packageName = packageElement.Attribute("Include")?.Value;
            var currentVersion = packageElement.Attribute("Version")?.Value;
            if (packageName is null || currentVersion is null) continue;

            // Skip excluded packages
            if (IsPackageExcluded(packageName, excludedPackages))
            {
                table.AddRow(packageName, currentVersion, "-", "[blue]Excluded[/]");
                BackendSummary.Excluded++;
                continue;
            }

            var versionResolution = await ResolvePackageVersionAsync(outdatedPackagesJson, packageName, currentVersion);

            if (versionResolution.LatestVersion is null)
            {
                BackendSummary.UpToDate++;
                continue;
            }

            var status = GetNuGetUpdateStatus(packageName, currentVersion, versionResolution.LatestVersion!);

            // Collect all potential updates
            if (status.CanUpdate)
            {
                candidatePackageUpdates[packageName] = new PackageUpdate(packageElement, packageName, currentVersion, status.TargetVersion);
            }
            else if (status.IsRestricted)
            {
                // Show restricted packages in the table but don't count them as updates
                table.AddRow(packageName, currentVersion, versionResolution.LatestVersion!, "[red]Excluded[/]");
                BackendSummary.Excluded++;
            }
        }

        // Group packages that share the same current version - these likely belong to the same family
        // and should be updated together to maintain compatibility
        var packageFamiliesByVersion = packageElements
            .Select(p => new
                {
                    Name = p.Attribute("Include")?.Value,
                    Version = p.Attribute("Version")?.Value,
                    Element = p
                }
            )
            .Where(p => !string.IsNullOrEmpty(p.Name) && !string.IsNullOrEmpty(p.Version))
            .GroupBy(p => p.Version)
            .Where(g => g.Count() > 1) // Only consider groups with 2 or more packages
            .ToList();

        // For each version group, if any package is being updated, ensure all packages in that group
        // are updated to compatible versions
        foreach (var versionGroup in packageFamiliesByVersion)
        {
            var packagesInFamily = versionGroup.Select(p => p.Name!).ToList();

            // Check if any package in this version family is being updated
            var updatesInFamily = candidatePackageUpdates.Values
                .Where(u => packagesInFamily.Contains(u.PackageName))
                .ToList();

            if (!updatesInFamily.Any())
            {
                continue;
            }

            // Find the most common target version for this family
            var targetVersionForFamily = updatesInFamily
                .GroupBy(u => u.NewVersion)
                .OrderByDescending(g => g.Count())
                .First()
                .Key;

            // Update all packages in this version family to the same target version
            foreach (var packageInfo in versionGroup)
            {
                var packageName = packageInfo.Name!;

                if (candidatePackageUpdates.ContainsKey(packageName) ||
                    IsPackageExcluded(packageName, excludedPackages))
                {
                    continue;
                }

                // Check if this version exists in NuGet
                var versionExists = await CheckIfVersionExistsAsync(packageName, targetVersionForFamily);
                if (!versionExists || targetVersionForFamily == packageInfo.Version)
                {
                    continue;
                }

                candidatePackageUpdates[packageName] = new PackageUpdate(
                    packageInfo.Element,
                    packageName,
                    packageInfo.Version!,
                    targetVersionForFamily
                );
            }
        }

        // Check for package dependencies that should be updated together
        // When updating a package, we need to check its dependencies and update them if needed
        var packagesToCheckDependencies = new Queue<string>(candidatePackageUpdates.Keys);
        var checkedPackages = new HashSet<string>();

        while (packagesToCheckDependencies.Count > 0)
        {
            var packageName = packagesToCheckDependencies.Dequeue();
            if (checkedPackages.Contains(packageName)) continue;
            checkedPackages.Add(packageName);

            var update = candidatePackageUpdates[packageName];

            // Get package dependencies from NuGet
            var dependencies = await GetPackageDependenciesAsync(packageName, update.NewVersion);

            foreach (var dependency in dependencies)
            {
                // Check if this dependency is in our project
                var dependencyElement = packageElements.FirstOrDefault(p =>
                    p.Attribute("Include")?.Value == dependency.PackageName
                );

                if (dependencyElement is null) continue;

                var currentDependencyVersion = dependencyElement.Attribute("Version")?.Value;
                if (currentDependencyVersion is null) continue;

                // Skip if already being updated or excluded
                if (candidatePackageUpdates.ContainsKey(dependency.PackageName) ||
                    IsPackageExcluded(dependency.PackageName, excludedPackages))
                {
                    continue;
                }

                // Check if the dependency needs to be updated to satisfy the version requirement
                if (SatisfiesVersionRequirement(currentDependencyVersion, dependency.VersionRange))
                {
                    continue;
                }

                // Find the appropriate version to update to
                var targetVersion = await FindBestVersionForDependencyAsync(
                    dependency.PackageName,
                    dependency.VersionRange,
                    currentDependencyVersion
                );

                if (targetVersion is null || targetVersion == currentDependencyVersion)
                {
                    continue;
                }

                candidatePackageUpdates[dependency.PackageName] = new PackageUpdate(
                    dependencyElement,
                    dependency.PackageName,
                    currentDependencyVersion,
                    targetVersion
                );

                // Add this package to check its dependencies too
                packagesToCheckDependencies.Enqueue(dependency.PackageName);
            }
        }

        // Add all updates to the table and updates list
        foreach (var update in candidatePackageUpdates.Values.OrderBy(u => u.PackageName))
        {
            var updateType = GetUpdateType(update.CurrentVersion, update.NewVersion);
            BackendSummary.IncrementUpdateType(updateType);

            var statusColor = updateType switch
            {
                UpdateType.Major => "[yellow]Major[/]",
                UpdateType.Minor => "[green]Minor[/]",
                UpdateType.Patch => "Patch",
                _ => "[green]Minor[/]"
            };

            table.AddRow(update.PackageName, update.CurrentVersion, update.NewVersion, statusColor);
            packageUpdatesToApply.Add(update);
        }

        if (table.Rows.Count > 0)
        {
            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All NuGet packages are up to date![/]");
        }

        if (packageUpdatesToApply.Count > 0 && !dryRun)
        {
            foreach (var update in packageUpdatesToApply)
            {
                update.Element.SetAttributeValue("Version", update.NewVersion);
            }

            // Save without XML declaration to preserve original format
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false), // No BOM
                Async = true
            };

            await using (var writer = XmlWriter.Create(directoryPackagesPath, settings))
            {
                await xDocument.SaveAsync(writer, CancellationToken.None);
            }

            AnsiConsole.MarkupLine("[green]Directory.Packages.props updated successfully![/]");
        }
        else if (packageUpdatesToApply.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {packageUpdatesToApply.Count} NuGet package(s) (dry-run mode)[/]");
        }
    }

    private static async Task<VersionResolution> ResolvePackageVersionAsync(JsonDocument outdatedPackagesJson, string packageName, string currentVersion)
    {
        var latestFromOutdated = GetLatestVersionFromJson(outdatedPackagesJson, packageName);

        if (latestFromOutdated is not null)
        {
            // Filter out incompatible prerelease versions
            if (IsPreReleaseVersion(latestFromOutdated) && !IsPreReleaseVersion(currentVersion))
            {
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

        // Package not found in outdated JSON - check NuGet API directly
        var latestFromApi = await GetLatestVersionFromNuGetApi(packageName);
        if (latestFromApi is null)
        {
            return new VersionResolution(null);
        }

        // Compare versions to see if an update is available
        if (latestFromApi == currentVersion || !IsNewerVersion(latestFromApi, currentVersion))
        {
            return new VersionResolution(null);
        }

        // Filter out incompatible prerelease versions
        if (!IsPreReleaseVersion(latestFromApi) || IsPreReleaseVersion(currentVersion))
        {
            return new VersionResolution(latestFromApi);
        }

        // Try to find a stable version instead
        var stableVersion = await GetLatestStableVersionFromNuGetApi(packageName);
        if (stableVersion is not null && stableVersion != currentVersion && IsNewerVersion(stableVersion, currentVersion))
        {
            return new VersionResolution(stableVersion);
        }

        return new VersionResolution(null);
    }

    private static Task<JsonDocument> GetOutdatedPackagesJsonAsync()
    {
        var output = ProcessHelper.StartProcess("dotnet list package --outdated --include-prerelease --format json", Configuration.ApplicationFolder, true, exitOnError: false, throwOnError: false);

        if (string.IsNullOrEmpty(output))
        {
            AnsiConsole.MarkupLine("[red]Failed to get outdated packages information[/]");
            Environment.Exit(1);
        }

        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(output));
        if (!JsonDocument.TryParseValue(ref reader, out var jsonDocument))
        {
            AnsiConsole.MarkupLine($"[red]Invalid JSON output from dotnet list package command: {output}[/]");
            Environment.Exit(1);
        }

        return Task.FromResult(jsonDocument);
    }

    private static string? GetLatestVersionFromJson(JsonDocument jsonDocument, string packageName)
    {
        var projects = jsonDocument.RootElement.GetProperty("projects");

        foreach (var project in projects.EnumerateArray())
        {
            if (!project.TryGetProperty("frameworks", out var frameworks)) continue;

            foreach (var framework in frameworks.EnumerateArray())
            {
                if (!framework.TryGetProperty("topLevelPackages", out var packages)) continue;

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

    private static bool IsPreReleaseVersion(string version)
    {
        return version.Contains("-");
    }

    private static async Task<string?> GetLatestVersionFromNuGetApi(string packageName)
    {
        // Check cache first
        var cacheKey = $"{packageName}_latest";
        if (NuGetApiCache.TryGetValue(cacheKey, out var cachedVersion))
        {
            return cachedVersion;
        }

        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
        var json = JsonDocument.Parse(response);
        var versions = json.RootElement.GetProperty("versions");

        // Get all versions including pre-release
        var allVersions = versions.EnumerateArray()
            .Select(v => v.GetString()!)
            .ToList();

        // Return the latest version (last in the array as they're sorted)
        var latestVersion = allVersions.LastOrDefault();
        NuGetApiCache[cacheKey] = latestVersion;
        return latestVersion;
    }

    private static async Task<string?> GetLatestStableVersionFromNuGetApi(string packageName)
    {
        // Check cache first
        var cacheKey = $"{packageName}_stable";
        if (NuGetApiCache.TryGetValue(cacheKey, out var cachedVersion))
        {
            return cachedVersion;
        }

        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
        var json = JsonDocument.Parse(response);
        var versions = json.RootElement.GetProperty("versions");

        // Get only stable versions (no pre-release)
        var stableVersions = versions.EnumerateArray()
            .Select(v => v.GetString()!)
            .Where(v => !IsPreReleaseVersion(v))
            .ToList();

        // Return the latest stable version
        var latestStable = stableVersions.LastOrDefault();
        NuGetApiCache[cacheKey] = latestStable;
        return latestStable;
    }

    private static bool IsNewerVersion(string version1, string version2)
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

    private static async Task<List<PackageDependency>> GetPackageDependenciesAsync(string packageName, string version)
    {
        var dependencies = new List<PackageDependency>();

        try
        {
            using var httpClient = new HttpClient();
            var catalogUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/{version.ToLower()}/{packageName.ToLower()}.nuspec";
            var nuspecXml = await httpClient.GetStringAsync(catalogUrl);

            var doc = XDocument.Parse(nuspecXml);
            var ns = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var dependencyGroups = doc.Descendants(ns + "dependencies").Elements(ns + "group");

            // Get dependencies for the most compatible target framework
            // Prioritize modern .NET dependencies
            var relevantGroup = dependencyGroups
                .OrderByDescending(g =>
                    {
                        var framework = g.Attribute("targetFramework")?.Value ?? "";

                        // No target framework means it applies to all - highest priority
                        if (string.IsNullOrEmpty(framework)) return 10000;

                        // Parse .NET version (net9.0, net10.0, net11.0, etc.)
                        var netMatch = Regex.Match(framework, @"^net(\d+)\.?(\d*)");
                        if (netMatch.Success && int.TryParse(netMatch.Groups[1].Value, out var majorVersion))
                        {
                            // Prefer newer .NET versions (we only support .NET 9+)
                            return 1000 + majorVersion;
                        }

                        // .NET Standard versions
                        if (framework == ".NETStandard2.1") return 21;
                        if (framework == ".NETStandard2.0") return 20;
                        if (framework.StartsWith(".NETStandard")) return 10;

                        // Unknown frameworks
                        return 0;
                    }
                )
                .FirstOrDefault();

            if (relevantGroup is not null)
            {
                foreach (var dep in relevantGroup.Elements(ns + "dependency"))
                {
                    var depName = dep.Attribute("id")?.Value;
                    var depVersion = dep.Attribute("version")?.Value;

                    if (depName is not null && depVersion is not null)
                    {
                        dependencies.Add(new PackageDependency(depName, depVersion));
                    }
                }
            }
        }
        catch
        {
            // If we can't get dependencies, just return empty list
        }

        return dependencies;
    }

    private static bool SatisfiesVersionRequirement(string currentVersion, string versionRange)
    {
        // Simple version range parsing - handles common cases
        // Examples: [9.0.0, ), [9.0.0], 9.0.0, >= 9.0.0

        // Remove whitespace
        versionRange = versionRange.Trim();

        // Exact version match
        if (!versionRange.Contains('[') && !versionRange.Contains('(') && !versionRange.Contains('>') && !versionRange.Contains('<'))
        {
            return currentVersion == versionRange;
        }

        // Handle >= version
        if (versionRange.StartsWith(">="))
        {
            var minVersion = versionRange.Substring(2).Trim();
            return !IsNewerVersion(minVersion, currentVersion);
        }

        // Handle [version, ) - minimum version
        if (versionRange.StartsWith("[") && versionRange.EndsWith(")"))
        {
            var parts = versionRange.Trim('[', ')').Split(',');
            if (parts.Length < 1)
            {
                return true;
            }

            var minVersion = parts[0].Trim();
            return !IsNewerVersion(minVersion, currentVersion);
        }

        // Handle [version] - exact version
        if (versionRange.StartsWith("[") && versionRange.EndsWith("]"))
        {
            var exactVersion = versionRange.Trim('[', ']');
            return currentVersion == exactVersion;
        }

        // For other complex ranges, assume it's satisfied to avoid breaking things
        return true;
    }

    private static async Task<string?> FindBestVersionForDependencyAsync(string packageName, string versionRange, string currentVersion)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
            var json = JsonDocument.Parse(response);
            var versions = json.RootElement.GetProperty("versions");

            var availableVersions = versions.EnumerateArray()
                .Select(v => v.GetString()!)
                .Where(v => !IsPreReleaseVersion(v) || IsPreReleaseVersion(currentVersion))
                .ToList();

            // Parse the version range to find minimum required version
            string? minRequiredVersion = null;

            if (versionRange.StartsWith(">="))
            {
                minRequiredVersion = versionRange.Substring(2).Trim();
            }
            else if (versionRange.StartsWith("[") && versionRange.Contains(","))
            {
                var parts = versionRange.Trim('[', ')', ']').Split(',');
                minRequiredVersion = parts[0].Trim();
            }
            else if (versionRange.StartsWith("[") && versionRange.EndsWith("]"))
            {
                // Exact version requirement
                return versionRange.Trim('[', ']');
            }
            else if (!versionRange.Contains('[') && !versionRange.Contains('('))
            {
                // Simple version number
                minRequiredVersion = versionRange;
            }

            if (minRequiredVersion is null)
            {
                return null;
            }

            // Find versions that satisfy the requirement AND are newer than current version
            // We never want to downgrade packages
            var candidateVersions = availableVersions
                .Where(v => !IsNewerVersion(minRequiredVersion, v) && IsNewerVersion(v, currentVersion))
                .OrderBy(v => v.Split('.').Select(int.Parse).ToArray(), new VersionComparer())
                .ToList();

            if (candidateVersions.Any())
            {
                // Return the minimum version that satisfies the requirement and is newer than current
                return candidateVersions.First();
            }

            // If the current version already satisfies the requirement, keep it
            if (!IsNewerVersion(minRequiredVersion, currentVersion))
            {
                return null; // No update needed
            }

            // If we can't find a suitable version, return null
            return null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> CheckIfVersionExistsAsync(string packageName, string targetVersion)
    {
        try
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
            var json = JsonDocument.Parse(response);
            var versions = json.RootElement.GetProperty("versions");

            return versions.EnumerateArray()
                .Any(v => v.GetString() == targetVersion);
        }
        catch
        {
            return false;
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

        AnsiConsole.WriteLine();
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
        table.AddColumn("Update Type");

        var npmPackageUpdatesToApply = new List<string>();
        string? newBiomeVersion = null;

        foreach (var package in outdatedPackages.RootElement.EnumerateObject())
        {
            var packageName = package.Name;

            // Skip excluded packages
            if (IsPackageExcluded(packageName, excludedPackages))
            {
                if (package.Value.TryGetProperty("wanted", out var packageWantedElement))
                {
                    var packageWantedVersion = packageWantedElement.GetString() ?? "unknown";
                    table.AddRow(packageName, packageWantedVersion, "-", "[blue]Excluded[/]");
                    FrontendSummary.Excluded++;
                }

                continue;
            }

            var packageInfo = package.Value;

            if (!packageInfo.TryGetProperty("current", out var currentElement) ||
                !packageInfo.TryGetProperty("wanted", out var wantedElement) ||
                !packageInfo.TryGetProperty("latest", out var latestElement))
            {
                continue;
            }

            var currentVersion = currentElement.GetString();
            var wantedVersion = wantedElement.GetString();
            var latestVersion = latestElement.GetString();

            if (currentVersion is null || wantedVersion is null || latestVersion is null) continue;

            // Use wanted version (from package.json) as the base for comparison
            // Skip packages that are already at the latest version
            if (wantedVersion == latestVersion) continue;

            // Check update type based on what's in package.json (wanted) vs latest
            var updateType = GetUpdateType(wantedVersion, latestVersion);
            FrontendSummary.IncrementUpdateType(updateType);

            var statusColor = updateType switch
            {
                UpdateType.Major => "[yellow]Major[/]",
                UpdateType.Minor => "[green]Minor[/]",
                UpdateType.Patch => "Patch",
                _ => "[green]Minor[/]"
            };

            // Show wanted version (from package.json) in the table
            table.AddRow(packageName, wantedVersion, latestVersion, statusColor);
            npmPackageUpdatesToApply.Add($"{packageName}@{latestVersion}");

            // Track Biome version for schema update
            if (packageName == "@biomejs/biome")
            {
                newBiomeVersion = latestVersion;
            }
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

        if (npmPackageUpdatesToApply.Count > 0 && !dryRun)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("[blue]Updating packages...[/]");

            // Use --save-exact to install exact versions without ^ prefix
            var updateCommand = $"npm install --save-exact {string.Join(" ", npmPackageUpdatesToApply)}";
            ProcessHelper.StartProcess(updateCommand, Configuration.ApplicationFolder);
            AnsiConsole.MarkupLine("[green]npm packages updated successfully![/]");

            // Update Biome schema version if Biome was updated
            UpdateBiomeSchemaVersion(dryRun, newBiomeVersion);
        }
        else if (npmPackageUpdatesToApply.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {npmPackageUpdatesToApply.Count} npm package(s) (dry-run mode)[/]");

            // Check if Biome schema would be updated
            UpdateBiomeSchemaVersion(dryRun, newBiomeVersion);
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
        // If target version is the same as current, don't update
        if (targetVersion == currentVersion)
        {
            return new UpdateStatus(false, true, currentVersion);
        }

        return new UpdateStatus(targetVersion is not null, true, targetVersion ?? currentVersion);
    }

    private static async Task<string?> FindLatestVersionInMajor(string packageName, int majorVersion)
    {
        using var httpClient = new HttpClient();
        var response = await httpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
        var json = JsonDocument.Parse(response);
        var versions = json.RootElement.GetProperty("versions");

        var matchingVersions = new List<string>();
        foreach (var version in versions.EnumerateArray())
        {
            var versionString = version.GetString()!;
            if (!versionString.Contains("-") && GetMajorVersion(versionString) == majorVersion)
            {
                matchingVersions.Add(versionString);
            }
        }

        return matchingVersions.LastOrDefault();
    }

    private static int GetMajorVersion(string version)
    {
        return new Version(version).Major;
    }

    private static void ValidateBackend()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Running backend validation...[/]");

        // Run pp build --backend
        AnsiConsole.MarkupLine("[dim]Running: pp build --backend[/]");
        ProcessHelper.StartProcess("pp build --backend", Configuration.SourceCodeFolder);

        // Run pp test
        AnsiConsole.MarkupLine("[dim]Running: pp test[/]");
        ProcessHelper.StartProcess("pp test", Configuration.SourceCodeFolder);

        AnsiConsole.MarkupLine("[green]✓ Backend validation completed successfully![/]");
    }

    private static void ValidateFrontend()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]Running frontend validation...[/]");

        // Run pp build --frontend
        AnsiConsole.MarkupLine("[dim]Running: pp build --frontend[/]");
        ProcessHelper.StartProcess("pp build --frontend", Configuration.SourceCodeFolder);

        AnsiConsole.MarkupLine("[green]✓ Frontend validation completed successfully![/]");
    }

    private static void UpdateAspireSdkVersion(bool dryRun)
    {
        var appHostPath = Path.Combine(Configuration.ApplicationFolder, "AppHost", "AppHost.csproj");
        if (!File.Exists(appHostPath)) return;

        // Read file content to check for SDK
        var fileContent = File.ReadAllText(appHostPath);

        // Use regex to find the Aspire SDK version
        var sdkVersionMatch = Regex.Match(fileContent, @"<Sdk\s+Name=""Aspire\.AppHost\.Sdk""\s+Version=""([^""]+)""\s*/>");
        if (!sdkVersionMatch.Success) return;

        var currentSdkVersion = sdkVersionMatch.Groups[1].Value;

        // Get the Aspire.Hosting.AppHost version from Directory.Packages.props
        var directoryPackagesPath = Path.Combine(Configuration.ApplicationFolder, "Directory.Packages.props");
        var packagesXml = XDocument.Load(directoryPackagesPath);
        var appHostPackageElement = packagesXml.Descendants("PackageVersion")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == "Aspire.Hosting.AppHost");

        var targetSdkVersion = appHostPackageElement?.Attribute("Version")?.Value;
        if (targetSdkVersion is null || targetSdkVersion == currentSdkVersion) return;

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
            // Replace only the version string, preserving formatting
            var updatedContent = fileContent.Replace(
                $@"<Sdk Name=""Aspire.AppHost.Sdk"" Version=""{currentSdkVersion}"" />",
                $@"<Sdk Name=""Aspire.AppHost.Sdk"" Version=""{targetSdkVersion}"" />"
            );

            // Write back preserving original formatting
            File.WriteAllText(appHostPath, updatedContent);
            AnsiConsole.MarkupLine($"[green]Updated Aspire.AppHost.Sdk from {currentSdkVersion} to {targetSdkVersion}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[blue]Would update Aspire SDK version (dry-run mode)[/]");
        }
    }

    private static void UpdateBiomeSchemaVersion(bool dryRun, string? newBiomeVersion)
    {
        if (newBiomeVersion is null) return;

        var biomeJsonPath = Path.Combine(Configuration.ApplicationFolder, "biome.json");
        if (!File.Exists(biomeJsonPath)) return;

        // Read file content to check for schema
        var fileContent = File.ReadAllText(biomeJsonPath);

        // Use regex to find the Biome schema version
        var schemaVersionMatch = Regex.Match(fileContent, @"""?\$schema""?\s*:\s*""https://biomejs\.dev/schemas/([^/]+)/schema\.json""");
        if (!schemaVersionMatch.Success) return;

        var currentSchemaVersion = schemaVersionMatch.Groups[1].Value;

        // Check if the schema version needs updating
        if (currentSchemaVersion == newBiomeVersion) return;

        var table = new Table();
        table.AddColumn("Schema");
        table.AddColumn("Current Version");
        table.AddColumn("Target Version");
        table.AddColumn("Status");

        var statusColor = dryRun ? "[yellow]Will update[/]" : "[green]Updated[/]";
        table.AddRow("biome.json $schema", currentSchemaVersion, newBiomeVersion, statusColor);
        AnsiConsole.Write(table);

        if (dryRun)
        {
            AnsiConsole.MarkupLine("[blue]Would update Biome schema version (dry-run mode)[/]");
        }
        else
        {
            // Replace only the version string in the schema URL, preserving formatting
            var updatedContent = fileContent.Replace(
                $"https://biomejs.dev/schemas/{currentSchemaVersion}/schema.json",
                $"https://biomejs.dev/schemas/{newBiomeVersion}/schema.json"
            );

            // Write back preserving original formatting
            File.WriteAllText(biomeJsonPath, updatedContent);
            AnsiConsole.MarkupLine($"[green]Updated Biome schema version from {currentSchemaVersion} to {newBiomeVersion}[/]");
        }
    }

    private static async Task UpdateDotnetToolsAsync(bool dryRun)
    {
        var dotnetToolsPath = Path.Combine(Configuration.ApplicationFolder, "dotnet-tools.json");
        if (!File.Exists(dotnetToolsPath)) return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("Analyzing .NET tools in dotnet-tools.json...");

        var toolsJson = await File.ReadAllTextAsync(dotnetToolsPath);
        var toolsDocument = JsonDocument.Parse(toolsJson);

        var table = new Table();
        table.AddColumn("Tool");
        table.AddColumn("Current Version");
        table.AddColumn("Latest Version");
        table.AddColumn("Update Type");

        var dotnetToolUpdatesToApply = new List<(string toolName, string currentVersion, string latestVersion)>();

        if (!toolsDocument.RootElement.TryGetProperty("tools", out var tools))
        {
            return;
        }

        foreach (var tool in tools.EnumerateObject())
        {
            var toolName = tool.Name;

            if (!tool.Value.TryGetProperty("version", out var versionElement))
            {
                continue;
            }

            var currentVersion = versionElement.GetString();
            if (currentVersion is null)
            {
                continue;
            }

            // Get latest version from NuGet API
            var latestVersion = await GetLatestVersionFromNuGetApi(toolName);

            if (latestVersion is null || latestVersion == currentVersion || !IsNewerVersion(latestVersion, currentVersion))
            {
                continue;
            }

            // Handle prerelease versions
            if (IsPreReleaseVersion(latestVersion) && !IsPreReleaseVersion(currentVersion))
            {
                // Try to find a stable version
                var stableVersion = await GetLatestStableVersionFromNuGetApi(toolName);
                if (stableVersion is null || stableVersion == currentVersion || !IsNewerVersion(stableVersion, currentVersion))
                {
                    continue; // Skip if no stable update available
                }

                latestVersion = stableVersion;
            }

            // Check update type
            var updateType = GetUpdateType(currentVersion, latestVersion);
            BackendSummary.IncrementUpdateType(updateType);

            var statusColor = updateType switch
            {
                UpdateType.Major => "[yellow]Major[/]",
                UpdateType.Minor => "[green]Minor[/]",
                UpdateType.Patch => "Patch",
                _ => "[green]Minor[/]"
            };

            table.AddRow(toolName, currentVersion, latestVersion, statusColor);
            dotnetToolUpdatesToApply.Add((toolName, currentVersion, latestVersion));
        }

        if (table.Rows.Count > 0)
        {
            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All .NET tools are up to date![/]");
            return;
        }

        if (dotnetToolUpdatesToApply.Count > 0 && !dryRun)
        {
            // Parse and update the JSON
            using var jsonDoc = JsonDocument.Parse(toolsJson);
            using var stream = new MemoryStream();
            await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                foreach (var property in jsonDoc.RootElement.EnumerateObject())
                {
                    if (property.Name == "tools")
                    {
                        writer.WritePropertyName("tools");
                        writer.WriteStartObject();

                        foreach (var tool in property.Value.EnumerateObject())
                        {
                            writer.WritePropertyName(tool.Name);
                            writer.WriteStartObject();

                            var updateInfo = dotnetToolUpdatesToApply.FirstOrDefault(u => u.toolName == tool.Name);

                            foreach (var toolProperty in tool.Value.EnumerateObject())
                            {
                                if (toolProperty.Name == "version" && updateInfo != default)
                                {
                                    writer.WriteString("version", updateInfo.latestVersion);
                                }
                                else
                                {
                                    toolProperty.WriteTo(writer);
                                }
                            }

                            writer.WriteEndObject();
                        }

                        writer.WriteEndObject();
                    }
                    else
                    {
                        property.WriteTo(writer);
                    }
                }

                writer.WriteEndObject();
            }

            var updatedJson = Encoding.UTF8.GetString(stream.ToArray());
            await File.WriteAllTextAsync(dotnetToolsPath, updatedJson);
            AnsiConsole.MarkupLine("[green]dotnet-tools.json updated successfully![/]");
        }
        else if (dotnetToolUpdatesToApply.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {dotnetToolUpdatesToApply.Count} .NET tool(s) (dry-run mode)[/]");
        }
    }

    private static async Task CheckDotnetSdkVersionAsync(bool dryRun, bool earlyCheck)
    {
        var globalJsonPath = Path.Combine(Configuration.ApplicationFolder, "global.json");
        var globalJson = await File.ReadAllTextAsync(globalJsonPath);
        var globalJsonDoc = JsonDocument.Parse(globalJson);
        var currentVersion = globalJsonDoc.RootElement.GetProperty("sdk").GetProperty("version").GetString()!;

        // Get latest .NET SDK version from the official releases
        var currentMajor = GetMajorVersion(currentVersion);
        var latestInMajor = await GetLatestDotnetSdkVersion(currentMajor);

        if (latestInMajor == currentVersion)
        {
            if (!earlyCheck)
            {
                AnsiConsole.MarkupLine("[green]✓ .NET SDK version is already up to date[/]");
            }

            return;
        }

        // Check if the latest version is installed locally
        var isInstalledLocally = IsDotnetSdkInstalledLocally(latestInMajor);

        // Early check - only care about blocking if SDK not installed
        if (earlyCheck)
        {
            if (isInstalledLocally) return; // If installed, we'll update it after other updates

            AnsiConsole.MarkupLine($"""
                                    [red]❌ Cannot update .NET SDK: version {latestInMajor} is not installed locally![/]
                                    [yellow]   Install it first: brew upgrade dotnet-sdk (macOS) or winget upgrade Microsoft.DotNet.SDK.{currentMajor} (Windows)[/]
                                    """
            );
            Environment.Exit(1);
        }

        AnsiConsole.MarkupLine($"[blue]A newer .NET SDK version is available: {latestInMajor} (current: {currentVersion})[/]");

        // Late check - show status information
        if (!isInstalledLocally)
        {
            AnsiConsole.MarkupLine(
                $"""
                 [red]   ⚠️  .NET SDK {latestInMajor} is NOT installed on your machine![/]
                 [yellow]   Update .NET: brew upgrade dotnet-sdk (macOS) or winget upgrade Microsoft.DotNet.SDK.{currentMajor} (Windows)[/]
                 """
            );
        }

        // Actually update .NET SDK if not in dry-run mode and SDK is installed
        if (!dryRun && isInstalledLocally)
        {
            // Update global.json
            using var stream = new MemoryStream();
            await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                foreach (var property in globalJsonDoc.RootElement.EnumerateObject())
                {
                    if (property.Name != "sdk")
                    {
                        property.WriteTo(writer);
                        continue;
                    }

                    writer.WritePropertyName("sdk");
                    writer.WriteStartObject();

                    foreach (var sdkProperty in property.Value.EnumerateObject())
                    {
                        if (sdkProperty.Name == "version")
                        {
                            writer.WriteString("version", latestInMajor);
                        }
                        else
                        {
                            sdkProperty.WriteTo(writer);
                        }
                    }

                    writer.WriteEndObject();
                }

                writer.WriteEndObject();
            }

            var updatedJson = Encoding.UTF8.GetString(stream.ToArray());
            await File.WriteAllTextAsync(globalJsonPath, updatedJson);
            AnsiConsole.MarkupLine($"\n[green]✓ Updated .NET SDK version from {currentVersion} to {latestInMajor} in global.json[/]");

            // Also update Prerequisite.cs
            await UpdatePrerequisiteDotnetVersion(latestInMajor);
        }
    }

    private static bool IsDotnetSdkInstalledLocally(string version)
    {
        var output = ProcessHelper.StartProcess("dotnet --list-sdks", redirectOutput: true);

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Regex.Match(line, @"^(\d+\.\d+\.\d+)");
            if (match.Success && match.Groups[1].Value == version)
            {
                return true;
            }
        }

        return false;
    }

    private static async Task<string> GetLatestDotnetSdkVersion(int majorVersion)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PlatformPlatform-CLI/1.0");

        // Get the releases index
        const string dotnetReleaseBaseUrl = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata";
        var response = await httpClient.GetStringAsync($"{dotnetReleaseBaseUrl}/releases-index.json");
        var releasesIndex = JsonDocument.Parse(response);

        // Find the channel for the major version
        var channelVersion = releasesIndex.RootElement
            .GetProperty("releases-index")
            .EnumerateArray()
            .Select(release => release.GetProperty("channel-version").GetString()!)
            .First(version => version.StartsWith($"{majorVersion}."));

        // Get the channel releases
        var channelUrl = $"{dotnetReleaseBaseUrl}/{channelVersion}/releases.json";
        var channelResponse = await httpClient.GetStringAsync(channelUrl);
        var channelData = JsonDocument.Parse(channelResponse);

        // Find the latest SDK version - the first release is always the latest
        var latestRelease = channelData.RootElement.GetProperty("releases").EnumerateArray().First();
        return latestRelease.GetProperty("sdk").GetProperty("version").GetString()!;
    }

    private static async Task UpdatePrerequisiteDotnetVersion(string newVersion)
    {
        var prerequisitePath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli", "Installation", "Prerequisite.cs");
        if (!File.Exists(prerequisitePath))
        {
            AnsiConsole.MarkupLine("[red]Could not find Prerequisite.cs to update[/]");
            Environment.Exit(1);
        }

        var content = await File.ReadAllTextAsync(prerequisitePath);
        var versionParts = newVersion.Split('.');

        // Create the new version string for the Prerequisite file
        var newVersionString = $"new Version({versionParts[0]}, {versionParts[1]}, {versionParts[2]})";

        // Replace the Dotnet prerequisite line
        var pattern = @"public static readonly Prerequisite Dotnet = new CommandLineToolPrerequisite\(""dotnet"", ""dotnet"", new Version\(\d+, \d+, \d+\)\);";
        var replacement = $"public static readonly Prerequisite Dotnet = new CommandLineToolPrerequisite(\"dotnet\", \"dotnet\", {newVersionString});";

        var updatedContent = Regex.Replace(content, pattern, replacement);

        if (updatedContent != content)
        {
            await File.WriteAllTextAsync(prerequisitePath, updatedContent);
            AnsiConsole.MarkupLine($"[green]✓ Updated Prerequisite.cs to require .NET {newVersion}[/]");
        }
    }

    private static bool IsPackageExcluded(string packageName, string[] excludePatterns)
    {
        foreach (var pattern in excludePatterns)
        {
            // Check for exact match
            if (pattern == packageName) return true;

            // Check for wildcard patterns
            if (pattern.Contains('*'))
            {
                var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
                if (Regex.IsMatch(packageName, regexPattern)) return true;
            }
        }

        return false;
    }

    private static UpdateType GetUpdateType(string currentVersion, string newVersion)
    {
        var currentParts = currentVersion.Split('-')[0].Split('.').Select(int.Parse).ToArray();
        var newParts = newVersion.Split('-')[0].Split('.').Select(int.Parse).ToArray();

        // Major version changed
        if (currentParts.Length > 0 && newParts.Length > 0 && currentParts[0] != newParts[0])
        {
            return UpdateType.Major;
        }

        // Minor version changed
        if (currentParts.Length > 1 && newParts.Length > 1 && currentParts[1] != newParts[1])
        {
            return UpdateType.Minor;
        }

        // Only patch version changed
        return UpdateType.Patch;
    }

    private static void DisplayUpdateSummary(bool showBackend, bool showFrontend)
    {
        var hasUpdates = (showBackend && BackendSummary.TotalUpdates > 0) ||
                         (showFrontend && FrontendSummary.TotalUpdates > 0);
        var hasExcluded = (showBackend && BackendSummary.Excluded > 0) ||
                          (showFrontend && FrontendSummary.Excluded > 0);
        var hasUpToDate = (showBackend && BackendSummary.UpToDate > 0) ||
                          (showFrontend && FrontendSummary.UpToDate > 0);

        if (!hasUpdates && !hasExcluded && !hasUpToDate) return;

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("📊 Update Summary:");

        var table = new Table();
        table.AddColumn("");
        table.AddColumn(new TableColumn("Backend").Centered());
        table.AddColumn(new TableColumn("Frontend").Centered());

        // Patch updates
        table.AddRow(
            "🔧 Patch",
            showBackend ? BackendSummary.Patch.ToString() : "-",
            showFrontend ? FrontendSummary.Patch.ToString() : "-"
        );

        // Minor updates
        table.AddRow(
            "[green]📦 Minor[/]",
            showBackend ? $"[green]{BackendSummary.Minor}[/]" : "-",
            showFrontend ? $"[green]{FrontendSummary.Minor}[/]" : "-"
        );

        // Major upgrades
        table.AddRow(
            "[yellow]⚠️  Major[/]",
            showBackend ? BackendSummary.Major > 0 ? $"[yellow]{BackendSummary.Major}[/]" : "0" : "-",
            showFrontend ? FrontendSummary.Major > 0 ? $"[yellow]{FrontendSummary.Major}[/]" : "0" : "-"
        );

        // Excluded
        if (hasExcluded)
        {
            table.AddRow(
                "[red]🚫 Excluded[/]",
                showBackend ? BackendSummary.Excluded > 0 ? $"[red]{BackendSummary.Excluded}[/]" : "0" : "-",
                showFrontend ? FrontendSummary.Excluded > 0 ? $"[red]{FrontendSummary.Excluded}[/]" : "0" : "-"
            );
        }

        // Up to date
        if (hasUpToDate)
        {
            table.AddRow(
                "[dim]✓ Up to date[/]",
                showBackend ? $"[dim]{BackendSummary.UpToDate}[/]" : "-",
                showFrontend ? $"[dim]{FrontendSummary.UpToDate}[/]" : "-"
            );
        }

        AnsiConsole.Write(table);
    }

    private class VersionComparer : IComparer<int[]>
    {
        public int Compare(int[]? x, int[]? y)
        {
            if (x is null || y is null) return 0;

            for (var i = 0; i < Math.Max(x.Length, y.Length); i++)
            {
                var xPart = i < x.Length ? x[i] : 0;
                var yPart = i < y.Length ? y[i] : 0;

                if (xPart < yPart) return -1;
                if (xPart > yPart) return 1;
            }

            return 0;
        }
    }

    private record PackageDependency(string PackageName, string VersionRange);

    private enum UpdateType
    {
        Patch,
        Minor,
        Major
    }

    private sealed class UpdateSummary
    {
        public int Patch { get; private set; }

        public int Minor { get; private set; }

        public int Major { get; private set; }

        public int Excluded { get; set; }

        public int UpToDate { get; set; }

        public int TotalUpdates => Patch + Minor + Major;

        public void IncrementUpdateType(UpdateType type)
        {
            switch (type)
            {
                case UpdateType.Patch:
                    Patch++;
                    break;
                case UpdateType.Minor:
                    Minor++;
                    break;
                case UpdateType.Major:
                    Major++;
                    break;
            }
        }
    }

    private sealed record PackageUpdate(XElement Element, string PackageName, string CurrentVersion, string NewVersion);

    private sealed record UpdateStatus(bool CanUpdate, bool IsRestricted, string TargetVersion);

    private sealed record VersionResolution(string? LatestVersion);
}
