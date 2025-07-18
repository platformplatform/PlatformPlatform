using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text;
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
    private static readonly UpdateSummary BackendSummary = new();
    private static readonly UpdateSummary FrontendSummary = new();

    public UpdatePackagesCommand() : base("update-packages", "Updates packages to their latest versions while preserving major versions for restricted packages")
    {
        AddOption(new Option<bool>(["--backend", "-b"], "Update only backend packages (NuGet)"));
        AddOption(new Option<bool>(["--frontend", "-f"], "Update only frontend packages (npm)"));
        AddOption(new Option<bool>(["--dry-run", "-d"], "Show what would be updated without making changes"));
        AddOption(new Option<bool>(["--build"], "Run build command after successful package updates"));
        AddOption(new Option<string?>(["--exclude", "-e"], "Comma-separated list of packages to exclude from updates"));
        AddOption(new Option<bool>(["--skip-update-dotnet"], "Skip updating .NET SDK version in global.json"));
        Handler = CommandHandler.Create<bool, bool, bool, bool, string?, bool>(Execute);
    }

    private static async Task Execute(bool backend, bool frontend, bool dryRun, bool build, string? exclude, bool skipUpdateDotnet)
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
            AnsiConsole.WriteLine();
        }

        BackendSummary.Reset();
        FrontendSummary.Reset();

        var updateBackend = backend || (!backend && !frontend);
        var updateFrontend = frontend || (!backend && !frontend);
        var updateDotnet = !skipUpdateDotnet;

        // Check .NET SDK version early if updating dotnet (default behavior)
        if (updateBackend && updateDotnet && !dryRun)
        {
            await CheckDotnetSdkVersionAsync(dryRun, updateDotnet, earlyCheck: true);
        }

        if (updateBackend)
        {
            await UpdateNuGetPackagesAsync(dryRun, excludedPackages);
            UpdateAspireSdkVersion(dryRun);
            await UpdateDotnetToolsAsync(dryRun);

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
        
        // Display update summary
        DisplayUpdateSummary(updateBackend, updateFrontend);
        
        // Show .NET SDK info at the end for backend updates (unless explicitly skipped)
        if (updateBackend && updateDotnet)
        {
            AnsiConsole.WriteLine();
            await CheckDotnetSdkVersionAsync(dryRun, updateDotnet, earlyCheck: false);
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

        var updates = new List<PackageUpdate>();

        foreach (var packageElement in packageElements)
        {
            var packageName = packageElement.Attribute("Include")?.Value;
            var currentVersion = packageElement.Attribute("Version")?.Value;
            if (packageName is null || currentVersion is null) continue;

            // Skip excluded packages
            if (excludedPackages.Contains(packageName))
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

            var status = GetNuGetUpdateStatus(packageName, currentVersion, versionResolution.LatestVersion);
            
            // Only show packages that can actually be updated
            if (status.CanUpdate)
            {
                var updateType = GetUpdateType(currentVersion, status.TargetVersion);
                BackendSummary.IncrementUpdateType(updateType);
                
                var statusColor = updateType switch
                {
                    UpdateType.Major => "[yellow]Major[/]",
                    UpdateType.Minor => "[green]Minor[/]",
                    UpdateType.Patch => "Patch",
                    _ => "[green]Minor[/]"
                };
                
                table.AddRow(packageName, currentVersion, status.TargetVersion, statusColor);
                updates.Add(new PackageUpdate(packageElement, packageName, currentVersion, status.TargetVersion));
            }
            else if (status.IsRestricted)
            {
                // Show restricted packages in the table but don't count them as updates
                table.AddRow(packageName, currentVersion, versionResolution.LatestVersion, "[red]Excluded[/]");
                BackendSummary.Excluded++;
            }
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
            if (latestFromApi is null) return new VersionResolution(null);

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

    private static Task<JsonDocument> GetOutdatedPackagesJsonAsync()
    {
        var output = ProcessHelper.StartProcess("dotnet list package --outdated --include-prerelease --format json", Configuration.ApplicationFolder, true, exitOnError: false, throwOnError: false);

        if (string.IsNullOrEmpty(output))
        {
            AnsiConsole.MarkupLine("[red]Failed to get outdated packages information[/]");
            Environment.Exit(1);
        }

        return Task.FromResult(JsonDocument.Parse(output));
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
            
            // Skip packages that are already at the wanted version
            if (currentVersion == wantedVersion) continue;
            
            // Check update type
            var updateType = GetUpdateType(currentVersion, wantedVersion);
            FrontendSummary.IncrementUpdateType(updateType);
            
            var statusColor = updateType switch
            {
                UpdateType.Major => "[yellow]Major[/]",
                UpdateType.Minor => "[green]Minor[/]",
                UpdateType.Patch => "Patch",
                _ => "[green]Minor[/]"
            };
            
            table.AddRow(packageName, currentVersion, wantedVersion, statusColor);
            updates.Add($"{packageName}@{wantedVersion}");
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
            var versionString = version.GetString();
            if (versionString is not null && !versionString.Contains("-") && GetMajorVersion(versionString) == majorVersion)
            {
                matchingVersions.Add(versionString);
            }
        }

        return matchingVersions.LastOrDefault();
    }

    private static int GetMajorVersion(string version)
    {
        var match = Regex.Match(version, @"^(\d+)");
        return match.Success ? int.Parse(match.Groups[1].Value) : 0;
    }

    private static async Task RunBuildCommand(bool backend, bool frontend)
    {
        if (!backend && !frontend) return;

        AnsiConsole.MarkupLine("[blue]Running build command...[/]");

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

        await buildCommand.InvokeAsync(args.ToArray());
        AnsiConsole.MarkupLine("[green]Build completed successfully![/]");
    }

    private static void UpdateAspireSdkVersion(bool dryRun)
    {
        var appHostPath = Path.Combine(Configuration.ApplicationFolder, "AppHost", "AppHost.csproj");
        if (!File.Exists(appHostPath)) return;

        var appHostXml = XDocument.Load(appHostPath);
        var sdkElement = appHostXml.Descendants("Sdk")
            .FirstOrDefault(e => e.Attribute("Name")?.Value == "Aspire.AppHost.Sdk");

        if (sdkElement is null) return;

        var currentSdkVersion = sdkElement.Attribute("Version")?.Value;
        if (currentSdkVersion is null) return;

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
            sdkElement.SetAttributeValue("Version", targetSdkVersion);
            appHostXml.Save(appHostPath);
            AnsiConsole.MarkupLine($"[green]Updated Aspire.AppHost.Sdk from {currentSdkVersion} to {targetSdkVersion}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[blue]Would update Aspire SDK version (dry-run mode)[/]");
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
        
        var updates = new List<(string toolName, string currentVersion, string latestVersion)>();
        
        if (toolsDocument.RootElement.TryGetProperty("tools", out var tools))
        {
            foreach (var tool in tools.EnumerateObject())
            {
                var toolName = tool.Name;
                if (tool.Value.TryGetProperty("version", out var versionElement))
                {
                    var currentVersion = versionElement.GetString();
                    if (currentVersion is not null)
                    {
                        // Get latest version from NuGet API
                        var latestVersion = await GetLatestVersionFromNuGetApi(toolName);
                        
                        if (latestVersion is not null && latestVersion != currentVersion && IsNewerVersion(latestVersion, currentVersion))
                        {
                            // Handle prerelease versions
                            if (IsPreReleaseVersion(latestVersion) && !IsPreReleaseVersion(currentVersion))
                            {
                                // Try to find a stable version
                                var stableVersion = await GetLatestStableVersionFromNuGetApi(toolName);
                                if (stableVersion is not null && stableVersion != currentVersion && IsNewerVersion(stableVersion, currentVersion))
                                {
                                    latestVersion = stableVersion;
                                }
                                else
                                {
                                    continue; // Skip if no stable update available
                                }
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
                            updates.Add((toolName, currentVersion, latestVersion));
                        }
                    }
                }
            }
        }
        
        if (table.Rows.Count > 0)
        {
            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[green]All .NET tools are up to date![/]");
        }
        
        if (updates.Count > 0 && !dryRun)
        {
            // Parse and update the JSON
            using var jsonDoc = JsonDocument.Parse(toolsJson);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
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
                            
                            var updateInfo = updates.FirstOrDefault(u => u.toolName == tool.Name);
                            
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
        else if (updates.Count > 0)
        {
            AnsiConsole.MarkupLine($"[blue]Would update {updates.Count} .NET tool(s) (dry-run mode)[/]");
        }
    }
    
    private static async Task CheckDotnetSdkVersionAsync(bool dryRun, bool updateDotnet, bool earlyCheck = false)
    {
        var globalJsonPath = Path.Combine(Configuration.ApplicationFolder, "global.json");
        if (!File.Exists(globalJsonPath)) return;

        var globalJson = await File.ReadAllTextAsync(globalJsonPath);
        var globalJsonDoc = JsonDocument.Parse(globalJson);
        
        if (!globalJsonDoc.RootElement.TryGetProperty("sdk", out var sdk) || 
            !sdk.TryGetProperty("version", out var versionElement))
        {
            return;
        }

        var currentVersion = versionElement.GetString();
        if (currentVersion is null) return;
        
        // Get latest .NET SDK version from the official releases
        var currentMajor = GetMajorVersion(currentVersion);
        var latestInMajor = await GetLatestDotnetSdkVersion(currentMajor);
        
        if (latestInMajor is null || latestInMajor == currentVersion || !IsNewerVersion(latestInMajor, currentVersion))
        {
            if (updateDotnet && !earlyCheck)
            {
                AnsiConsole.MarkupLine("[green]✓ .NET SDK version is already up to date[/]");
            }
            return;
        }

        // Check if the latest version is installed locally
        var isInstalledLocally = await IsDotnetSdkInstalledLocally(latestInMajor);
        
        // Early check - only care about blocking if SDK not installed
        if (earlyCheck)
        {
            if (isInstalledLocally) return; // If installed, we'll update it after other updates
            
            AnsiConsole.MarkupLine($"""
                [red]❌ Cannot update .NET SDK: version {latestInMajor} is not installed locally![/]
                [yellow]   Install it first: brew upgrade dotnet-sdk (macOS) or winget upgrade Microsoft.DotNet.SDK.{currentMajor} (Windows)[/]
                """);
            Environment.Exit(1);
        }
        
        // Late check - show status information
        if (dryRun)
        {
            AnsiConsole.MarkupLine($"[blue]A newer .NET SDK version is available: {latestInMajor} (current: {currentVersion})[/]");
            if (!isInstalledLocally)
            {
                AnsiConsole.MarkupLine($"""
                    [red]⚠️  But you need to install .NET SDK {latestInMajor} on your machine first![/]
                    [yellow]   Update .NET: brew upgrade dotnet-sdk (macOS) or winget upgrade Microsoft.DotNet.SDK.{currentMajor} (Windows)[/]
                    """);
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[yellow]⚠️  A newer .NET SDK version is available: {latestInMajor} (current: {currentVersion})[/]");
            if (!isInstalledLocally)
            {
                AnsiConsole.MarkupLine($"""
                    [red]   ⚠️  .NET SDK {latestInMajor} is NOT installed on your machine![/]
                    [yellow]   Update .NET: brew upgrade dotnet-sdk (macOS) or winget upgrade Microsoft.DotNet.SDK.{currentMajor} (Windows)[/]
                    """);
            }
        }
        
        // Actually update .NET SDK if requested and not in dry-run mode
        if (updateDotnet && !dryRun && isInstalledLocally)
        {
            
            // Update global.json
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
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
        else if (updateDotnet && dryRun)
        {
            if (!isInstalledLocally)
            {
                // Block execution in dry-run mode when SDK is not installed
                Environment.Exit(1);
            }
        }
    }
    
    private static Task<bool> IsDotnetSdkInstalledLocally(string version)
    {
        var output = ProcessHelper.StartProcess("dotnet --list-sdks", Configuration.ApplicationFolder, true, exitOnError: false, throwOnError: false);
        
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var match = Regex.Match(line, @"^(\d+\.\d+\.\d+)");
            if (match.Success && match.Groups[1].Value == version)
            {
                return Task.FromResult(true);
            }
        }
        
        return Task.FromResult(false);
    }
    
    private static async Task<string?> GetLatestDotnetSdkVersion(int majorVersion)
    {
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("PlatformPlatform-CLI/1.0");
        
        // Get the releases index
        var response = await httpClient.GetStringAsync("https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/releases-index.json");
        var releasesIndex = JsonDocument.Parse(response);
        
        // Find the channel for the major version
        string? channelVersion = null;
        foreach (var release in releasesIndex.RootElement.GetProperty("releases-index").EnumerateArray())
        {
            if (release.TryGetProperty("channel-version", out var channelVersionElement))
            {
                var version = channelVersionElement.GetString();
                if (version?.StartsWith($"{majorVersion}.") == true)
                {
                    channelVersion = version;
                    break;
                }
            }
        }
        
        if (channelVersion is null) return null;
        
        // Get the channel releases
        var channelUrl = $"https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/{channelVersion}/releases.json";
        var channelResponse = await httpClient.GetStringAsync(channelUrl);
        var channelData = JsonDocument.Parse(channelResponse);
        
        // Find the latest SDK version
        if (channelData.RootElement.TryGetProperty("releases", out var releases))
        {
            foreach (var release in releases.EnumerateArray())
            {
                if (release.TryGetProperty("sdk", out var sdk) && 
                    sdk.TryGetProperty("version", out var sdkVersion))
                {
                    return sdkVersion.GetString();
                }
            }
        }
        
        return null;
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

    private enum UpdateType
    {
        Patch,
        Minor,
        Major
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
    
    private sealed class UpdateSummary
    {
        public int Patch { get; set; }
        public int Minor { get; set; }
        public int Major { get; set; }
        public int Excluded { get; set; }
        public int UpToDate { get; set; }
        
        public void Reset()
        {
            Patch = 0;
            Minor = 0;
            Major = 0;
            Excluded = 0;
            UpToDate = 0;
        }
        
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
        
        public int TotalUpdates => Patch + Minor + Major;
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
            showBackend ? (BackendSummary.Major > 0 ? $"[yellow]{BackendSummary.Major}[/]" : "0") : "-",
            showFrontend ? (FrontendSummary.Major > 0 ? $"[yellow]{FrontendSummary.Major}[/]" : "0") : "-"
        );
        
        // Excluded
        if (hasExcluded)
        {
            table.AddRow(
                "[red]🚫 Excluded[/]", 
                showBackend ? (BackendSummary.Excluded > 0 ? $"[red]{BackendSummary.Excluded}[/]" : "0") : "-",
                showFrontend ? (FrontendSummary.Excluded > 0 ? $"[red]{FrontendSummary.Excluded}[/]" : "0") : "-"
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
    
    private sealed record PackageUpdate(XElement Element, string PackageName, string CurrentVersion, string NewVersion);
    private sealed record UpdateStatus(bool CanUpdate, bool IsRestricted, string TargetVersion);
    private sealed record VersionResolution(string? LatestVersion);
}