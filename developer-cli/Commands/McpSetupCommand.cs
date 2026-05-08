using System.CommandLine;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeveloperCli.Installation;
using DeveloperCli.Utilities;
using Spectre.Console;

namespace DeveloperCli.Commands;

public class McpSetupCommand : Command
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly JsonSerializerOptions JsonWriteOptions = new() { WriteIndented = true };

    public McpSetupCommand() : base(
        "mcp-setup",
        "Configure Claude Code MCP servers (currently: Azure Application Insights)"
    )
    {
        SetAction(_ => Execute());
    }

    private static void Execute()
    {
        Prerequisite.Ensure(Prerequisite.AzureCli, Prerequisite.Node);

        PrintHeader("Select MCP server");
        var serverType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold]Which MCP server do you want to set up?[/]")
                .AddChoices("Azure (Application Insights)")
        );

        if (serverType.StartsWith("Azure")) SetupAzure();
    }

    private static void SetupAzure()
    {
        PrintHeader("Azure setup");
        ShowAzureIntroPrompt();

        PrintHeader("Azure login");
        var subscriptions = LoadAzureSubscriptions();

        PrintHeader("Discovering Application Insights resources");
        var resources = DiscoverAppInsightsResources(subscriptions);
        if (resources.Length == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No Application Insights resources found across your accessible subscriptions.[/]");
            AnsiConsole.MarkupLine("[grey]If a resource exists, make sure your account has at least 'Reader' role on it.[/]");
            Environment.Exit(0);
        }

        PrintHeader("Select Application Insights resources");
        AnsiConsole.MarkupLine(
            """
            Pick one resource per environment. Recognized environment suffixes are
            [blue]stage[/]/[blue]staging[/] and [blue]prod[/]/[blue]production[/], matching the entries already
            committed in [blue].mcp.json[/].

            Subscription IDs are written to [blue].claude/settings.local.json[/] (gitignored, your
            machine only). All selections must be from the same Azure tenant.
            """
        );
        AnsiConsole.WriteLine();

        var selected = AnsiConsole.Prompt(
            new MultiSelectionPrompt<AppInsightsResource>()
                .Title("[bold]Select Application Insights resources to expose to Claude Code:[/]")
                .Required()
                .PageSize(20)
                .MoreChoicesText("[grey](Move up and down to reveal more)[/]")
                .InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
                .UseConverter(r =>
                    $"[white]{Markup.Escape(r.Name)}[/]  [grey]subscription=[/]{Markup.Escape(r.SubscriptionName)}  [grey]resourceGroup=[/]{Markup.Escape(r.ResourceGroup)}  [grey]tenant=[/]{Markup.Escape(r.TenantDisplay)}"
                )
                .AddChoices(resources)
        );

        var distinctTenants = selected.Select(r => r.TenantId).Distinct().ToArray();
        if (distinctTenants.Length > 1)
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] Please pick resources from a single tenant per setup run.");
            AnsiConsole.MarkupLine($"[grey]Selected tenants: {string.Join(", ", distinctTenants)}[/]");
            Environment.Exit(1);
        }

        var picks = new Dictionary<string, AppInsightsResource>();
        foreach (var resource in selected)
        {
            var environmentName = MapToEnvironmentName(resource.Name);
            if (environmentName is null)
            {
                AnsiConsole.MarkupLine($"[red]ERROR:[/] Cannot map '[yellow]{resource.Name}[/]' to a known environment.");
                AnsiConsole.MarkupLine("[grey]Resource names must end in '-stage', '-staging', '-prod', or '-production'.[/]");
                Environment.Exit(1);
            }

            if (picks.TryGetValue(environmentName, out var existingPick))
            {
                AnsiConsole.MarkupLine($"[red]ERROR:[/] Multiple resources map to environment [yellow]{environmentName}[/]: '{existingPick.Name}' and '{resource.Name}'.");
                AnsiConsole.MarkupLine("[grey]Pick at most one resource per environment.[/]");
                Environment.Exit(1);
            }

            picks[environmentName] = resource;
        }

        var tenantId = distinctTenants[0];

        PrintHeader("Writing local MCP env values");
        WriteSettingsLocalJson(picks, tenantId);

        PrintHeader("Done");
        AnsiConsole.MarkupLine("[green]MCP env values written to .claude/settings.local.json. Restart Claude Code to pick them up.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[grey]Future worktrees of this clone will inherit settings.local.json via the [blue]post-checkout[/] hook installed by [blue]{Configuration.AliasName} install[/].[/]");
    }

    private static AzureAccount[] LoadAzureSubscriptions()
    {
        var json = RunAz("account list -o json", true, false);
        if (string.IsNullOrWhiteSpace(json) || !json.TrimStart().StartsWith('['))
        {
            AnsiConsole.MarkupLine("[blue]Logging in to Azure...[/]");
            RunAz("login", false);
            json = RunAz("account list -o json", true);
        }

        var accounts = JsonSerializer.Deserialize<AzureAccount[]>(json, JsonOptions);
        if (accounts is null || accounts.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] No Azure subscriptions found.");
            Environment.Exit(1);
        }

        var enabled = accounts.Where(a => a.State == "Enabled").ToArray();
        if (enabled.Length == 0)
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] No enabled Azure subscriptions found.");
            Environment.Exit(1);
        }

        AnsiConsole.MarkupLine($"[green]Found {enabled.Length} enabled subscription(s) across {enabled.Select(a => a.TenantId).Distinct().Count()} tenant(s).[/]");
        return enabled;
    }

    private static AppInsightsResource[] DiscoverAppInsightsResources(AzureAccount[] subscriptions)
    {
        var results = new List<AppInsightsResource>();

        AnsiConsole.Progress()
            .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
            .Start(c =>
                {
                    var task = c.AddTask($"[blue]Listing Application Insights across {subscriptions.Length} subscription(s)[/]", maxValue: subscriptions.Length);
                    foreach (var subscription in subscriptions)
                    {
                        var json = RunAz(
                            $"resource list --subscription {subscription.Id} --resource-type microsoft.insights/components -o json",
                            true,
                            false
                        );

                        if (!string.IsNullOrWhiteSpace(json) && json.TrimStart().StartsWith('['))
                        {
                            var rawResources = JsonSerializer.Deserialize<RawResource[]>(json, JsonOptions);
                            if (rawResources is not null)
                            {
                                foreach (var rawResource in rawResources)
                                {
                                    results.Add(new AppInsightsResource(
                                            rawResource.Name,
                                            rawResource.ResourceGroup,
                                            subscription.Id,
                                            subscription.Name,
                                            subscription.TenantId,
                                            subscription.TenantDefaultDomain ?? subscription.TenantId
                                        )
                                    );
                                }
                            }
                        }

                        task.Increment(1);
                    }
                }
            );

        return results.OrderBy(r => r.TenantDisplay).ThenBy(r => r.SubscriptionName).ThenBy(r => r.Name).ToArray();
    }

    private static void WriteSettingsLocalJson(Dictionary<string, AppInsightsResource> picks, string tenantId)
    {
        var path = Path.Combine(Configuration.SourceCodeFolder, ".claude", "settings.local.json");
        var root = LoadOrCreateJsonObject(path);

        var env = root["env"] as JsonObject;
        if (env is null)
        {
            env = new JsonObject();
            root["env"] = env;
        }

        env["AZURE_TENANT_ID"] = tenantId;
        foreach (var (environmentName, resource) in picks)
        {
            env[$"AZURE_SUBSCRIPTION_ID_{environmentName}"] = resource.SubscriptionId;
        }

        File.WriteAllText(path, root.ToJsonString(JsonWriteOptions));
        AnsiConsole.MarkupLine($"[green]Wrote .claude/settings.local.json with {picks.Count} subscription value(s).[/]");
    }

    private static JsonObject LoadOrCreateJsonObject(string path)
    {
        if (!File.Exists(path))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return new JsonObject();
        }

        var content = File.ReadAllText(path);
        if (string.IsNullOrWhiteSpace(content)) return new JsonObject();

        var parsed = JsonNode.Parse(content);
        return parsed as JsonObject ?? new JsonObject();
    }

    private static string? MapToEnvironmentName(string aiResourceName)
    {
        var lastDash = aiResourceName.LastIndexOf('-');
        var suffix = lastDash >= 0 ? aiResourceName[(lastDash + 1)..] : aiResourceName;
        return suffix.ToLowerInvariant() switch
        {
            "stage" or "staging" => "STAGING",
            "prod" or "production" => "PRODUCTION",
            _ => null
        };
    }

    private static string RunAz(string arguments, bool redirectOutput, bool exitOnError = true)
    {
        return ProcessHelper.StartProcess(
            new ProcessStartInfo
            {
                FileName = Configuration.IsWindows ? "cmd.exe" : "az",
                Arguments = Configuration.IsWindows ? $"/C az {arguments}" : arguments,
                RedirectStandardOutput = redirectOutput,
                RedirectStandardError = redirectOutput,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            exitOnError: exitOnError
        );
    }

    private static void PrintHeader(string heading)
    {
        var separator = new string('-', Math.Max(0, Console.WindowWidth - heading.Length - 1));
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }

    private static void ShowAzureIntroPrompt()
    {
        var prompt =
            """
            Setting up the Azure MCP server lets Claude Code query Azure resources from your local machine.

            This command will:
             * Run [blue]az login[/] (if you are not already signed in)
             * Discover Application Insights resources you have access to across all subscriptions
             * Let you pick which environment(s) to wire up (staging / production)
             * Write the actual tenant + subscription IDs to [blue].claude/settings.local.json[/] (gitignored, your machine only)

            After completion, restart Claude Code to load the new servers.

            [bold]Continue?[/]
            """;

        if (!AnsiConsole.Confirm(prompt.Replace("\n\n", "\n"))) Environment.Exit(0);
        AnsiConsole.WriteLine();
    }
}

internal sealed record AzureAccount(string Id, string Name, string TenantId, string? TenantDefaultDomain, string State);

internal sealed record AppInsightsResource(
    string Name,
    string ResourceGroup,
    string SubscriptionId,
    string SubscriptionName,
    string TenantId,
    string TenantDisplay
);

internal sealed record RawResource(string Name, string ResourceGroup, string Location);
