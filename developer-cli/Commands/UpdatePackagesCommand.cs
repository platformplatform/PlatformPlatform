using System.CommandLine;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public sealed class UpdatePackagesCommand : Command
{
    // Packages kept on their current major version. A new major requires code changes that must be
    // handled in a dedicated upgrade, so these are never auto-bumped across a major boundary:
    // - MediatR and FluentAssertions changed their licensing/APIs in later majors.
    // - Microsoft.ApplicationInsights drops PageView tracking (used heavily) when moving to OpenTelemetry.
    private static readonly string[] RestrictedNuGetPackages =
        ["MediatR", "FluentAssertions", "Microsoft.ApplicationInsights", "Microsoft.ApplicationInsights.AspNetCore"];

    private static readonly Dictionary<string, string?> NuGetApiCache = new();
    private static readonly UpdateSummary BackendSummary = new();
    private static readonly UpdateSummary FrontendSummary = new();
    private static readonly HttpClient HttpClient = CreateHttpClient();

    // When true, decorative output (tables, banners, progress chatter) is replaced with a terse,
    // machine-parseable plain-text report so the command can be driven from scripts and skills.
    private static bool _quietMode;

    public UpdatePackagesCommand() : base("update-packages", "Updates packages to their latest versions while preserving major versions for restricted packages")
    {
        var backendOption = new Option<bool>("--backend", "-b") { Description = "Update only backend packages (NuGet)" };
        var frontendOption = new Option<bool>("--frontend", "-f") { Description = "Update only frontend packages (npm)" };
        var dryRunOption = new Option<bool>("--dry-run", "-d") { Description = "Show what would be updated without making changes" };
        var excludeOption = new Option<string?>("--exclude", "-e") { Description = "Comma-separated list of packages to exclude from updates" };
        var includeMajorFrameworkUpdatesOption = new Option<bool>("--include-major-framework-updates") { Description = "Allow updating .NET and Node.js to new major versions (default: only update within current major)" };
        var quietOption = new Option<bool>("--quiet", "-q") { Description = "Terse, machine-parseable output for scripting (no tables or banners)" };

        Options.Add(backendOption);
        Options.Add(frontendOption);
        Options.Add(dryRunOption);
        Options.Add(excludeOption);
        Options.Add(includeMajorFrameworkUpdatesOption);
        Options.Add(quietOption);

        SetAction(async parseResult => await Execute(
                parseResult.GetValue(backendOption),
                parseResult.GetValue(frontendOption),
                parseResult.GetValue(dryRunOption),
                parseResult.GetValue(excludeOption),
                parseResult.GetValue(includeMajorFrameworkUpdatesOption),
                parseResult.GetValue(quietOption)
            )
        );
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PlatformPlatform-CLI/1.0");
        return client;
    }

    private static async Task Execute(bool backend, bool frontend, bool dryRun, string? exclude, bool includeMajorFrameworkUpdates, bool quiet)
    {
        _quietMode = quiet;

        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var excludedPackages = exclude?.Split(',').Select(p => p.Trim()).Where(p => p != "").ToList() ?? [];

        if (excludedPackages.Count > 0 && !_quietMode)
        {
            AnsiConsole.MarkupLine($"[yellow]Excluding packages: {string.Join(", ", excludedPackages)}[/]");
            AnsiConsole.WriteLine();
        }

        var excludedPackagesArray = excludedPackages.ToArray();

        if (dryRun && !_quietMode)
        {
            AnsiConsole.MarkupLine("[blue]Running in dry-run mode - no changes will be made[/]");
            AnsiConsole.WriteLine();
        }


        var updateBackend = backend || !frontend;
        var updateFrontend = frontend || !backend;

        // Check .NET SDK version early if updating backend (default behavior)
        if (updateBackend && !dryRun)
        {
            await CheckDotnetSdkVersionAsync(dryRun, true, includeMajorFrameworkUpdates);
        }

        if (updateBackend)
        {
            // Update central package management files
            foreach (var propsFile in Directory.GetFiles(Configuration.SourceCodeFolder, "Directory.Packages.props", SearchOption.AllDirectories))
            {
                await UpdateNuGetPackagesAsync(propsFile, "PackageVersion", dryRun, excludedPackagesArray);
            }

            // Update .csproj files that have inline PackageReference versions (not using central package management)
            foreach (var csprojFile in Directory.GetFiles(Configuration.SourceCodeFolder, "*.csproj", SearchOption.AllDirectories))
            {
                await UpdateNuGetPackagesAsync(csprojFile, "PackageReference", dryRun, excludedPackagesArray, true);
            }

            UpdateAspireSdkVersion(dryRun);
            Prerequisite.WarnIfAspireCliDoesNotMatchSdk();
            await UpdateDotnetToolsAsync(dryRun);
        }

        if (updateFrontend)
        {
            UpdateNpmPackages(dryRun, excludedPackagesArray, includeMajorFrameworkUpdates);
        }

        // Display update summary
        DisplayUpdateSummary(updateBackend, updateFrontend);

        // Show .NET SDK info at the end for backend updates
        if (updateBackend)
        {
            AnsiConsole.WriteLine();
            await CheckDotnetSdkVersionAsync(dryRun, false, includeMajorFrameworkUpdates);
        }

        // Sync .node-version with Prerequisite.cs if updating frontend
        if (updateFrontend && !dryRun)
        {
            await SyncNodeVersionFile();
        }
    }

    private static async Task UpdateNuGetPackagesAsync(string filePath, string elementName, bool dryRun, string[] excludedPackages, bool requireVersionAttribute = false)
    {
        if (!File.Exists(filePath)) return;

        var xDocument = XDocument.Load(filePath);
        var packageElements = xDocument.Descendants(elementName).ToArray();

        // Skip files that don't have any packages with Version attributes (e.g., csproj files using central package management)
        if (requireVersionAttribute && !packageElements.Any(e => e.Attribute("Version") is not null))
        {
            return;
        }

        var fileName = Path.GetFileName(filePath);
        if (!_quietMode) AnsiConsole.MarkupLine($"Analyzing NuGet packages in {fileName}...");

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
                if (_quietMode) Console.WriteLine($"backend excluded {packageName} {currentVersion}");
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
                if (_quietMode) Console.WriteLine($"backend restricted {packageName} {currentVersion} (latest {versionResolution.LatestVersion} is a new major, pinned)");
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
            if (!checkedPackages.Add(packageName)) continue;

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
            if (_quietMode) Console.WriteLine($"backend {updateType.ToString().ToLowerInvariant()} {update.PackageName} {update.CurrentVersion} -> {update.NewVersion}");
            packageUpdatesToApply.Add(update);
        }

        if (!_quietMode)
        {
            if (table.Rows.Count > 0)
            {
                AnsiConsole.Write(table);
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]All {fileName} NuGet packages are up to date![/]");
            }
        }

        if (packageUpdatesToApply.Count > 0 && !dryRun)
        {
            foreach (var update in packageUpdatesToApply)
            {
                update.Element.SetAttributeValue("Version", update.NewVersion);
            }

            // Determine indent chars from the existing file
            var existingIndent = xDocument.ToString().Contains("\n    ") ? "    " : "  ";

            // Save without XML declaration to preserve original format
            var settings = new XmlWriterSettings
            {
                OmitXmlDeclaration = true,
                Indent = true,
                IndentChars = existingIndent,
                Encoding = new UTF8Encoding(false), // No BOM
                Async = true
            };

            await using (var writer = XmlWriter.Create(filePath, settings))
            {
                await xDocument.SaveAsync(writer, CancellationToken.None);
            }

            if (!_quietMode) AnsiConsole.MarkupLine($"[green]{fileName} updated successfully![/]");
        }
        else if (packageUpdatesToApply.Count > 0 && !_quietMode)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {packageUpdatesToApply.Count} {fileName} NuGet package(s) (dry-run mode)[/]");
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
        // `dotnet list package --outdated` runs once per project/props file. After the first file
        // (Directory.Packages.props) is rewritten with new versions, a later call restores against a
        // now-stale state and can return JSON containing only `problems` with no `projects` array.
        // Treat a missing `projects` key as "not found" so the caller falls back to the NuGet API.
        if (!jsonDocument.RootElement.TryGetProperty("projects", out var projects))
        {
            return null;
        }

        foreach (var project in projects.EnumerateArray())
        {
            if (!project.TryGetProperty("frameworks", out var frameworks)) continue;

            foreach (var framework in frameworks.EnumerateArray())
            {
                if (!framework.TryGetProperty("topLevelPackages", out var packages)) continue;

                foreach (var package in packages.EnumerateArray())
                {
                    if (package.TryGetProperty("id", out var id) &&
                        id.GetString() == packageName &&
                        package.TryGetProperty("latestVersion", out var latestVersion))
                    {
                        return latestVersion.GetString();
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

        var response = await HttpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
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

        var response = await HttpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
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
            var catalogUrl = $"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/{version.ToLower()}/{packageName.ToLower()}.nuspec";
            var nuspecXml = await HttpClient.GetStringAsync(catalogUrl);

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
            var response = await HttpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
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
            var response = await HttpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
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

    private static void UpdateNpmPackages(bool dryRun, string[] excludedPackages, bool includeMajorFrameworkUpdates)
    {
        var packageJsonPath = Path.Combine(Configuration.ApplicationFolder, "package.json");

        if (!File.Exists(packageJsonPath))
        {
            AnsiConsole.MarkupLine($"[red]package.json not found at {packageJsonPath}[/]");
            return;
        }

        if (!_quietMode)
        {
            AnsiConsole.WriteLine();
            AnsiConsole.MarkupLine("Analyzing npm packages in package.json...");
        }

        var workspaceMap = BuildNpmWorkspaceMap(packageJsonPath);
        var output = ProcessHelper.StartProcess("npm outdated --json", Configuration.ApplicationFolder, true, exitOnError: false, throwOnError: false);

        if (string.IsNullOrWhiteSpace(output))
        {
            if (!_quietMode) AnsiConsole.MarkupLine("[green]All npm packages are up to date![/]");
            return;
        }

        // Extract only the JSON portion (npm may include warnings/notices before or after the JSON)
        var jsonStart = output.IndexOf('{');
        var jsonEnd = output.LastIndexOf('}');

        if (jsonStart == -1 || jsonEnd == -1 || jsonEnd < jsonStart)
        {
            if (!_quietMode) AnsiConsole.MarkupLine("[green]All npm packages are up to date![/]");
            return;
        }

        var jsonOutput = output.Substring(jsonStart, jsonEnd - jsonStart + 1);
        var outdatedPackages = JsonDocument.Parse(jsonOutput);
        var table = new Table();
        table.AddColumn("Package");
        table.AddColumn("Workspace");
        table.AddColumn("Current Version");
        table.AddColumn("Latest Version");
        table.AddColumn("Update Type");

        var npmCandidates = new List<NpmCandidate>();

        // First pass: collect all candidates. When the same package is outdated in multiple
        // workspaces npm reports it as an array of entries; we emit one candidate per entry so
        // each workspace's package.json is updated to its own correct version.
        foreach (var package in outdatedPackages.RootElement.EnumerateObject())
        {
            var packageName = package.Name;
            var entries = package.Value.ValueKind == JsonValueKind.Array
                ? package.Value.EnumerateArray().Where(e => e.ValueKind == JsonValueKind.Object).ToArray()
                : [package.Value];

            foreach (var entry in entries)
            {
                AppendNpmCandidate(npmCandidates, packageName, entry, excludedPackages, includeMajorFrameworkUpdates, workspaceMap);
            }
        }

        // Family logic: when packages share the same current version (likely the same family),
        // an npm install fails with peer-dependency conflicts if some members can reach a new
        // major and others cannot. Pin the whole family to the lowest reachable target major.
        // Scope grouping by workspace -- different workspaces may legitimately diverge.
        var npmFamilies = npmCandidates
            .Where(c => c is { IsExcluded: false, LatestVersion: not null })
            .GroupBy(c => (c.WorkspaceName, c.WantedVersion))
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var family in npmFamilies)
        {
            var familyMembers = family.ToList();
            var familyTargetMajors = familyMembers.Select(c => GetMajorVersion(c.LatestVersion!)).Distinct().ToArray();
            if (familyTargetMajors.Length <= 1) continue;

            var lowestTargetMajor = familyTargetMajors.Min();
            foreach (var member in familyMembers.Where(c => GetMajorVersion(c.LatestVersion!) > lowestTargetMajor))
            {
                var safeVersion = GetHighestNpmVersionInMajor(member.PackageName, lowestTargetMajor);
                member.LatestVersion = safeVersion is not null && IsNewerVersion(safeVersion, member.WantedVersion)
                    ? safeVersion
                    : null;
            }
        }

        // Second pass: build table and install list (grouped by workspace) from final candidate state.
        // Root-only updates use empty-string as the bucket key so the dictionary stays non-nullable.
        var updatesByWorkspace = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var candidate in npmCandidates)
        {
            var workspaceLabel = candidate.WorkspaceName ?? workspaceMap.RootName;

            if (candidate.IsExcluded)
            {
                table.AddRow(candidate.PackageName, workspaceLabel, candidate.WantedVersion, "-", "[blue]Excluded[/]");
                if (_quietMode) Console.WriteLine($"frontend excluded {candidate.PackageName} {candidate.WantedVersion} [{workspaceLabel}]");
                FrontendSummary.Excluded++;
                continue;
            }

            if (candidate.LatestVersion is null) continue;

            var updateType = GetUpdateType(candidate.WantedVersion, candidate.LatestVersion);
            FrontendSummary.IncrementUpdateType(updateType);

            var statusColor = updateType switch
            {
                UpdateType.Major => "[yellow]Major[/]",
                UpdateType.Minor => "[green]Minor[/]",
                UpdateType.Patch => "Patch",
                _ => "[green]Minor[/]"
            };

            table.AddRow(candidate.PackageName, workspaceLabel, candidate.WantedVersion, candidate.LatestVersion, statusColor);
            if (_quietMode) Console.WriteLine($"frontend {updateType.ToString().ToLowerInvariant()} {candidate.PackageName} {candidate.WantedVersion} -> {candidate.LatestVersion} [{workspaceLabel}]");

            var bucketKey = candidate.WorkspaceName ?? string.Empty;
            if (!updatesByWorkspace.TryGetValue(bucketKey, out var list))
            {
                list = new List<string>();
                updatesByWorkspace[bucketKey] = list;
            }

            list.Add($"{candidate.PackageName}@{candidate.LatestVersion}");
        }

        if (table.Rows.Count > 0)
        {
            if (!_quietMode) AnsiConsole.Write(table);
        }
        else
        {
            if (!_quietMode) AnsiConsole.MarkupLine("[green]All npm packages are up to date![/]");
            return;
        }

        var totalUpdates = updatesByWorkspace.Values.Sum(l => l.Count);
        if (totalUpdates > 0 && !dryRun)
        {
            if (!_quietMode)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[blue]Updating packages...[/]");
            }

            // Run a separate `npm install` per workspace so each workspace's package.json picks up
            // the new exact versions. With npm workspaces, omitting `-w` only updates root.
            // Process workspace-scoped buckets first; the root-only bucket (empty-string key) runs last.
            var orderedBuckets = updatesByWorkspace
                .OrderBy(kvp => kvp.Key.Length == 0 ? 1 : 0)
                .ThenBy(kvp => kvp.Key, StringComparer.Ordinal);
            foreach (var (workspaceName, packagesForWorkspace) in orderedBuckets)
            {
                var workspaceFlag = workspaceName.Length == 0 ? string.Empty : $"-w {workspaceName} ";
                var updateCommand = $"npm install --save-exact {workspaceFlag}{string.Join(" ", packagesForWorkspace)}";
                ProcessHelper.StartProcess(updateCommand, Configuration.ApplicationFolder);
            }

            if (!_quietMode) AnsiConsole.MarkupLine("[green]npm packages updated successfully![/]");

            // Patch transitive vulnerabilities that resolve within the current semver ranges.
            // Running this here keeps the update-packages command as the single source of truth
            // for dependency hygiene; otherwise transitive vulns linger silently between bumps.
            if (!_quietMode)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine("[blue]Running npm audit fix...[/]");
            }

            ProcessHelper.StartProcess("npm audit fix", Configuration.ApplicationFolder, exitOnError: false, throwOnError: false);
        }
        else if (totalUpdates > 0 && !_quietMode)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {totalUpdates} npm package(s) (dry-run mode)[/]");
        }
    }

    private static void AppendNpmCandidate(List<NpmCandidate> candidates, string packageName, JsonElement packageInfo, string[] excludedPackages, bool includeMajorFrameworkUpdates, NpmWorkspaceMap workspaceMap)
    {
        var workspaceName = ResolveWorkspaceName(packageInfo, workspaceMap);

        if (IsPackageExcluded(packageName, excludedPackages))
        {
            var excludedWanted = packageInfo.TryGetProperty("wanted", out var packageWantedElement)
                ? packageWantedElement.GetString() ?? "unknown"
                : "unknown";
            candidates.Add(new NpmCandidate(packageName, excludedWanted, null) { IsExcluded = true, WorkspaceName = workspaceName });
            return;
        }

        if (!packageInfo.TryGetProperty("current", out var currentElement) ||
            !packageInfo.TryGetProperty("wanted", out var wantedElement) ||
            !packageInfo.TryGetProperty("latest", out var latestElement))
        {
            return;
        }

        var currentVersion = currentElement.GetString();
        var wantedVersion = wantedElement.GetString();
        var latestVersion = latestElement.GetString();

        if (currentVersion is null || wantedVersion is null || latestVersion is null) return;

        // Skip packages already at latest
        if (wantedVersion == latestVersion) return;

        // Restrict @types/node to current major unless explicitly allowed
        if (packageName == "@types/node" && !includeMajorFrameworkUpdates)
        {
            var resolved = GetHighestNpmVersionInMajor(packageName, GetMajorVersion(wantedVersion));
            if (resolved is null || !IsNewerVersion(resolved, wantedVersion))
            {
                return;
            }

            latestVersion = resolved;
        }

        candidates.Add(new NpmCandidate(packageName, wantedVersion, latestVersion) { WorkspaceName = workspaceName });
    }

    private static string? ResolveWorkspaceName(JsonElement packageInfo, NpmWorkspaceMap workspaceMap)
    {
        if (!packageInfo.TryGetProperty("dependent", out var dependentElement)) return null;
        var dependent = dependentElement.GetString();
        if (string.IsNullOrEmpty(dependent)) return null;
        if (dependent == workspaceMap.RootName) return null;
        return workspaceMap.DependentToFullName.GetValueOrDefault(dependent, dependent);
    }

    private static NpmWorkspaceMap BuildNpmWorkspaceMap(string rootPackageJsonPath)
    {
        var rootJson = JsonDocument.Parse(File.ReadAllText(rootPackageJsonPath));
        var rootName = rootJson.RootElement.TryGetProperty("name", out var rootNameElement) ? rootNameElement.GetString() ?? "" : "";

        var dependentToFullName = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!rootJson.RootElement.TryGetProperty("workspaces", out var workspacesElement) || workspacesElement.ValueKind != JsonValueKind.Array)
        {
            return new NpmWorkspaceMap(rootName, dependentToFullName);
        }

        var rootDirectory = Path.GetDirectoryName(rootPackageJsonPath)!;
        foreach (var workspaceGlob in workspacesElement.EnumerateArray())
        {
            var pattern = workspaceGlob.GetString();
            if (pattern is null) continue;
            foreach (var workspaceDirectory in ExpandWorkspaceGlob(rootDirectory, pattern))
            {
                var workspacePackageJson = Path.Combine(workspaceDirectory, "package.json");
                if (!File.Exists(workspacePackageJson)) continue;
                var workspaceJson = JsonDocument.Parse(File.ReadAllText(workspacePackageJson));
                if (!workspaceJson.RootElement.TryGetProperty("name", out var nameElement)) continue;
                var fullName = nameElement.GetString();
                if (string.IsNullOrEmpty(fullName)) continue;
                // npm reports `dependent` as the unscoped form for scoped packages (@repo/ui -> "ui"),
                // and as the package name as-is for unscoped ones (account-webapp -> "account-webapp").
                var unscoped = fullName.StartsWith('@') ? fullName.Split('/', 2)[1] : fullName;
                dependentToFullName[unscoped] = fullName;
            }
        }

        return new NpmWorkspaceMap(rootName, dependentToFullName);
    }

    private static IEnumerable<string> ExpandWorkspaceGlob(string rootDirectory, string pattern)
    {
        // npm workspaces only use the trailing `*` form (e.g. "shared-webapp/*" or a literal path).
        // Expand the trailing wildcard manually and return a literal path otherwise.
        if (pattern.EndsWith("/*"))
        {
            var parentRelative = pattern[..^2];
            var parentDirectory = Path.Combine(rootDirectory, parentRelative);
            if (!Directory.Exists(parentDirectory)) return [];
            return Directory.GetDirectories(parentDirectory);
        }

        var literalPath = Path.Combine(rootDirectory, pattern);
        return Directory.Exists(literalPath) ? [literalPath] : [];
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
        var response = await HttpClient.GetStringAsync($"https://api.nuget.org/v3-flatcontainer/{packageName.ToLower()}/index.json");
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

    private static void UpdateAspireSdkVersion(bool dryRun)
    {
        var appHostPath = Path.Combine(Configuration.ApplicationFolder, "AppHost", "AppHost.csproj");
        if (!File.Exists(appHostPath)) return;

        // Read file content to check for SDK
        var fileContent = File.ReadAllText(appHostPath);

        // Use regex to find the Aspire SDK version in Project attribute
        var projectSdkMatch = Regex.Match(fileContent, @"<Project\s+Sdk=""Aspire\.AppHost\.Sdk/([^""]+)"">");
        if (!projectSdkMatch.Success) return;

        var currentSdkVersion = projectSdkMatch.Groups[1].Value;

        // Get the Aspire.Hosting version from Directory.Packages.props
        var directoryPackagesPath = Path.Combine(Configuration.ApplicationFolder, "Directory.Packages.props");
        var packagesXml = XDocument.Load(directoryPackagesPath);
        var appHostPackageElement = packagesXml.Descendants("PackageVersion")
            .FirstOrDefault(e => e.Attribute("Include")?.Value == "Aspire.Hosting");

        var targetSdkVersion = appHostPackageElement?.Attribute("Version")?.Value;
        if (targetSdkVersion is null || targetSdkVersion == currentSdkVersion) return;

        if (_quietMode)
        {
            Console.WriteLine($"backend {GetUpdateType(currentSdkVersion, targetSdkVersion).ToString().ToLowerInvariant()} Aspire.AppHost.Sdk {currentSdkVersion} -> {targetSdkVersion} (sdk)");
        }
        else
        {
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
        }

        if (!dryRun)
        {
            // Replace Project element SDK attribute version
            var updatedContent = fileContent.Replace(
                $@"<Project Sdk=""Aspire.AppHost.Sdk/{currentSdkVersion}"">",
                $@"<Project Sdk=""Aspire.AppHost.Sdk/{targetSdkVersion}"">"
            );

            // Write back preserving original formatting
            File.WriteAllText(appHostPath, updatedContent);
            if (!_quietMode) AnsiConsole.MarkupLine($"[green]Updated Aspire.AppHost.Sdk from {currentSdkVersion} to {targetSdkVersion}[/]");
        }
        else if (!_quietMode)
        {
            AnsiConsole.MarkupLine("[blue]Would update Aspire SDK version (dry-run mode)[/]");
        }
    }

    private static async Task UpdateDotnetToolsAsync(bool dryRun)
    {
        // Find all dotnet-tools.json files
        var dotnetToolsFiles = Directory.GetFiles(Configuration.SourceCodeFolder, "dotnet-tools.json", SearchOption.AllDirectories);

        if (dotnetToolsFiles.Length == 0) return;

        foreach (var dotnetToolsPath in dotnetToolsFiles)
        {
            var relativePath = Path.GetRelativePath(Configuration.SourceCodeFolder, dotnetToolsPath);

            if (!_quietMode)
            {
                AnsiConsole.WriteLine();
                AnsiConsole.MarkupLine($"Analyzing .NET tools in {relativePath}...");
            }

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
                continue;
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
                if (_quietMode) Console.WriteLine($"backend {updateType.ToString().ToLowerInvariant()} {toolName} {currentVersion} -> {latestVersion} (tool)");
                dotnetToolUpdatesToApply.Add((toolName, currentVersion, latestVersion));
            }

            if (!_quietMode)
            {
                if (table.Rows.Count > 0)
                {
                    AnsiConsole.Write(table);
                }
                else
                {
                    AnsiConsole.MarkupLine($"[green]All .NET tools in {relativePath} are up to date![/]");
                }
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
                if (!_quietMode) AnsiConsole.MarkupLine($"[green]{relativePath} updated successfully![/]");
            }
            else if (dotnetToolUpdatesToApply.Count > 0 && !_quietMode)
            {
                AnsiConsole.MarkupLine($"[blue]Would update {dotnetToolUpdatesToApply.Count} .NET tool(s) in {relativePath} (dry-run mode)[/]");
            }
        }
    }

    private static async Task CheckDotnetSdkVersionAsync(bool dryRun, bool earlyCheck, bool includeMajorFrameworkUpdates)
    {
        var globalJsonPath = Path.Combine(Configuration.ApplicationFolder, "global.json");
        var globalJson = await File.ReadAllTextAsync(globalJsonPath);
        var globalJsonDoc = JsonDocument.Parse(globalJson);
        var currentVersion = globalJsonDoc.RootElement.GetProperty("sdk").GetProperty("version").GetString()!;

        // Get latest .NET SDK version from the official releases
        var currentMajor = GetMajorVersion(currentVersion);

        // Determine which version to target
        int targetMajor;
        if (includeMajorFrameworkUpdates)
        {
            var latestMajorVersion = await GetLatestDotnetMajorVersion();
            targetMajor = latestMajorVersion > currentMajor ? latestMajorVersion : currentMajor;
        }
        else
        {
            targetMajor = currentMajor;
        }

        var latestVersion = await GetLatestDotnetSdkVersion(targetMajor);

        if (latestVersion == currentVersion)
        {
            if (!earlyCheck && !_quietMode)
            {
                AnsiConsole.MarkupLine("[green]✓ .NET SDK version is already up to date[/]");
            }

            return;
        }

        // Check if the latest version is installed locally
        var isInstalledLocally = IsDotnetSdkInstalledLocally(latestVersion);

        // Early check - only care about blocking if SDK not installed
        if (earlyCheck)
        {
            if (isInstalledLocally) return; // If installed, we'll update it after other updates

            AnsiConsole.MarkupLine($"""
                                    [red]❌ Cannot update .NET SDK: version {latestVersion} is not installed locally![/]
                                    [yellow]   Install it first: brew upgrade dotnet-sdk (macOS) or winget upgrade Microsoft.DotNet.SDK.{targetMajor} (Windows)[/]
                                    """
            );
            Environment.Exit(1);
        }

        if (_quietMode)
        {
            Console.WriteLine($"backend {GetUpdateType(currentVersion, latestVersion).ToString().ToLowerInvariant()} dotnet-sdk {currentVersion} -> {latestVersion} (sdk)");
        }
        else
        {
            AnsiConsole.MarkupLine($"[blue]A newer .NET SDK version is available: {latestVersion} (current: {currentVersion})[/]");
        }

        // Late check - show status information
        if (!isInstalledLocally)
        {
            AnsiConsole.MarkupLine(
                $"""
                 [red]   ⚠️  .NET SDK {latestVersion} is NOT installed on your machine![/]
                 [yellow]   Update .NET: brew upgrade dotnet-sdk (macOS) or winget upgrade Microsoft.DotNet.SDK.{targetMajor} (Windows)[/]
                 """
            );
        }

        // Actually update .NET SDK if not in dry-run mode and SDK is installed
        if (!dryRun && isInstalledLocally)
        {
            // Update all global.json files
            await UpdateAllGlobalJsonFiles(latestVersion);

            // Also update Prerequisite.cs
            await UpdatePrerequisiteDotnetVersion(latestVersion);

            // Update TargetFramework in all csproj files
            await UpdateTargetFrameworkInAllCsprojFiles(latestVersion);

            // Update ContainerBaseImage in all csproj files
            await UpdateContainerBaseImageInAllCsprojFiles(latestVersion);
        }
    }

    private static async Task UpdateTargetFrameworkInAllCsprojFiles(string newSdkVersion)
    {
        var newMajor = GetMajorVersion(newSdkVersion);
        var newTargetFramework = $"net{newMajor}.0";

        var pattern = @"<TargetFramework>net\d+\.0(-\w+)?</TargetFramework>";
        var replacement = $"<TargetFramework>{newTargetFramework}$1</TargetFramework>";

        var updatedFiles = new List<string>();
        var csprojFiles = Directory.GetFiles(Configuration.SourceCodeFolder, "*.csproj", SearchOption.AllDirectories);

        foreach (var csprojPath in csprojFiles)
        {
            var content = await File.ReadAllTextAsync(csprojPath);
            var updatedContent = Regex.Replace(content, pattern, replacement);

            if (updatedContent != content)
            {
                await File.WriteAllTextAsync(csprojPath, updatedContent);
                updatedFiles.Add(Path.GetRelativePath(Configuration.SourceCodeFolder, csprojPath));
            }
        }

        if (updatedFiles.Count > 0)
        {
            AnsiConsole.MarkupLine($"[green]✓ Updated TargetFramework to {newTargetFramework} in {updatedFiles.Count} csproj file(s)[/]");
        }
    }

    private static async Task UpdateContainerBaseImageInAllCsprojFiles(string newSdkVersion)
    {
        var newMajor = GetMajorVersion(newSdkVersion);

        // Pattern to match ContainerBaseImage tags with dotnet base images
        // Captures: mcr.microsoft.com/dotnet/{runtime}:{version}-{variant}
        var pattern = @"<ContainerBaseImage>mcr\.microsoft\.com/dotnet/(aspnet|runtime):\d+\.0(-[^<]+)?</ContainerBaseImage>";
        var replacement = $"<ContainerBaseImage>mcr.microsoft.com/dotnet/$1:{newMajor}.0$2</ContainerBaseImage>";

        var updatedFiles = new List<string>();
        var csprojFiles = Directory.GetFiles(Configuration.SourceCodeFolder, "*.csproj", SearchOption.AllDirectories);

        foreach (var csprojPath in csprojFiles)
        {
            var content = await File.ReadAllTextAsync(csprojPath);
            var updatedContent = Regex.Replace(content, pattern, replacement);

            if (updatedContent != content)
            {
                await File.WriteAllTextAsync(csprojPath, updatedContent);
                updatedFiles.Add(Path.GetRelativePath(Configuration.SourceCodeFolder, csprojPath));
            }
        }

        if (updatedFiles.Count > 0)
        {
            AnsiConsole.MarkupLine($"[green]✓ Updated ContainerBaseImage to .NET {newMajor}.0 in {updatedFiles.Count} csproj file(s)[/]");
        }
    }

    private static async Task UpdateAllGlobalJsonFiles(string newVersion)
    {
        var globalJsonFiles = Directory.GetFiles(Configuration.SourceCodeFolder, "global.json", SearchOption.AllDirectories);
        var updatedFiles = new List<string>();

        foreach (var filePath in globalJsonFiles)
        {
            var content = await File.ReadAllTextAsync(filePath);
            var jsonDocument = JsonDocument.Parse(content);
            var currentVersion = jsonDocument.RootElement.GetProperty("sdk").GetProperty("version").GetString()!;

            if (currentVersion == newVersion) continue;

            using var stream = new MemoryStream();
            await using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();

                foreach (var property in jsonDocument.RootElement.EnumerateObject())
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
                            writer.WriteString("version", newVersion);
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
            await File.WriteAllTextAsync(filePath, updatedJson);
            updatedFiles.Add(Path.GetRelativePath(Configuration.SourceCodeFolder, filePath));
        }

        if (updatedFiles.Count > 0)
        {
            AnsiConsole.MarkupLine($"[green]✓ Updated .NET SDK version to {newVersion} in {updatedFiles.Count} global.json file(s)[/]");
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

    private static async Task<int> GetLatestDotnetMajorVersion()
    {
        // Get the releases index
        const string dotnetReleaseBaseUrl = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata";
        var response = await HttpClient.GetStringAsync($"{dotnetReleaseBaseUrl}/releases-index.json");
        var releasesIndex = JsonDocument.Parse(response);

        // Find the latest major version
        var latestMajor = releasesIndex.RootElement
            .GetProperty("releases-index")
            .EnumerateArray()
            .Select(release =>
                {
                    var channelVersion = release.GetProperty("channel-version").GetString()!;
                    var parts = channelVersion.Split('.');
                    return int.Parse(parts[0]);
                }
            )
            .Max();

        return latestMajor;
    }

    private static async Task<string> GetLatestDotnetSdkVersion(int majorVersion)
    {
        // Get the releases index
        const string dotnetReleaseBaseUrl = "https://dotnetcli.blob.core.windows.net/dotnet/release-metadata";
        var response = await HttpClient.GetStringAsync($"{dotnetReleaseBaseUrl}/releases-index.json");
        var releasesIndex = JsonDocument.Parse(response);

        // Find the channel for the major version
        var channelVersion = releasesIndex.RootElement
            .GetProperty("releases-index")
            .EnumerateArray()
            .Select(release => release.GetProperty("channel-version").GetString()!)
            .First(version => version.StartsWith($"{majorVersion}."));

        // Get the channel releases
        var channelUrl = $"{dotnetReleaseBaseUrl}/{channelVersion}/releases.json";
        var channelResponse = await HttpClient.GetStringAsync(channelUrl);
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

    private static async Task SyncNodeVersionFile()
    {
        var prerequisitePath = Path.Combine(Configuration.SourceCodeFolder, "developer-cli", "Installation", "Prerequisite.cs");
        var content = await File.ReadAllTextAsync(prerequisitePath);
        var match = Regex.Match(content, @"Node = new CommandLineToolPrerequisite\(""node"", ""NodeJS"", new Version\((\d+), (\d+), (\d+)\)\);");
        if (!match.Success) return;

        var nodeVersion = $"{match.Groups[1].Value}.{match.Groups[2].Value}.{match.Groups[3].Value}";
        await File.WriteAllTextAsync(Path.Combine(Configuration.ApplicationFolder, ".node-version"), nodeVersion + Environment.NewLine);
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

        if (_quietMode)
        {
            if (showBackend) Console.WriteLine($"summary backend patch={BackendSummary.Patch} minor={BackendSummary.Minor} major={BackendSummary.Major} excluded={BackendSummary.Excluded} uptodate={BackendSummary.UpToDate}");
            if (showFrontend) Console.WriteLine($"summary frontend patch={FrontendSummary.Patch} minor={FrontendSummary.Minor} major={FrontendSummary.Major} excluded={FrontendSummary.Excluded} uptodate={FrontendSummary.UpToDate}");
            return;
        }

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

    private static string? GetHighestNpmVersionInMajor(string packageName, int major)
    {
        var output = ProcessHelper.StartProcess($"npm view {packageName} versions --json", Configuration.ApplicationFolder, true, exitOnError: false, throwOnError: false);
        if (string.IsNullOrWhiteSpace(output)) return null;

        var jsonStart = output.IndexOf('[');
        var jsonEnd = output.LastIndexOf(']');
        if (jsonStart == -1 || jsonEnd == -1 || jsonEnd < jsonStart) return null;

        var versions = JsonDocument.Parse(output.Substring(jsonStart, jsonEnd - jsonStart + 1)).RootElement
            .EnumerateArray()
            .Select(element => element.GetString()!)
            .Where(version => !IsPreReleaseVersion(version) && GetMajorVersion(version) == major)
            .ToList();

        if (versions.Count == 0) return null;

        var highestVersion = versions[0];
        foreach (var version in versions)
        {
            if (IsNewerVersion(version, highestVersion)) highestVersion = version;
        }

        return highestVersion;
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

    private sealed class NpmCandidate(string packageName, string wantedVersion, string? latestVersion)
    {
        public string PackageName { get; } = packageName;

        public string WantedVersion { get; } = wantedVersion;

        public string? LatestVersion { get; set; } = latestVersion;

        public bool IsExcluded { get; init; }

        // Full workspace package name (e.g. "@repo/emails") when the outdated dep lives in a workspace,
        // or null when it lives in the root application/package.json. Used to scope `npm install -w`.
        public string? WorkspaceName { get; init; }
    }

    // Maps `dependent` values reported by `npm outdated --json` to the full workspace package name.
    // npm reports `dependent` as the unscoped form (e.g. "ui" for "@repo/ui") plus the root name as-is
    // ("application"). The root entry is keyed by the root package name with a null value to indicate
    // "no -w flag needed".
    private sealed record NpmWorkspaceMap(string RootName, IReadOnlyDictionary<string, string> DependentToFullName);

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
