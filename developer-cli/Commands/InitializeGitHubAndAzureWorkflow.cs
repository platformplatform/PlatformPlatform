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
        EnsureAzureAndGitHubCliToolsAreInstalled();

        var gitHubUrl = GetGitHubUrl();

        ShowIntro(gitHubUrl);

        var subscription = GetAzureSubscription(skipAzureLogin);

        AnsiConsole.WriteLine(
            $"Azure subscription {subscription.Name} ({subscription.Id}) on Tenant {subscription.TenantId} selected.");

        // Ensure 'Microsoft.ContainerService' service provider is registered on Azure Subscription

        // Configuring Azure AD Service Principal for passwordless deployments using OpenID Connect and federated credentials

        //Grant subscription level 'Contributor' and 'User Access Administrator' role to the Infrastructure Service Principal

        // Configuring Azure AD 'Azure SQL Server Admins' Security Group

        // Configure GitHub secrets and variables

        return 0;
    }

    private void EnsureAzureAndGitHubCliToolsAreInstalled()
    {
        PrerequisitesChecker.CheckCommandLineTool("az", "Azure CLI", new Version(2, 55), true);
         PrerequisitesChecker.CheckCommandLineTool("gh", "GitHub CLI", new Version(2, 39), true);
    }

    private string GetGitHubUrl()
    {
        var gitRemotes = ProcessHelper.StartProcess(
            "git",
            "remote -v",
            Installation.Environment.SolutionFolder,
            printCommand: false,
            redirectOutput: true
        );

        var gitRemoteRegex = new Regex(@"(?<url>https://github\.com/.*\.git)");
        var gitRemoteMatch = gitRemoteRegex.Match(gitRemotes);
        if (!gitRemoteMatch.Success)
        {
            AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. This tool only works with GitHub remotes.[/]");
            Environment.Exit(0);
        }

        return gitRemoteMatch.Groups["url"].Value;
    }

    private void ShowIntro(string gitHubUrl)
    {
        var setupConfirmationPrompt =
            $"""

             [bold]This command will setup passwordless deployments from {gitHubUrl} to Azure.[/]

             If you continue this command will do the following:

             [bold]Collect data:[/]
             * Prompt you to login to Azure (using a browser)
             * Prompt you to select an Azure subscription
             * Prompt you to login to GitHub
             * Confirm before continuing

             [bold]Set up trust between Azure and GitHub:[/]
             * Ensure deployment of Azure Container Apps Environment is enabled on Azure Subscription
             * Configure a Service Principal for passwordless deployments using federated credentials (OpenID Connect)
             * Grant the Service Principal 'Contributor' and 'User Access Administrator' to the Azure Subscription
             * Create a new 'Azure SQL Server Admins' Active Directory Security Group and make the Service Principal owner
             * Configure GitHub Repository with info about Azure Tenant, Subscription, Service Principal, etc.

             [bold]Prerequisites:[/]
             * The user used to login to Azure must have Owner permissions on the Azure subscription
             * The user used to login to Azure must have permissions to create AD Security Groups and Service Principals
             * The GitHub user used to login must have Owner permissions on the GitHub repository

             [green]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm(setupConfirmationPrompt, false)) Environment.Exit(0);
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