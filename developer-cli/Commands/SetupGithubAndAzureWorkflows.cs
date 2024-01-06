using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Text.Json;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = System.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class SetupGithubAndAzureWorkflows : Command
{
    private static readonly JsonSerializerOptions? JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public SetupGithubAndAzureWorkflows() : base(
        "setup-github-and-azure-workflow",
        "Set up trust between Azure and GitHub for passwordless deployments using Azure Service Principals with OpenID (aka Federated Credentials)."
    )
    {
        var skipAzureLoginOption = new Option<bool>(["--skip-azure-login"], "Skip Azure login");

        AddOption(skipAzureLoginOption);

        Handler = CommandHandler.Create<bool>(Execute);
    }

    private int Execute(bool skipAzureLogin = false)
    {
        EnsureAzureAndGithubCliToolsAreInstalled();

        PrintHeader("Introduction");

        var githubInfo = GetGithubInfo();

        ShowIntroPrompt();

        PrintHeader("Collecting data");

        var subscription = GetAzureSubscription(skipAzureLogin);
        var (azureContainerRegistryName, azureContainerRegistryExists) = GetAzureContainerRegistryName(subscription);
        var servicePrincipalName = $"GitHub Azure - {githubInfo.OrganizationName} - {githubInfo.RepositoryName}";
        var existingServicePrincipalId = GetExistingServicePrincipalId(servicePrincipalName);

        LoginToGithub();

        PrintHeader("Confirm changes");

        ConfirmChangesPrompt(
            githubInfo,
            subscription,
            azureContainerRegistryName,
            azureContainerRegistryExists,
            servicePrincipalName,
            existingServicePrincipalId
        );

        PrintHeader("Configuring Azure and GitHub");

        PrepareSubscriptionForContainerAppsEnvironment(subscription.Id);

        var servicePrincipalId = existingServicePrincipalId ?? CreateServicePrincipal(servicePrincipalName);

        // Grant 'Contributor' and 'User Access Administrator' roles at the subscription level to the Infrastructure Service Principal

        // Configure Azure AD 'Azure SQL Server Admins' Security Group

        // Configure GitHub secrets and variables

        return 0;
    }

    private void EnsureAzureAndGithubCliToolsAreInstalled()
    {
        PrerequisitesChecker.CheckCommandLineTool("az", "Azure CLI", new Version(2, 55), true);
        PrerequisitesChecker.CheckCommandLineTool("gh", "GitHub CLI", new Version(2, 39), true);
    }

    private GithubInfo GetGithubInfo()
    {
        var gitRemotes = ProcessHelper.StartProcess(
            "git",
            "remote -v",
            Installation.Environment.SolutionFolder,
            printCommand: false,
            redirectOutput: true
        );

        var gitRemoteRegex = new Regex(@"(?<url>https://github\.com/.*\.git)");
        var gitRemoteMatches = gitRemoteRegex.Match(gitRemotes);
        if (!gitRemoteMatches.Success)
        {
            AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. This tool only works with GitHub remotes.[/]");
            Environment.Exit(0);
        }

        var githubUrl = gitRemoteMatches.Groups["url"].Value.Replace(".git", "");
        var githubOrganization = githubUrl.Split("/")[3];
        var githubRepositoryName = githubUrl.Split("/")[4];

        return new GithubInfo(githubOrganization, githubRepositoryName, githubUrl);
    }

    private void ShowIntroPrompt()
    {
        var setupIntroPrompt =
            """
            This command will configure passwordless deployments from GitHub to Azure. If you continue, this command will do the following:

            * Prompt you to log in to Azure and select a subscription
            * Prompt you to log in to GitHub
            * Confirm before you continue

            You need owner permissions on the Azure subscription and GitHub repository. Plus you need permissions to create Directory Groups and Service Principals in Microsoft Entra ID.

            [bold]Would you like to continue?[/]
            """;

        if (!AnsiConsole.Confirm(setupIntroPrompt, false)) Environment.Exit(0);

        AnsiConsole.WriteLine();
    }

    private Subscription GetAzureSubscription(bool skipAzureLogin)
    {
        // Both `az login` and `az account list` will return a JSON array of subscriptions
        var arguments = skipAzureLogin ? "account list" : "login";
        var subscriptionListJson =
            ProcessHelper.StartProcess("az", arguments, redirectOutput: true, printCommand: false);

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

        var title = "[bold]Please select an Azure subscription[/]";
        var selectedDisplayName = AnsiConsole.Prompt(new SelectionPrompt<string>()
            .Title($"{title}")
            .AddChoices(activeSubscriptions.Select(s => s.Name)));

        var selectedSubscriptions = activeSubscriptions.Where(s => s.Name == selectedDisplayName).ToArray();
        if (selectedSubscriptions.Length > 1)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Found two subscriptions with the name {selectedDisplayName}.");
            Environment.Exit(1);
        }

        var subscription = selectedSubscriptions.Single();
        ProcessHelper.StartProcess("az", $"account set --subscription {subscription.Id}", printCommand: false);

        AnsiConsole.MarkupLine($"{title}: {subscription.Name}\n");
        return subscription;
    }

    private (string, bool) GetAzureContainerRegistryName(Subscription azureSubscription)
    {
        var existingContainerRegistryName = Environment.GetEnvironmentVariable("CONTAINER_REGISTRY_NAME") ?? "";

        while (true)
        {
            var registryName = AnsiConsole.Ask("[bold]Please enter a unique name for the Azure Container Registry.[/]",
                existingContainerRegistryName);

            // Check whether the Azure Container Registry name is available
            var checkAvailability = ProcessHelper.StartProcess(
                "az",
                $"acr check-name --name {registryName}",
                redirectOutput: true,
                printCommand: false
            );

            var nameAvailable =
                JsonDocument.Parse(checkAvailability).RootElement.GetProperty("nameAvailable").GetBoolean();

            if (nameAvailable)
            {
                return (registryName, false);
            }

            // Check if the Azure Container Registry is a resource under the current subscription
            var showExistingRegistry = ProcessHelper.StartProcess(
                "az",
                $"acr show --name {registryName} --subscription {azureSubscription.Id}",
                redirectOutput: true,
                printCommand: false
            );

            var jsonRegex = new Regex(@"\{.*\}", RegexOptions.Singleline);
            var match = jsonRegex.Match(showExistingRegistry);

            if (match.Success)
            {
                var jsonDocument = JsonDocument.Parse(match.Value);
                if (jsonDocument.RootElement.GetProperty("id").GetString()?.Contains(azureSubscription.Id) == true)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow] The Azure Container Registry {registryName} exists on the selected subscription and will be reused.[/]");
                    return (registryName, true);
                }
            }

            AnsiConsole.MarkupLine(
                $"[red]ERROR:[/] The Azure Container Registry {registryName} is invalid or already exists. Please try again.");
        }
    }

    private string? GetExistingServicePrincipalId(string servicePrincipalName)
    {
        var existingServicePrincipalId = ProcessHelper.StartProcess(
            "az",
            $"""ad sp list --display-name "{servicePrincipalName}" --query "[].appId" -o tsv""",
            redirectOutput: true,
            printCommand: false
        ).Trim();

        var existingAppId = ProcessHelper.StartProcess(
            "az",
            $"""ad app list --display-name "{servicePrincipalName}" --query "[].appId" -o tsv""",
            redirectOutput: true,
            printCommand: false
        ).Trim();

        if (string.IsNullOrEmpty(existingServicePrincipalId) && string.IsNullOrEmpty(existingAppId)) return null;

        if (string.IsNullOrEmpty(existingServicePrincipalId))
        {
            AnsiConsole.MarkupLine(
                $"[red]The Service Principal '{servicePrincipalName}' exists but the App Registration does not. Please manually delete the Service Principle and retry.[/]");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(existingAppId))
        {
            AnsiConsole.MarkupLine(
                $"[red]The App Registration '{servicePrincipalName}' exists but the Service Principal does not. Please manually delete the App Registration and retry.[/]");
            Environment.Exit(1);
        }

        AnsiConsole.MarkupLine(
            $"[yellow]The Service Principal (App registration) '{servicePrincipalName}' already exists with App ID: {existingServicePrincipalId}[/]");

        if (!AnsiConsole.Confirm("The existing Service Principal will be reused. Do you want to continue?"))
        {
            AnsiConsole.MarkupLine("[red]Please delete the existing Service Principal and try again.[/]");
            Environment.Exit(1);
            return null;
        }

        return existingServicePrincipalId;
    }

    private void LoginToGithub()
    {
        ProcessHelper.StartProcess("gh", "auth login --git-protocol https --web", printCommand: false);
        var output = ProcessHelper.StartProcess("gh", "auth status", redirectOutput: true, printCommand: false);
        if (!output.Contains("Logged in to github.com")) Environment.Exit(0);
        AnsiConsole.WriteLine();
    }

    private void ConfirmChangesPrompt(
        GithubInfo githubInfo,
        Subscription subscription,
        string azureContainerRegistryName,
        bool azureContainerRegistryExists,
        string servicePrincipalName,
        string? existingServicePrincipalId
    )
    {
        var reuseContainerRegistry = azureContainerRegistryExists ? " - reuse existing Azure Container Registry" : "";
        var reuseServicePrinciple = existingServicePrincipalId is not null ? " - reuse existing service principle" : "";

        var setupConfirmPrompt =
            $"""
             * GitHub Organization: [blue]{githubInfo.OrganizationName}[/]
             * GitHub Repository Name: [blue]{githubInfo.RepositoryName}[/]
             * GitHub Repository URL: [blue]{githubInfo.GithubUrl}[/]
             * Azure Subscription: [blue]{subscription.Name} ({subscription.Id})[/]
             * Microsoft Entra ID Tenant ID: [blue]{subscription.TenantId}[/]
             * Azure Container Registry Name: [blue]{azureContainerRegistryName}[/]{reuseContainerRegistry}
             * Service Principal Name: [blue]{servicePrincipalName}[/]{reuseServicePrinciple}
             * SQL Admins Security Group Name: [blue]Azure SQL Server Admins[/]

             [bold]If you continue the following changes will be made:[/]
             1. Ensure deployment of Azure Container Apps Environment is enabled on Azure Subscription
             2. Create a Service Principal with federated credentials and trust deployments from Github
             3. Grant the Service Principal 'Contributor' and 'User Access Administrator' to the Azure Subscription
             4. Create a new 'Azure SQL Server Admins' Active Directory Security Group and make the Service Principal owner
             5. Configure GitHub Repository with info about Azure Tenant, Subscription, Service Principal, etc.

             After this setup you can deploy to Azure Container Apps Environment using GitHub Actions.

             [bold]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm($"{setupConfirmPrompt}", false)) Environment.Exit(0);
    }

    private void PrepareSubscriptionForContainerAppsEnvironment(string subscriptionId)
    {
        ProcessHelper.StartProcess(
            "az",
            $"provider register --namespace Microsoft.ContainerService --subscription {subscriptionId}",
            redirectOutput: true,
            printCommand: false
        );
    }

    private string CreateServicePrincipal(string servicePrincipalName)
    {
        var servicePrincipalId = ProcessHelper.StartProcess(
            "az",
            $"""ad app create --display-name "{servicePrincipalName}" --query appId -o tsv""",
            redirectOutput: true,
            printCommand: false
        ).Trim();

        ProcessHelper.StartProcess(
            "az",
            $"ad sp create --id {servicePrincipalId}",
            redirectOutput: true,
            printCommand: false
        );

        AnsiConsole.MarkupLine(
            $"Service Principle {servicePrincipalName} created successfully. Id: [blue]{servicePrincipalId}[/]");

        return servicePrincipalId;
    }

    private void PrintHeader(string heading)
    {
        var separator = new string('━', Console.WindowWidth - heading.Length - 1);
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }
}

[UsedImplicitly]
public record Subscription(string Id, string Name, string TenantId, string State);

public record GithubInfo(string OrganizationName, string RepositoryName, string GithubUrl);