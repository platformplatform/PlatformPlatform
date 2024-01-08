using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
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

        AzureInfo azureInfo = new();
        azureInfo.Subscription = GetAzureSubscription(skipAzureLogin);
        azureInfo.ServicePrincipalName = $"GitHub Azure - {githubInfo.OrganizationName} - {githubInfo.RepositoryName}";
        PublishExistingServicePrincipalId(azureInfo);
        PublishExistingSqlAdminSecurityGroup(azureInfo);
        azureInfo.ContainerRegistry = GetAzureContainerRegistryName(azureInfo.Subscription);

        LoginToGithub();

        PrintHeader("Confirm changes");

        ConfirmChangesPrompt(githubInfo, azureInfo);

        PrintHeader("Configuring Azure and GitHub");

        PrepareSubscriptionForContainerAppsEnvironment(azureInfo.Subscription.Id);

        if (!azureInfo.ServicePrincipalExists)
        {
            CreateServicePrincipal(azureInfo);
        }

        CreateFederatedCredentials(azureInfo, githubInfo);

        GrantSubscriptionPermissionsForServicePrincipal(azureInfo);

        if (!azureInfo.SqlAdminsSecurityGroupExists)
        {
            CreateAzureSqlServerSecurityGroup(azureInfo);
        }

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
        var gitRemotes = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "git",
            Arguments = "remote -v",
            WorkingDirectory = Installation.Environment.SolutionFolder,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false);

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
        var subscriptionListJson = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = skipAzureLogin ? "account list" : "login",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false);

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
        ProcessHelper.StartProcess(
            new ProcessStartInfo { FileName = "az", Arguments = $"account set --subscription {subscription.Id}" },
            printCommand: false
        );

        AnsiConsole.MarkupLine($"{title}: {subscription.Name}\n");
        return subscription;
    }

    private void PublishExistingServicePrincipalId(AzureInfo azureInfo)
    {
        var existingServicePrincipalId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad sp list --display-name "{azureInfo.ServicePrincipalName}" --query "[].appId" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false).Trim();

        var existingServicePrincipalAppId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad app list --display-name "{azureInfo.ServicePrincipalName}" --query "[].appId" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false).Trim();

        if (existingServicePrincipalId != string.Empty && existingServicePrincipalAppId != string.Empty)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]The Service Principal (App registration) '{azureInfo.ServicePrincipalName}' already exists with App ID: {existingServicePrincipalId}[/]");

            if (AnsiConsole.Confirm("The existing Service Principal will be reused. Do you want to continue?"))
            {
                AnsiConsole.WriteLine();
                azureInfo.ServicePrincipalExists = true;
                azureInfo.ServicePrincipalId = existingServicePrincipalId;
                azureInfo.ServicePrincipalAppId = existingServicePrincipalAppId;
                return;
            }

            AnsiConsole.MarkupLine("[red]Please delete the existing Service Principal and try again.[/]");
            Environment.Exit(1);
        }

        if (!string.IsNullOrEmpty(existingServicePrincipalId))
        {
            AnsiConsole.MarkupLine(
                $"[red]The Service Principal '{azureInfo.ServicePrincipalName}' exists but the App Registration does not. Please manually delete the Service Principle and retry.[/]");
            Environment.Exit(1);
        }

        if (!string.IsNullOrEmpty(existingServicePrincipalAppId))
        {
            AnsiConsole.MarkupLine(
                $"[red]The App Registration '{azureInfo.ServicePrincipalName}' exists but the Service Principal does not. Please manually delete the App Registration and retry.[/]");
            Environment.Exit(1);
        }
    }

    private void PublishExistingSqlAdminSecurityGroup(AzureInfo azureInfo)
    {
        var existingSqlAdminSecurityGroupId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad group list --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --query "[].id" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false).Trim();

        if (existingSqlAdminSecurityGroupId != string.Empty)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]The Azure AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' already exists with ID: {existingSqlAdminSecurityGroupId}[/]");

            if (AnsiConsole.Confirm("The existing Azure AD Security Group will be reused. Do you want to continue?"))
            {
                AnsiConsole.WriteLine();
                azureInfo.SqlAdminsSecurityGroupExists = true;
                azureInfo.SqlAdminsSecurityGroupId = existingSqlAdminSecurityGroupId;
                return;
            }

            AnsiConsole.MarkupLine("[red]Please delete the existing Azure AD Security Group and try again.[/]");
            Environment.Exit(1);
        }
    }

    private ContainerRegistry GetAzureContainerRegistryName(Subscription azureSubscription)
    {
        var existingContainerRegistryName = Environment.GetEnvironmentVariable("CONTAINER_REGISTRY_NAME") ?? "";

        while (true)
        {
            var registryName = AnsiConsole.Ask("[bold]Please enter a unique name for the Azure Container Registry.[/]",
                existingContainerRegistryName);

            //  Check whether the Azure Container Registry name is available
            var checkAvailability = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "az",
                Arguments = $"acr check-name --name {registryName}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }, printCommand: false);

            var nameAvailable =
                JsonDocument.Parse(checkAvailability).RootElement.GetProperty("nameAvailable").GetBoolean();

            if (nameAvailable)
            {
                AnsiConsole.WriteLine();
                return new ContainerRegistry(registryName, false);
            }

            // Checks if the Azure Container Registry is a resource under the current subscription
            var showExistingRegistry = ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "az",
                Arguments = $"acr show --name {registryName} --subscription {azureSubscription.Id}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }, printCommand: false);

            var jsonRegex = new Regex(@"\{.*\}", RegexOptions.Singleline);
            var match = jsonRegex.Match(showExistingRegistry);

            if (match.Success)
            {
                var jsonDocument = JsonDocument.Parse(match.Value);
                if (jsonDocument.RootElement.GetProperty("id").GetString()?.Contains(azureSubscription.Id) == true)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow] The Azure Container Registry {registryName} exists on the selected subscription and will be reused.[/]");
                    return new ContainerRegistry(registryName, true);
                }
            }

            AnsiConsole.MarkupLine(
                $"[red]ERROR:[/] The Azure Container Registry {registryName} is invalid or already exists. Please try again.");
        }
    }

    private void LoginToGithub()
    {
        ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth login --git-protocol https --web"
            }, printCommand: false
        );

        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = "auth status",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false);

        if (!output.Contains("Logged in to github.com")) Environment.Exit(0);
        AnsiConsole.WriteLine();
    }

    private void ConfirmChangesPrompt(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        var reuseContainerRegistry =
            azureInfo.ContainerRegistry.Exists ? " - reuse existing Azure Container Registry" : "";
        var reuseServicePrinciple = azureInfo.ServicePrincipalExists ? " - reuse existing service principle" : "";

        var setupConfirmPrompt =
            $"""
             * GitHub Organization: [blue]{githubInfo.OrganizationName}[/]
             * GitHub Repository Name: [blue]{githubInfo.RepositoryName}[/]
             * GitHub Repository URL: [blue]{githubInfo.GithubUrl}[/]
             * Azure Subscription: [blue]{azureInfo.Subscription.Name} ({azureInfo.Subscription.Id})[/]
             * Microsoft Entra ID Tenant ID: [blue]{azureInfo.Subscription.TenantId}[/]
             * Azure Container Registry Name: [blue]{azureInfo.ContainerRegistry.Name}[/]{reuseContainerRegistry}
             * Service Principal Name: [blue]{azureInfo.ServicePrincipalName}[/]{reuseServicePrinciple}
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
        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"provider register --namespace Microsoft.ContainerService --subscription {subscriptionId}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false);

        AnsiConsole.MarkupLine(
            "[green]Successfully ensured deployment of Azure Container Apps Environment is enabled on Azure Subscription.[/]");
    }

    private void CreateServicePrincipal(AzureInfo azureInfo)
    {
        azureInfo.ServicePrincipalId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad app create --display-name "{azureInfo.ServicePrincipalName}" --query appId -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false).Trim();

        azureInfo.ServicePrincipalAppId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"ad sp create --id {azureInfo.ServicePrincipalId}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false);

        azureInfo.ServicePrincipalObjectId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad sp list --filter "appId eq '{azureInfo.ServicePrincipalAppId}'" --query "[].objectId" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false).Trim();

        AnsiConsole.MarkupLine(
            $"[green]Successfully create a Service Principal {azureInfo.ServicePrincipalName} ({azureInfo.ServicePrincipalId}).[/]");
    }

    private void CreateFederatedCredentials(AzureInfo azureInfo, GithubInfo githubInfo)
    {
        CreateFederatedCredential("MainBranch", "ref:refs/heads/main");
        CreateFederatedCredential("PullRequests", "pull_request");
        CreateFederatedCredential("SharedEnvironment", "environment:shared");
        CreateFederatedCredential("StagingEnvironment", "environment:staging");
        CreateFederatedCredential("ProductionEnvironment", "environment:production");

        AnsiConsole.MarkupLine(
            $"[green]Successfully created Federated Credentials allowing passwordless deployments from {githubInfo.GithubUrl}.[/]");

        void CreateFederatedCredential(string displayName, string refRefsHeadsMain)
        {
            var parameters = JsonSerializer.Serialize(new
            {
                name = displayName,
                issuer = "https://token.actions.githubusercontent.com",
                subject = $"repo:{githubInfo.GithubUrl}:{refRefsHeadsMain}",
                audiences = new[] { "api://AzureADTokenExchange" }
            });

            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "az",
                Arguments =
                    $@"ad app federated-credential create --id {azureInfo.ServicePrincipalAppId} --parameters  @-",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }, printCommand: false, input: parameters);
        }
    }

    private void GrantSubscriptionPermissionsForServicePrincipal(AzureInfo azureInfo)
    {
        GrantAccess("Contributor");
        GrantAccess("User Access Administrator");
        GrantAccess("AcrPush");

        AnsiConsole.MarkupLine(
            "[green]Successfully granted the Service Principal 'Contributor' and `User Access Administrator` rights to the Azure Subscription.[/]");

        void GrantAccess(string role)
        {
            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "az",
                Arguments =
                    $"role assignment create --assignee {azureInfo.ServicePrincipalId} --role \"{role}\" --scope /subscriptions/{azureInfo.Subscription.Id}",
                RedirectStandardOutput = true,
                RedirectStandardError = true
            }, printCommand: false);
        }
    }

    private void CreateAzureSqlServerSecurityGroup(AzureInfo azureInfo)
    {
        azureInfo.SqlAdminsSecurityGroupId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad group create --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --mail-nickname "AzureSQLServerAdmins" --query "id" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false).Trim();
        
        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"ad group member add --group {azureInfo.SqlAdminsSecurityGroupId} --member-id {azureInfo.ServicePrincipalObjectId}",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }, printCommand: false);
        
        AnsiConsole.MarkupLine(
            $"[green]Successfully created Azure AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' and granted the Service Principal {azureInfo.ServicePrincipalName} owner.[/]");
    }

    private void PrintHeader(string heading)
    {
        var separator = new string('━', Console.WindowWidth - heading.Length - 1);
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }
}

public record GithubInfo(string OrganizationName, string RepositoryName, string GithubUrl);

public class AzureInfo
{
    public Subscription Subscription { get; set; } = default!;

    public ContainerRegistry ContainerRegistry { get; set; } = default!;

    public string ServicePrincipalName { get; set; } = default!;

    public object ServicePrincipalId { get; set; } = default!;

    public string ServicePrincipalAppId { get; set; } = default!;
    
    public string ServicePrincipalObjectId { get; set; } = default!;

    public bool ServicePrincipalExists { get; set; }

    public string SqlAdminsSecurityGroupName { get; } = "Azure SQL Server Admins";
    
    public string SqlAdminsSecurityGroupNickName { get; } = "AzureSQLServerAdmins";

    public string SqlAdminsSecurityGroupId { get; set; } = default!;

    public bool SqlAdminsSecurityGroupExists { get; set; }
}

[UsedImplicitly]
public record Subscription(string Id, string Name, string TenantId, string State);

public record ContainerRegistry(string Name, bool Exists);