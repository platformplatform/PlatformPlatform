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
        Handler = CommandHandler.Create<bool, bool, bool, bool>(Execute);
    }

    private static async Task Execute(bool backend, bool frontend, bool dryRun, bool build)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.Node);

        var updateBackend = backend || (!backend && !frontend);
        var updateFrontend = frontend || (!backend && !frontend);

        if (updateBackend)
        {
            await UpdateNuGetPackagesAsync(dryRun);

            if (build && !dryRun)
            {
                await RunBuildCommand(true, false);
            }
        }

        if (updateFrontend)
        {
            UpdateNpmPackages(dryRun);

            if (build && !dryRun)
            {
                await RunBuildCommand(false, true);
            }
        }
    }

    private static async Task UpdateNuGetPackagesAsync(bool dryRun)
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

        foreach (var packageElement in packageElements)
        {
            var packageName = packageElement.Attribute("Include")?.Value;
            var currentVersion = packageElement.Attribute("Version")?.Value;
            if (packageName is null || currentVersion is null)
            {
                continue;
            }

            var latestVersion = GetLatestVersionFromJson(outdatedPackagesJson, packageName);

            if (latestVersion is null)
            {
                continue;
            }

            var status = GetNuGetUpdateStatus(packageName, currentVersion, latestVersion);
            var statusColor = status.IsRestricted ? "[red]Restricted[/]" : "[yellow]Will update[/]";

            table.AddRow(packageName, currentVersion, latestVersion, statusColor);

            if (status.CanUpdate)
            {
                updates.Add(new PackageUpdate(packageElement, packageName, currentVersion, status.TargetVersion));
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
            if (AnsiConsole.Confirm($"Update {updates.Count} NuGet package(s)?"))
            {
                foreach (var update in updates)
                {
                    update.Element.SetAttributeValue("Version", update.NewVersion);
                    AnsiConsole.MarkupLine($"[green]Updated {update.PackageName} from {update.CurrentVersion} to {update.NewVersion}[/]");
                }

                xDocument.Save(directoryPackagesPath);
                AnsiConsole.MarkupLine("[green]Directory.Packages.props updated successfully![/]");
            }
        }
    }

    private static async Task<JsonDocument?> GetOutdatedPackagesJsonAsync()
    {
        var output = "";
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            output = ProcessHelper.StartProcess(
                "dotnet list package --outdated --format json",
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

    private static void UpdateNpmPackages(bool dryRun)
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

            table.AddRow(packageName, currentVersion, latestVersion, "[yellow]Will update[/]");
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
            if (AnsiConsole.Confirm($"Update {updates.Count} npm package(s)?"))
            {
                var updateCommand = $"npm install {string.Join(" ", updates)}";
                ProcessHelper.StartProcess(updateCommand, Configuration.ApplicationFolder);
                AnsiConsole.MarkupLine("[green]npm packages updated successfully![/]");
            }
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
}
