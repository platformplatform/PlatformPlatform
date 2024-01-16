using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

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
        AzureInfo azureInfo = new();

        Configuration.VerboseLogging = verboseLogging;

        EnsureAzureAndGithubCliToolsAreInstalled();

        PrintHeader("Introduction");

        var githubInfo = GetGithubInfo();

        ShowIntroPrompt();

        PrintHeader("Collecting data");

        CollectAzureSubscriptionInfo(azureInfo, skipAzureLogin, githubInfo);

        CollectExistingAppRegistration(azureInfo);

        CollectExistingSqlAdminSecurityGroup(azureInfo);

        CollectAzureContainerRegistryName(githubInfo, azureInfo);

        CollectDomainNames(githubInfo, azureInfo);

        CollectUniquePrefix(githubInfo, azureInfo);

        LoginToGithub();

        PrintHeader("Confirm changes");

        ConfirmChangesPrompt(githubInfo, azureInfo);

        PrintHeader("Configuring Azure and GitHub");

        PrepareSubscriptionForContainerAppsEnvironment(azureInfo.Subscription.Id);

        CreateAppRegistrationIfNotExists(azureInfo);

        CreateFederatedCredentials(azureInfo, githubInfo);

        GrantSubscriptionPermissionsToServicePrincipal(azureInfo);

        CreateAzureSqlServerSecurityGroup(azureInfo);

        CreateGithubSecretsAndVariables(githubInfo, azureInfo);

        CreateGithubEnvironments(githubInfo);

        PrintHeader("Configuration of GitHub and Azure completed 🎉");

        ShowSuccessMessage(githubInfo);

        return 0;
    }

    private void EnsureAzureAndGithubCliToolsAreInstalled()
    {
        PrerequisitesChecker.CheckCommandLineTool("az", "Azure CLI", new Version(2, 55), true);
        PrerequisitesChecker.CheckCommandLineTool("gh", "GitHub CLI", new Version(2, 41), true);
    }

    private GithubInfo GetGithubInfo()
    {
        var gitRemotes = ProcessHelper.StartProcess("git remote -v", Configuration.GetSolutionFolder(), true);

        var gitRemoteRegex = new Regex(@"(?<url>https://github\.com/.*\.git)");
        var gitRemoteMatches = gitRemoteRegex.Match(gitRemotes);
        if (!gitRemoteMatches.Success)
        {
            AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. This tool only works with GitHub remotes.[/]");
            Environment.Exit(0);
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

        if (!AnsiConsole.Confirm(setupIntroPrompt, false)) Environment.Exit(0);

        AnsiConsole.WriteLine();
    }

    private void CollectAzureSubscriptionInfo(AzureInfo azureInfo, bool skipAzureLogin, GithubInfo githubInfo)
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
        ProcessHelper.StartProcess($"az account set --subscription {subscription.Id}");

        AnsiConsole.MarkupLine($"{title}: {subscription.Name}\n");

        azureInfo.AppRegistrationName = $"GitHub Azure - {githubInfo.OrganizationName} - {githubInfo.RepositoryName}";

        azureInfo.Subscription = subscription;
    }

    private void CollectExistingAppRegistration(AzureInfo azureInfo)
    {
        var appRegistrationId = ProcessHelper.StartProcess(
            $"""az ad app list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv""",
            redirectOutput: true
        ).Trim();

        var servicePrincipalId = ProcessHelper.StartProcess(
            $"""az ad sp list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv""",
            redirectOutput: true
        ).Trim();

        var servicePrincipalObjectId = ProcessHelper.StartProcess(
            $"""az ad sp list --filter "appId eq '{appRegistrationId}'" --query "[].id" -o tsv""",
            redirectOutput: true
        ).Trim();

        if (appRegistrationId != string.Empty && servicePrincipalId != string.Empty)
        {
            azureInfo.AppRegistrationExists = true;
            AnsiConsole.MarkupLine(
                $"[yellow]The App Registration '{azureInfo.AppRegistrationName}' already exists with App ID: {servicePrincipalId}[/]");

            if (AnsiConsole.Confirm("The existing App Registration will be reused. Do you want to continue?"))
            {
                azureInfo.AppRegistrationId = appRegistrationId;
                azureInfo.ServicePrincipalId = servicePrincipalId;
                azureInfo.ServicePrincipalObjectId = servicePrincipalObjectId;
                AnsiConsole.WriteLine();
                return;
            }

            AnsiConsole.MarkupLine("[red]Please delete the existing App Registration and try again.[/]");
            Environment.Exit(1);
        }

        if (appRegistrationId != string.Empty || servicePrincipalId != string.Empty)
        {
            AnsiConsole.MarkupLine(
                $"[red]The App Registration or Service Principal '{azureInfo.AppRegistrationName}' exists but not both. Please manually delete and retry.[/]");
            Environment.Exit(1);
        }
    }

    private void CollectExistingSqlAdminSecurityGroup(AzureInfo azureInfo)
    {
        azureInfo.SqlAdminsSecurityGroupId = ProcessHelper.StartProcess(
            $"""az ad group list --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --query "[].id" -o tsv""",
            redirectOutput: true
        ).Trim();

        if (azureInfo.SqlAdminsSecurityGroupId == string.Empty)
        {
            azureInfo.SqlAdminsSecurityGroupId = null;
            return;
        }

        AnsiConsole.MarkupLine(
            $"[yellow]The AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' already exists with ID: {azureInfo.SqlAdminsSecurityGroupId}[/]");

        if (AnsiConsole.Confirm("The existing AD Security Group will be reused. Do you want to continue?"))
        {
            AnsiConsole.WriteLine();
            azureInfo.SqlAdminsSecurityGroupExists = true;
            return;
        }

        AnsiConsole.MarkupLine("[red]Please delete the existing AD Security Group and try again.[/]");
        Environment.Exit(1);
    }

    private void CollectAzureContainerRegistryName(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        githubInfo.Variables.TryGetValue("CONTAINER_REGISTRY_NAME", out var existingContainerRegistryName);
        existingContainerRegistryName ??= githubInfo.OrganizationName.ToLower();

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
                azureInfo.ContainerRegistry = new ContainerRegistry(registryName);
                return;
            }

            // Checks if the Azure Container Registry is a resource under the current subscription
            var showExistingRegistry = ProcessHelper.StartProcess(
                $"az acr show --name {registryName} --subscription {azureInfo.Subscription.Id}", redirectOutput: true);

            var jsonRegex = new Regex(@"\{.*\}", RegexOptions.Singleline);
            var match = jsonRegex.Match(showExistingRegistry);

            if (match.Success)
            {
                var jsonDocument = JsonDocument.Parse(match.Value);
                if (jsonDocument.RootElement.GetProperty("id").GetString()?.Contains(azureInfo.Subscription.Id) == true)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]The Azure Container Registry {registryName} exists on the selected subscription and will be reused.[/]");
                    AnsiConsole.WriteLine();
                    azureInfo.ContainerRegistry = new ContainerRegistry(registryName);
                    return;
                }
            }

            AnsiConsole.MarkupLine(
                $"[red]ERROR:[/]The Azure Container Registry {registryName} is invalid or already exists. Please try again.");
        }
    }

    private void CollectDomainNames(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        AnsiConsole.MarkupLine(
            "You can configure a custom domain name for both production and staging environments. During deployment you will be asked to configure DNS records, after which a valid certificate will automatically be generated and configured.");

        githubInfo.Variables.TryGetValue("DOMAIN_NAME_PRODUCTION", out var domainNameProduction);
        azureInfo.ProductionDomainName = GetValidDomainName("production", domainNameProduction);

        githubInfo.Variables.TryGetValue("DOMAIN_NAME_STAGING", out var domainNameStaging);
        domainNameStaging = GetValidDomainName("staging",
            domainNameStaging
            ?? (azureInfo.ProductionDomainName == "-" ? null : $"staging.{azureInfo.ProductionDomainName}")
        );
        azureInfo.StagingDomainName = domainNameStaging;
        return;

        string GetValidDomainName(string displayName, string? defaultDomainName = "")
        {
            while (true)
            {
                if (defaultDomainName == "-") defaultDomainName = "";
                var domainName =
                    AnsiConsole.Ask(
                        $"[bold]Please enter a domain name for [blue]{displayName}[/]. Use [blue]-[/] or leave blank to configure later.[/]",
                        defaultDomainName
                    );

                if (string.IsNullOrWhiteSpace(domainName))
                {
                    AnsiConsole.WriteLine();
                    return "-";
                }

                if (Uri.CheckHostName(domainName) == UriHostNameType.Dns || !domainName.Contains('.'))
                {
                    AnsiConsole.WriteLine();

                    return domainName;
                }

                AnsiConsole.MarkupLine(
                    $"[red]ERROR:[/]The domain name {domainName} is not a valid host name. Please try again.");
            }
        }
    }

    private void CollectUniquePrefix(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        githubInfo.Variables.TryGetValue("UNIQUE_CLUSTER_PREFIX", out var uniquePrefix);

        AnsiConsole.MarkupLine(
            "When creating Azure resources like SQL Server, Blob storage, Service Bus, Key Vaults, etc., a global unique name is required. To do this we use a prefix of 2-6 characters, which allows for flexibility for the rest of the name. E.g. if you select 'acme' the production SQL Server in West Europe will be named 'acme-prod-euw'`.");

        var defaultValue = uniquePrefix ?? githubInfo.OrganizationName.ToLower()
            .Substring(0, Math.Min(6, githubInfo.OrganizationName.Length));

        while (true)
        {
            uniquePrefix = AnsiConsole.Ask(
                "[bold]Please enter a unique prefix between 2-6 characters (e.g. an acronym for your product or company).[/]",
                defaultValue
            ).ToLower();

            if (uniquePrefix.Length is < 2 or > 6) continue;

            azureInfo.UniquePrefix = uniquePrefix;

            AnsiConsole.WriteLine();
            return;
        }
    }


    private void LoginToGithub()
    {
        ProcessHelper.StartProcess("gh auth login --git-protocol https --web");

        var output = ProcessHelper.StartProcess("gh auth status", redirectOutput: true);

        if (!output.Contains("Logged in to github.com")) Environment.Exit(0);
        AnsiConsole.WriteLine();
    }

    private void ConfirmChangesPrompt(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        var appRegistrationInto = azureInfo.AppRegistrationExists ? "Update" : "Create";
        var reuseSqlAdminsSecurityGroupIntro = azureInfo.SqlAdminsSecurityGroupExists ? "Update" : "Create";

        var setupConfirmPrompt =
            $"""
             [bold]If you continue the following changes will be made:[/]

             1. Ensure deployment of Azure Container Apps Environment is enabled on Azure Subscription: [blue]{azureInfo.Subscription.Name} ({azureInfo.Subscription.Id})[/]

             2. {appRegistrationInto} [blue]{azureInfo.AppRegistrationName}[/] App Registration with Federated Credentials and trust deployments from Github

             3. Grant the App Registration 'Contributor' and 'User Access Administrator' role to the Azure Subscription

             4. {reuseSqlAdminsSecurityGroupIntro} [blue]{azureInfo.SqlAdminsSecurityGroupName}[/] AD Security Group and make the App Registration owner

             5. Configure GitHub Repository [blue]{githubInfo.GithubUrl}[/] with the following
             
                GitHub Secrets:
                * AZURE_TENANT_ID: [blue]{azureInfo.Subscription.TenantId}[/]
                * AZURE_SUBSCRIPTION_ID: [blue]{azureInfo.Subscription.Id}[/]
                * AZURE_SERVICE_PRINCIPAL_ID: [blue]{azureInfo.AppRegistrationId ?? "will be generated"}[/]
                * ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID: [blue]{azureInfo.SqlAdminsSecurityGroupId ?? "will be generated"}[/]
                
                GitHub Variables:
                * CONTAINER_REGISTRY_NAME: [blue]{azureInfo.ContainerRegistry.Name}[/]
                * DOMAIN_NAME_PRODUCTION: [blue]{azureInfo.ProductionDomainName}[/]
                * DOMAIN_NAME_STAGING: [blue]{azureInfo.StagingDomainName}[/]
                * UNIQUE_CLUSTER_PREFIX: [blue]{azureInfo.UniquePrefix}[/]

             6. Create [blue]shared[/], [blue]staging[/], and [blue]production[/] environments in GitHub repository

             7. Provide instructions on how to do the first deployment of infrastructure and application to Azure, and make recommendations configuring to GitHub

             Please note that you can run this command multiple times to update the configuration.

             [bold]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm($"{setupConfirmPrompt}", false)) Environment.Exit(0);
    }

    private void PrepareSubscriptionForContainerAppsEnvironment(string subscriptionId)
    {
        ProcessHelper.StartProcess(
            $"az provider register --namespace Microsoft.ContainerService --subscription {subscriptionId}",
            redirectOutput: !Configuration.VerboseLogging
        );

        AnsiConsole.MarkupLine(
            "[green]Successfully ensured deployment of Azure Container Apps Environment is enabled on Azure Subscription.[/]");
    }

    private void CreateAppRegistrationIfNotExists(AzureInfo azureInfo)
    {
        if (azureInfo.AppRegistrationExists) return;

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
                RedirectStandardOutput = !Configuration.VerboseLogging,
                RedirectStandardError = !Configuration.VerboseLogging
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
                redirectOutput: !Configuration.VerboseLogging
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
            redirectOutput: !Configuration.VerboseLogging);

        AnsiConsole.MarkupLine(
            $"[green]Successfully created AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' and granted the App Registration {azureInfo.AppRegistrationName} owner.[/]");
    }

    private void CreateGithubSecretsAndVariables(GithubInfo githubInfo, AzureInfo azureInfo)
    {
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
            $"gh variable set DOMAIN_NAME_PRODUCTION -b\"{azureInfo.ProductionDomainName}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh variable set DOMAIN_NAME_STAGING -b\"{azureInfo.StagingDomainName}\" --repo={githubInfo.Path}");
        ProcessHelper.StartProcess(
            $"gh variable set UNIQUE_CLUSTER_PREFIX -b\"{azureInfo.UniquePrefix}\" --repo={githubInfo.Path}");

        AnsiConsole.MarkupLine("[green]Successfully created secrets in GitHub.[/]");
    }

    private void CreateGithubEnvironments(GithubInfo githubInfo)
    {
        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{githubInfo.Path}/environments/shared""",
            redirectOutput: true
        );
        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{githubInfo.Path}/environments/staging""",
            redirectOutput: true
        );
        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{githubInfo.Path}/environments/production""",
            redirectOutput: true
        );

        AnsiConsole.MarkupLine(
            "[green]Successfully created [bold]shared[/], [bold]staging[/], and [bold]production[/] environments in GitHub repository.[/]");
    }

    private void ShowSuccessMessage(GithubInfo githubInfo)
    {
        var setupIntroPrompt =
            $"""
             We are almost done.

             [yellow]Please read and follow the following instructions to complete the configuration:[/]

             1. To avoid automated deployments to Staging and Production, please go to [blue]{githubInfo.GithubUrl}/settings/environments[/] and check [blue]Required reviewers[/] for the [bold]staging[/] and [bold]production[/] environments. This will ensure manual approval before deployment of changes to infrastructure to Staging and Production.

             2. Azure Infrastructure needs to be created before we can deploy Docker images to the Azure Container Registry. But deployment of Azure Container Apps requires a Docker image to be available in the Azure Container Registry. The solution is to follow these steps carefully:

             - Run the [blue]Cloud Infrastructure - Deployment[/] GitHub workflow to deploy the Azure Infrastructure to the Shared environment, [yellow]but do not approve infrastructure changes to Staging and Production yet.[/]
             - Run the [blue]Application - Build and Deploy[/] GitHub workflow to build and deploy the Docker images to the Azure Container Registry, [yellow]but do not deploy the application to Staging and Production yet.[/]
             - Complete the deployment of the Azure Infrastructure to Staging and Production - [yellow]please see step 3.[/]
             - Complete the deployment of the application to Staging and Production.

             3. If you configured a domain a DNS record needs to be created before the Azure Container App can be fully created. The GitHub action deployment of infrastructure will fail with clear instructions on how to create the DNS record, after which you can [yellow]click the "Re-run failed jobs" in the GitHub UI[/], to complete the deployment.

             TIP: If the GitHub workflow fails, you can always rerun the workflow. Knowing these issues will help you understand the error messages more easily.

             [bold]Optionally, but recommended:[/]

             - Set up branch protection rules for the [blue]main[/] branch to require pull request reviews before merging.
             - Set up SonarCloud for code quality and security analysis. Workflows are already configured to run SonarCloud analysis. Just go to [blue]https://sonarcloud.io[/] and connect your GitHub account, and add [blue]SONAR_TOKEN[/] secret, and [blue]SONAR_ORGANIZATION[/] and [blue]SONAR_PROJECT_KEY[/] variables to the GitHub repository.
             - Enable Microsoft Defender for Cloud (aka Azure Security Center). PlatformPlatform is built following all security recommendations, but as the system evolves, new security recommendations will be added. Microsoft Defender for Cloud will help you keep track of these recommendations and help you fix them.

             Also, note that you can run this command multiple times to update the configuration.
             """;

        AnsiConsole.MarkupLine($"{setupIntroPrompt}");
        AnsiConsole.WriteLine();
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

    public string? AppRegistrationId { get; set; }

    public string ServicePrincipalId { get; set; } = default!;

    public string ServicePrincipalObjectId { get; set; } = default!;

    public bool AppRegistrationExists { get; set; }

    public string SqlAdminsSecurityGroupName => "Azure SQL Server Admins";

    public string SqlAdminsSecurityGroupNickName => "AzureSQLServerAdmins";

    public string? SqlAdminsSecurityGroupId { get; set; }

    public bool SqlAdminsSecurityGroupExists { get; set; }

    public string ProductionDomainName { get; set; } = "-";

    public string StagingDomainName { get; set; } = "-";

    public string UniquePrefix { get; set; } = default!;
}

[UsedImplicitly]
public record Subscription(string Id, string Name, string TenantId, string State);

public record ContainerRegistry(string Name);