using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;
using Environment = PlatformPlatform.DeveloperCli.Installation.Environment;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class SetupGithubAndAzureWorkflows : Command
{
    private static readonly JsonSerializerOptions? JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public SetupGithubAndAzureWorkflows() : base(
        "setup-github-and-azure-workflow",
        "Set up trust between Azure and GitHub for passwordless deployments using Azure App Registration with OpenID (aka Federated Credentials)."
    )
    {
        AddOption(new Option<bool>(["--skip-azure-login"], "Skip Azure login"));
        AddOption(new Option<bool>(["--verbose-logging"], "Print Azure and Github CLI commands and output"));

        Handler = CommandHandler.Create<bool, bool>(Execute);
    }

    private int Execute(bool skipAzureLogin = false, bool verboseLogging = false)
    {
        Environment.VerboseLogging = verboseLogging;

        EnsureAzureAndGithubCliToolsAreInstalled();

        PrintHeader("Introduction");

        var githubInfo = GetGithubInfo();

        ShowIntroPrompt();

        PrintHeader("Collecting data");

        AzureInfo azureInfo = new();
        azureInfo.Subscription = GetAzureSubscription(skipAzureLogin);
        azureInfo.AppRegistrationName = $"GitHub Azure - {githubInfo.OrganizationName} - {githubInfo.RepositoryName}";
        PublishExistingAppRegistration(azureInfo);
        PublishExistingSqlAdminSecurityGroup(azureInfo);
        azureInfo.ContainerRegistry = GetAzureContainerRegistryName(azureInfo.Subscription, githubInfo);

        LoginToGithub();

        PrintHeader("Confirm changes");

        ConfirmChangesPrompt(githubInfo, azureInfo);

        PrintHeader("Configuring Azure and GitHub");

        PrepareSubscriptionForContainerAppsEnvironment(azureInfo.Subscription.Id);

        if (!azureInfo.AppRegistrationExists)
        {
            CreateAppRegistration(azureInfo);
        }

        CreateFederatedCredentials(azureInfo, githubInfo);

        GrantSubscriptionPermissionsToServicePrincipal(azureInfo);

        CreateAzureSqlServerSecurityGroup(azureInfo);

        CreateGithubSecretsAndVariables(githubInfo, azureInfo);

        return 0;
    }

    private void EnsureAzureAndGithubCliToolsAreInstalled()
    {
        PrerequisitesChecker.CheckCommandLineTool("az", "Azure CLI", new Version(2, 55), true);
        PrerequisitesChecker.CheckCommandLineTool("gh", "GitHub CLI", new Version(2, 39), true);
    }

    private GithubInfo GetGithubInfo()
    {
        var gitRemotes = ProcessHelper.StartProcess("git remote -v", Environment.SolutionFolder, true);

        var gitRemoteRegex = new Regex(@"(?<url>https://github\.com/.*\.git)");
        var gitRemoteMatches = gitRemoteRegex.Match(gitRemotes);
        if (!gitRemoteMatches.Success)
        {
            AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. This tool only works with GitHub remotes.[/]");
            System.Environment.Exit(0);
        }

        var githubInfo = new GithubInfo(gitRemoteMatches.Groups["url"].Value);

        var githubVariablesJson = ProcessHelper.StartProcess(
            $"gh api repos/{githubInfo.Path}/actions/variables", redirectOutput: true);

        var githubVariables = JsonDocument.Parse(githubVariablesJson);
        foreach (var variable in githubVariables.RootElement.GetProperty("variables").EnumerateArray())
        {
            var variableName = variable.GetProperty("name").GetString()!;
            var variableValue = variable.GetProperty("value").GetString()!;
            githubInfo.Variables.Add(variableName, variableValue);
        }

        return githubInfo;
    }

    private void ShowIntroPrompt()
    {
        var setupIntroPrompt =
            """
            This command will configure passwordless deployments from GitHub to Azure. If you continue, this command will do the following:

            * Prompt you to log in to Azure and select a subscription
            * Prompt you to log in to GitHub
            * Confirm before you continue

            You need owner permissions on the Azure subscription and GitHub repository. Plus you need permissions to create Directory Groups and App Registrations (aka Service Principals) in Microsoft Entra ID.

            [bold]Would you like to continue?[/]
            """;

        if (!AnsiConsole.Confirm(setupIntroPrompt, false)) System.Environment.Exit(0);

        AnsiConsole.WriteLine();
    }

    private Subscription GetAzureSubscription(bool skipAzureLogin)
    {
        // Both `az login` and `az account list` will return a JSON array of subscriptions
        var subscriptionListJson =
            ProcessHelper.StartProcess($"az {(skipAzureLogin ? "account list" : "login")}", redirectOutput: true);

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
            System.Environment.Exit(1);
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
            System.Environment.Exit(1);
        }

        var subscription = selectedSubscriptions.Single();
        ProcessHelper.StartProcess($"az account set --subscription {subscription.Id}");

        AnsiConsole.MarkupLine($"{title}: {subscription.Name}\n");
        return subscription;
    }

    private void PublishExistingAppRegistration(AzureInfo azureInfo)
    {
        azureInfo.AppRegistrationId = ProcessHelper.StartProcess(
            $"""az ad app list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv""",
            redirectOutput: true
        ).Trim();

        azureInfo.ServicePrincipalId = ProcessHelper.StartProcess(
            $"""az ad sp list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv""",
            redirectOutput: true
        ).Trim();

        azureInfo.ServicePrincipalObjectId = ProcessHelper.StartProcess(
            $"""az ad sp list --filter "appId eq '{azureInfo.AppRegistrationId}'" --query "[].id" -o tsv""",
            redirectOutput: true
        ).Trim();

        if (azureInfo.AppRegistrationId != string.Empty && azureInfo.ServicePrincipalId != string.Empty)
        {
            azureInfo.AppRegistrationExists = true;
            AnsiConsole.MarkupLine(
                $"[yellow]The App Registration '{azureInfo.AppRegistrationName}' already exists with App ID: {azureInfo.ServicePrincipalId}[/]");

            if (AnsiConsole.Confirm("The existing App Registration will be reused. Do you want to continue?"))
            {
                AnsiConsole.WriteLine();
                return;
            }

            AnsiConsole.MarkupLine("[red]Please delete the existing App Registration and try again.[/]");
            System.Environment.Exit(1);
        }

        if (azureInfo.AppRegistrationId != string.Empty || azureInfo.ServicePrincipalId != string.Empty)
        {
            AnsiConsole.MarkupLine(
                $"[red]The App Registration or Service Principal '{azureInfo.AppRegistrationName}' exists but not both. Please manually delete and retry.[/]");
            System.Environment.Exit(1);
        }
    }

    private void PublishExistingSqlAdminSecurityGroup(AzureInfo azureInfo)
    {
        azureInfo.SqlAdminsSecurityGroupId = ProcessHelper.StartProcess(
            $"""az ad group list --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --query "[].id" -o tsv""",
            redirectOutput: true
        ).Trim();

        if (azureInfo.SqlAdminsSecurityGroupId == string.Empty) return;

        AnsiConsole.MarkupLine(
            $"[yellow]The AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' already exists with ID: {azureInfo.SqlAdminsSecurityGroupId}[/]");

        if (AnsiConsole.Confirm("The existing AD Security Group will be reused. Do you want to continue?"))
        {
            AnsiConsole.WriteLine();
            azureInfo.SqlAdminsSecurityGroupExists = true;
            return;
        }

        AnsiConsole.MarkupLine("[red]Please delete the existing AD Security Group and try again.[/]");
        System.Environment.Exit(1);
    }

    private ContainerRegistry GetAzureContainerRegistryName(Subscription azureSubscription, GithubInfo githubInfo)
    {
        var existingContainerRegistryName = githubInfo.Variables["CONTAINER_REGISTRY_NAME"];

        while (true)
        {
            var registryName = AnsiConsole.Ask("[bold]Please enter a unique name for the Azure Container Registry.[/]",
                existingContainerRegistryName);

            //  Check whether the Azure Container Registry name is available
            var checkAvailability =
                ProcessHelper.StartProcess($"az acr check-name --name {registryName}", redirectOutput: true);

            if (JsonDocument.Parse(checkAvailability).RootElement.GetProperty("nameAvailable").GetBoolean())
            {
                AnsiConsole.WriteLine();
                return new ContainerRegistry(registryName, false);
            }

            // Checks if the Azure Container Registry is a resource under the current subscription
            var showExistingRegistry = ProcessHelper.StartProcess(
                $"az acr show --name {registryName} --subscription {azureSubscription.Id}", redirectOutput: true);

            var jsonRegex = new Regex(@"\{.*\}", RegexOptions.Singleline);
            var match = jsonRegex.Match(showExistingRegistry);

            if (match.Success)
            {
                var jsonDocument = JsonDocument.Parse(match.Value);
                if (jsonDocument.RootElement.GetProperty("id").GetString()?.Contains(azureSubscription.Id) == true)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]The Azure Container Registry {registryName} exists on the selected subscription and will be reused.[/]");
                    AnsiConsole.WriteLine();
                    return new ContainerRegistry(registryName, true);
                }
            }

            AnsiConsole.MarkupLine(
                $"[red]ERROR:[/]The Azure Container Registry {registryName} is invalid or already exists. Please try again.");
        }
    }

    private void LoginToGithub()
    {
        ProcessHelper.StartProcess("gh auth login --git-protocol https --web");

        var output = ProcessHelper.StartProcess("gh auth status", redirectOutput: true);

        if (!output.Contains("Logged in to github.com")) System.Environment.Exit(0);
        AnsiConsole.WriteLine();
    }

    private void ConfirmChangesPrompt(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        var appRegistrationInto = azureInfo.AppRegistrationExists ? "Update" : "Create";
        var reuseSqlAdminsSecurityGroupIntro = azureInfo.SqlAdminsSecurityGroupExists ? "Reuse" : "Create";

        var setupConfirmPrompt =
            $"""
             [bold]If you continue the following changes will be made:[/]

             1. Ensure deployment of Azure Container Apps Environment is enabled on Azure Subscription: [blue]{azureInfo.Subscription.Name} ({azureInfo.Subscription.Id})[/]

             2. {appRegistrationInto} [blue]{azureInfo.AppRegistrationName}[/] App Registration with Federated Credentials and trust deployments from Github

             3. Grant the App Registration 'Contributor' and 'User Access Administrator' to the Azure Subscription

             4. {reuseSqlAdminsSecurityGroupIntro} [blue]{azureInfo.SqlAdminsSecurityGroupName}[/] AD Security Group and make the App Registration owner

             5. Configure GitHub Repository [blue]{githubInfo.GithubUrl}[/] with the following
             
                GitHub Secrets:
                * AZURE_TENANT_ID: [blue]{azureInfo.Subscription.TenantId}[/]
                * AZURE_SUBSCRIPTION_ID: [blue]{azureInfo.Subscription.Id}[/]
                * AZURE_SERVICE_PRINCIPAL_ID: [blue]{azureInfo.AppRegistrationId}[/]
                * ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID: [blue]{azureInfo.SqlAdminsSecurityGroupId}[/]
                
                GitHub Variables:
                * CONTAINER_REGISTRY_NAME: [blue]{azureInfo.ContainerRegistry.Name}[/]
                * UNIQUE_CLUSTER_PREFIX: [blue]{githubInfo.Variables["UNIQUE_CLUSTER_PREFIX"]}[/]
                * DOMAIN_NAME_STAGING: [blue]{githubInfo.Variables["DOMAIN_NAME_STAGING"]}[/]
                * DOMAIN_NAME_PRODUCTION: [blue]{githubInfo.Variables["DOMAIN_NAME_PRODUCTION"]}[/]

             After this setup you can run GitHub workflows to deploy infrastructure and Docker containers to Azure.

             [bold]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm($"{setupConfirmPrompt}", false)) System.Environment.Exit(0);
    }

    private void PrepareSubscriptionForContainerAppsEnvironment(string subscriptionId)
    {
        ProcessHelper.StartProcess(
            $"az provider register --namespace Microsoft.ContainerService --subscription {subscriptionId}",
            redirectOutput: !Environment.VerboseLogging
        );

        AnsiConsole.MarkupLine(
            "[green]Successfully ensured deployment of Azure Container Apps Environment is enabled on Azure Subscription.[/]");
    }

    private void CreateAppRegistration(AzureInfo azureInfo)
    {
        azureInfo.AppRegistrationId = ProcessHelper.StartProcess(
            $"""az ad app create --display-name "{azureInfo.AppRegistrationName}" --query appId -o tsv""",
            redirectOutput: true
        ).Trim();

        azureInfo.ServicePrincipalId = ProcessHelper.StartProcess(
            $"az ad sp create --id {azureInfo.AppRegistrationId} --query appId -o tsv",
            redirectOutput: true
        ).Trim();

        azureInfo.ServicePrincipalObjectId = ProcessHelper.StartProcess(
            $"""az ad sp list --filter "appId eq '{azureInfo.AppRegistrationId}'" --query "[].id" -o tsv""",
            redirectOutput: true
        ).Trim();

        AnsiConsole.MarkupLine(
            $"[green]Successfully created an App Registration {azureInfo.AppRegistrationName} ({azureInfo.AppRegistrationId}).[/]");
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
                subject = $"""repo:{githubInfo.Path}:{refRefsHeadsMain}""",
                audiences = new[] { "api://AzureADTokenExchange" }
            });

            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "az",
                Arguments = $"ad app federated-credential create --id {azureInfo.AppRegistrationId} --parameters  @-",
                RedirectStandardInput = true,
                RedirectStandardOutput = !Environment.VerboseLogging,
                RedirectStandardError = !Environment.VerboseLogging
            }, parameters);
        }
    }

    private void GrantSubscriptionPermissionsToServicePrincipal(AzureInfo azureInfo)
    {
        GrantAccess("Contributor");
        GrantAccess("User Access Administrator");
        GrantAccess("AcrPush");

        AnsiConsole.MarkupLine(
            $"[green]Successfully granted Service Principal ({azureInfo.ServicePrincipalId}) 'Contributor' and `User Access Administrator` rights to Azure Subscription.[/]");

        void GrantAccess(string role)
        {
            ProcessHelper.StartProcess(
                $"az role assignment create --assignee {azureInfo.ServicePrincipalId} --role \"{role}\" --scope /subscriptions/{azureInfo.Subscription.Id}",
                redirectOutput: !Environment.VerboseLogging
            );
        }
    }

    private void CreateAzureSqlServerSecurityGroup(AzureInfo azureInfo)
    {
        if (!azureInfo.SqlAdminsSecurityGroupExists)
        {
            azureInfo.SqlAdminsSecurityGroupId = ProcessHelper.StartProcess(
                $"""az ad group create --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --mail-nickname "{azureInfo.SqlAdminsSecurityGroupNickName}" --query "id" -o tsv""",
                redirectOutput: true
            ).Trim();
        }

        ProcessHelper.StartProcess(
            $"az ad group member add --group {azureInfo.SqlAdminsSecurityGroupId} --member-id {azureInfo.ServicePrincipalObjectId}",
            redirectOutput: !Environment.VerboseLogging);

        AnsiConsole.MarkupLine(
            $"[green]Successfully created AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' and granted the App Registration {azureInfo.AppRegistrationName} owner.[/]");
    }

    private void CreateGithubSecretsAndVariables(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        var clusterPrefix = githubInfo.Variables["UNIQUE_CLUSTER_PREFIX"];
        var domainNameStaging = githubInfo.Variables["DOMAIN_NAME_STAGING"];
        var domainNameProduction = githubInfo.Variables["DOMAIN_NAME_PRODUCTION"];

        ProcessHelper.StartProcess(
            $"gh secret set AZURE_TENANT_ID -b\"{azureInfo.Subscription.TenantId}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh secret set AZURE_SUBSCRIPTION_ID -b\"{azureInfo.Subscription.Id}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh secret set AZURE_SERVICE_PRINCIPAL_ID -b\"{azureInfo.AppRegistrationId}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh secret set ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID -b\"{azureInfo.SqlAdminsSecurityGroupId}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh variable set CONTAINER_REGISTRY_NAME -b\"{azureInfo.ContainerRegistry.Name}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh variable set UNIQUE_CLUSTER_PREFIX -b\"{clusterPrefix}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh variable set DOMAIN_NAME_STAGING -b\"{domainNameStaging}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh variable set DOMAIN_NAME_PRODUCTION -b\"{domainNameProduction}\" --repo={githubInfo.Path}");

        AnsiConsole.MarkupLine("[green]Successfully created secrets in GitHub.[/]");
    }

    private void PrintHeader(string heading)
    {
        var separator = new string('━', Console.WindowWidth - heading.Length - 1);
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }
}

public class GithubInfo
{
    public GithubInfo(string gitUrl)
    {
        GithubUrl = gitUrl.Replace(".git", "");
        OrganizationName = GithubUrl.Split("/")[3];
        RepositoryName = GithubUrl.Split("/")[4];
        Path = $"{OrganizationName}/{RepositoryName}";
    }

    public string OrganizationName { get; }

    public string RepositoryName { get; }

    public string GithubUrl { get; }

    public string Path { get; }

    public Dictionary<string, string> Secrets { get; set; } = new();

    public Dictionary<string, string> Variables { get; set; } = new();
}

public class AzureInfo
{
    public Subscription Subscription { get; set; } = default!;

    public ContainerRegistry ContainerRegistry { get; set; } = default!;

    public string AppRegistrationName { get; set; } = default!;

    public string AppRegistrationId { get; set; } = default!;

    public string ServicePrincipalId { get; set; } = default!;

    public string ServicePrincipalObjectId { get; set; } = default!;

    public bool AppRegistrationExists { get; set; }

    public string SqlAdminsSecurityGroupName { get; } = "Azure SQL Server Admins";

    public string SqlAdminsSecurityGroupNickName { get; } = "AzureSQLServerAdmins";

    public string SqlAdminsSecurityGroupId { get; set; } = default!;

    public bool SqlAdminsSecurityGroupExists { get; set; }
}

[UsedImplicitly]
public record Subscription(string Id, string Name, string TenantId, string State);

public record ContainerRegistry(string Name, bool Exists);