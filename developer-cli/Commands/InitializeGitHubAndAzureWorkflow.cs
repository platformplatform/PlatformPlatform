using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = System.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class InitializeGitHubAndAzureWorkflow : Command
{
    private static readonly JsonSerializerOptions? JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public InitializeGitHubAndAzureWorkflow() : base(
        "initialize-github-and-azure-workflow",
        "Set up trust between Azure and GitHub for passwordless deployments using Azure Service Principals with OpenID (aka. Federated Credentials)."
    )
    {
        var skipAzureLoginOption = new Option<bool>(["--skip-azure-login"], "Skip Azure login");

        AddOption(skipAzureLoginOption);

        Handler = CommandHandler.Create<bool>(Execute);
    }

    private int Execute(bool skipAzureLogin = false)
    {
        var subscription = GetAzureSubscription(skipAzureLogin);

        AnsiConsole.WriteLine(
            $"Azure subscription {subscription.Name} ({subscription.Id}) on Tenant {subscription.TenantId} selected.");

        // Prompting GitHub Repository

        // Ensure 'Microsoft.ContainerService' service provider is registered on Azure Subscription

        // Configuring Azure AD Service Principal for passwordless deployments using OpenID Connect and federated credentials

        //Grant subscription level 'Contributor' and 'User Access Administrator' role to the Infrastructure Service Principal

        // Configuring Azure AD 'Azure SQL Server Admins' Security Group

        // Configure GitHub secrets and variables

        return 0;
    }

    private Subscription GetAzureSubscription(bool skipAzureLogin)
    {
        // Both `az login` and `az account list` will return a JSON array of subscriptions
        var accountList = skipAzureLogin ? "account list" : "login";
        var subscriptionListJson = ProcessHelper.StartProcess("az", accountList, redirectOutput: true);

        // Regular expression to match JSON part
        var jsonRegex = new Regex(@"\[.*\]", RegexOptions.Singleline);
        var match = jsonRegex.Match(subscriptionListJson);

        List<Subscription>? azureSubscriptions = null;
        if (match.Success)
        {
            azureSubscriptions = JsonSerializer.Deserialize<List<Subscription>>(match.Value, JsonSerializerOptions);
        }

        if (azureSubscriptions == null)
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] No subscriptions found.");
            Environment.Exit(1);
        }

        var activeSubscriptions = azureSubscriptions.Where(s => s.State == "Enabled").ToList();

        var selectedDisplayName = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title("Please select an Azure subscription")
            .AddChoices(activeSubscriptions.Select(s => s.Name)));

        var selectedSubscriptions = activeSubscriptions.Where(s => s.Name == selectedDisplayName).ToArray();
        if (selectedSubscriptions.Length > 1)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Found two subscription with the name {selectedDisplayName}.");
            Environment.Exit(1);
        }

        return selectedSubscriptions.Single();
    }
}

[UsedImplicitly]
public record Subscription(string Id, string Name, string TenantId, string State);