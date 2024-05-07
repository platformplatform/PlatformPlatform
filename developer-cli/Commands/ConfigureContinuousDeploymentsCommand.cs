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
public class ConfigureContinuousDeploymentsCommand : Command
{
    private static readonly JsonSerializerOptions? JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public ConfigureContinuousDeploymentsCommand() : base(
        "configure-continuous-deployments",
        "Set up trust between Azure and GitHub for passwordless deployments using Azure App Registration with OpenID (aka Federated Credentials)."
    )
    {
        AddOption(new Option<bool>(["--skip-azure-login"], "Skip Azure login"));
        AddOption(new Option<bool>(["--verbose-logging"], "Print Azure and Github CLI commands and output"));

        Handler = CommandHandler.Create<bool, bool>(Execute);
    }

    private int Execute(bool skipAzureLogin = false, bool verboseLogging = false)
    {
        PrerequisitesChecker.Check("dotnet", "az", "gh");

        AzureInfo azureInfo = new();

        Configuration.VerboseLogging = verboseLogging;

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

    private GithubInfo GetGithubInfo()
    {
        var gitRemotes = ProcessHelper.StartProcess("git remote -v", Configuration.GetSourceCodeFolder(), true);

        var gitRemoteRegex = new Regex(@"(?<url>(https://github\.com/.*/.*\.git)|(git@github\.com:.*/.*\.git))");
        var gitRemoteMatches = gitRemoteRegex.Match(gitRemotes);
        if (!gitRemoteMatches.Success)
        {
            AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. This tool only works with GitHub remotes.[/]");
            Environment.Exit(0);
        }

        var githubInfo = new GithubInfo(gitRemoteMatches.Groups["url"].Value);

        var githubVariablesJson = ProcessHelper.StartProcess($"gh api repos/{githubInfo.Path}/actions/variables", redirectOutput: true);

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

    private string RunAzureCliCommand(string arguments, bool redirectOutput = true)
    {
        var azureCliCommand = Configuration.IsWindows ? "cmd.exe /C az" : "az";

        return ProcessHelper.StartProcess($"{azureCliCommand} {arguments}", redirectOutput: redirectOutput);
    }

    private void CollectAzureSubscriptionInfo(AzureInfo azureInfo, bool skipAzureLogin, GithubInfo githubInfo)
    {
        // Both `az login` and `az account list` will return a JSON array of subscriptions
        var subscriptionListJson = RunAzureCliCommand($"{(skipAzureLogin ? "account list --output json" : "login")}");

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
            .AddChoices(activeSubscriptions.Select(s => s.Name))
        );

        var selectedSubscriptions = activeSubscriptions.Where(s => s.Name == selectedDisplayName).ToArray();
        if (selectedSubscriptions.Length > 1)
        {
            AnsiConsole.MarkupLine($"[red]ERROR:[/] Found two subscriptions with the name {selectedDisplayName}.");
            Environment.Exit(1);
        }

        var subscription = selectedSubscriptions.Single();
        RunAzureCliCommand($"account set --subscription {subscription.Id}", false);

        AnsiConsole.MarkupLine($"{title}: {subscription.Name}\n");

        azureInfo.AppRegistrationName = $"GitHub Azure - {githubInfo.OrganizationName} - {githubInfo.RepositoryName}";

        azureInfo.Subscription = subscription;
    }

    private void CollectExistingAppRegistration(AzureInfo azureInfo)
    {
        var appRegistrationId = RunAzureCliCommand(
            $"""ad app list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv"""
        ).Trim();

        var servicePrincipalId = RunAzureCliCommand(
            $"""ad sp list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv"""
        ).Trim();

        var servicePrincipalObjectId = RunAzureCliCommand(
            $"""ad sp list --filter "appId eq '{appRegistrationId}'" --query "[].id" -o tsv"""
        ).Trim();

        if (appRegistrationId != string.Empty && servicePrincipalId != string.Empty)
        {
            azureInfo.AppRegistrationExists = true;
            AnsiConsole.MarkupLine(
                $"[yellow]The App Registration '{azureInfo.AppRegistrationName}' already exists with App ID: {servicePrincipalId}[/]"
            );

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
                $"[red]The App Registration or Service Principal '{azureInfo.AppRegistrationName}' exists but not both. Please manually delete and retry.[/]"
            );
            Environment.Exit(1);
        }
    }

    private void CollectExistingSqlAdminSecurityGroup(AzureInfo azureInfo)
    {
        azureInfo.SqlAdminsSecurityGroupId = RunAzureCliCommand(
            $"""ad group list --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --query "[].id" -o tsv"""
        ).Trim();

        if (azureInfo.SqlAdminsSecurityGroupId == string.Empty)
        {
            azureInfo.SqlAdminsSecurityGroupId = null;
            return;
        }

        AnsiConsole.MarkupLine(
            $"[yellow]The AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' already exists with ID: {azureInfo.SqlAdminsSecurityGroupId}[/]"
        );

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
            var registryName = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold]Please enter a unique name for the Azure Container Registry (ACR) that will store your container images[/]")
                    .DefaultValue(existingContainerRegistryName)
                    .Validate(input =>
                        Regex.IsMatch(input, "^[a-z0-9]{5,50}$")
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]ERROR:[/]The name must be 5-50 characters and contain only lowercase characters a-z or 0-9.")
                    )
            );

            //  Check whether the Azure Container Registry name is available
            var checkAvailability = RunAzureCliCommand($"acr check-name --name {registryName} --query \"nameAvailable\" -o tsv");

            if (bool.Parse(checkAvailability))
            {
                AnsiConsole.WriteLine();
                azureInfo.ContainerRegistry = new ContainerRegistry(registryName);
                return;
            }

            // Checks if the Azure Container Registry is a resource under the current subscription
            var showExistingRegistry = RunAzureCliCommand($"acr show --name {registryName} --subscription {azureInfo.Subscription.Id} --output json");

            var jsonRegex = new Regex(@"\{.*\}", RegexOptions.Singleline);
            var match = jsonRegex.Match(showExistingRegistry);

            if (match.Success)
            {
                var jsonDocument = JsonDocument.Parse(match.Value);
                if (jsonDocument.RootElement.GetProperty("id").GetString()?.Contains(azureInfo.Subscription.Id) == true)
                {
                    AnsiConsole.MarkupLine(
                        $"[yellow]The Azure Container Registry {registryName} exists on the selected subscription and will be reused.[/]"
                    );
                    AnsiConsole.WriteLine();
                    azureInfo.ContainerRegistry = new ContainerRegistry(registryName);
                    return;
                }
            }

            AnsiConsole.MarkupLine(
                $"[red]ERROR:[/]The Azure Container Registry name [blue]{registryName}[/] is already in use, possibly in another subscription. Please enter a unique name."
            );
        }
    }

    private void CollectDomainNames(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        githubInfo.Variables.TryGetValue("DOMAIN_NAME_PRODUCTION", out var domainNameProduction);
        azureInfo.ProductionDomainName = domainNameProduction ?? "-";

        githubInfo.Variables.TryGetValue("DOMAIN_NAME_STAGING", out var domainNameStaging);
        azureInfo.StagingDomainName = domainNameStaging ?? "-";
    }

    private void CollectUniquePrefix(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        githubInfo.Variables.TryGetValue("UNIQUE_CLUSTER_PREFIX", out var uniquePrefix);

        AnsiConsole.MarkupLine(
            "When creating Azure resources like SQL Server, Blob storage, Service Bus, Key Vaults, etc., a global unique name is required. To do this we use a prefix of 2-6 characters, which allows for flexibility for the rest of the name. E.g. if you select 'acme' the production SQL Server in West Europe will be named 'acme-prod-euw'`."
        );

        var defaultValue = uniquePrefix
                           ?? githubInfo.OrganizationName.ToLower().Substring(0, Math.Min(6, githubInfo.OrganizationName.Length));


        uniquePrefix = AnsiConsole.Prompt(
            new TextPrompt<string>("[bold]Please enter a unique prefix between 2-6 characters (e.g. an acronym for your product or company).[/]")
                .DefaultValue(defaultValue)
                .Validate(input =>
                    Regex.IsMatch(input, "^[a-z0-9]{2,6}$")
                        ? ValidationResult.Success()
                        : ValidationResult.Error("[red]ERROR:[/]The unique prefix must be 2-6 characters and contain only lowercase characters a-z or 0-9.")
                )
        );

        azureInfo.UniquePrefix = uniquePrefix;

        AnsiConsole.WriteLine();
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
        var appRegistrationAction = azureInfo.AppRegistrationExists ? "updated" : "created";
        var reuseSqlAdminsSecurityGroupAction = azureInfo.SqlAdminsSecurityGroupExists ? "updated" : "created";

        var setupConfirmPrompt =
            $"""
             [bold]If you continue the following will happen:[/]

             1. The App Registration named [blue]{azureInfo.AppRegistrationName}[/] will be {appRegistrationAction} allowing GitHub to do passwordless deployments to Azure.

             2. The App Registration will be granted the 'Contributor' and 'User Access Administrator' roles in the Azure Subscription.

             3. The AD Security Group [blue]{azureInfo.SqlAdminsSecurityGroupName}[/] will be {reuseSqlAdminsSecurityGroupAction}, with the App Registration set as the owner.

             4. The GitHub Repository [blue]{githubInfo.GithubUrl}[/] will be configured with the following secrets and variables:
             
                GitHub Secrets (soft secrets):
                * AZURE_TENANT_ID: [blue]{azureInfo.Subscription.TenantId}[/]
                * AZURE_SUBSCRIPTION_ID: [blue]{azureInfo.Subscription.Id}[/]
                * AZURE_SERVICE_PRINCIPAL_ID: [blue]{azureInfo.AppRegistrationId ?? "will be generated"}[/]
                * ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID: [blue]{azureInfo.SqlAdminsSecurityGroupId ?? "will be generated"}[/]
             
                GitHub Variables:
                * CONTAINER_REGISTRY_NAME: [blue]{azureInfo.ContainerRegistry.Name}[/]
                * DOMAIN_NAME_PRODUCTION: [blue]{azureInfo.ProductionDomainName}[/] (can be changed later)
                * DOMAIN_NAME_STAGING: [blue]{azureInfo.StagingDomainName}[/] (can be changed later)
                * UNIQUE_CLUSTER_PREFIX: [blue]{azureInfo.UniquePrefix}[/]

             5. The following environments will be created in the GitHub repository [blue]shared[/], [blue]staging[/], and [blue]production[/] if they do not exist.

             6. You will receive instructions for initial infrastructure and application deployment to Azure, as well as GitHub configuration recommendations.

             Please note that this command can be run again update the configuration. Use the [yellow]--skip-azure-login[/] flag to avoid logging in to Azure again.

             [bold]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm($"{setupConfirmPrompt}", false)) Environment.Exit(0);
    }

    private void PrepareSubscriptionForContainerAppsEnvironment(string subscriptionId)
    {
        RunAzureCliCommand(
            $"provider register --namespace Microsoft.ContainerService --subscription {subscriptionId}",
            !Configuration.VerboseLogging
        );

        AnsiConsole.MarkupLine("[green]Successfully ensured deployment of Azure Container Apps Environment is enabled on Azure Subscription.[/]");
    }

    private void CreateAppRegistrationIfNotExists(AzureInfo azureInfo)
    {
        if (azureInfo.AppRegistrationExists) return;

        azureInfo.AppRegistrationId = RunAzureCliCommand(
            $"""ad app create --display-name "{azureInfo.AppRegistrationName}" --query appId -o tsv"""
        ).Trim();

        azureInfo.ServicePrincipalId = RunAzureCliCommand(
            $"ad sp create --id {azureInfo.AppRegistrationId} --query appId -o tsv"
        ).Trim();

        azureInfo.ServicePrincipalObjectId = RunAzureCliCommand(
            $"""ad sp list --filter "appId eq '{azureInfo.AppRegistrationId}'" --query "[].id" -o tsv"""
        ).Trim();

        AnsiConsole.MarkupLine(
            $"[green]Successfully created an App Registration {azureInfo.AppRegistrationName} ({azureInfo.AppRegistrationId}).[/]"
        );
    }

    private void CreateFederatedCredentials(AzureInfo azureInfo, GithubInfo githubInfo)
    {
        CreateFederatedCredential("MainBranch", "ref:refs/heads/main");
        CreateFederatedCredential("PullRequests", "pull_request");
        CreateFederatedCredential("SharedEnvironment", "environment:shared");
        CreateFederatedCredential("StagingEnvironment", "environment:staging");
        CreateFederatedCredential("ProductionEnvironment", "environment:production");

        AnsiConsole.MarkupLine(
            $"[green]Successfully created Federated Credentials allowing passwordless deployments from {githubInfo.GithubUrl}.[/]"
        );

        void CreateFederatedCredential(string displayName, string refRefsHeadsMain)
        {
            var parameters = JsonSerializer.Serialize(new
                {
                    name = displayName,
                    issuer = "https://token.actions.githubusercontent.com",
                    subject = $"""repo:{githubInfo.Path}:{refRefsHeadsMain}""",
                    audiences = new[] { "api://AzureADTokenExchange" }
                }
            );

            ProcessHelper.StartProcess(new ProcessStartInfo
                {
                    FileName = Configuration.IsWindows ? "cmd.exe" : "az",
                    Arguments =
                        $"{(Configuration.IsWindows ? "/C az" : string.Empty)} ad app federated-credential create --id {azureInfo.AppRegistrationId} --parameters  @-",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = !Configuration.VerboseLogging,
                    RedirectStandardError = !Configuration.VerboseLogging
                }, parameters
            );
        }
    }

    private void GrantSubscriptionPermissionsToServicePrincipal(AzureInfo azureInfo)
    {
        GrantAccess("Contributor");
        GrantAccess("User Access Administrator");
        GrantAccess("AcrPush");

        AnsiConsole.MarkupLine(
            $"[green]Successfully granted Service Principal ({azureInfo.ServicePrincipalId}) 'Contributor' and `User Access Administrator` rights to Azure Subscription.[/]"
        );

        void GrantAccess(string role)
        {
            RunAzureCliCommand(
                $"role assignment create --assignee {azureInfo.ServicePrincipalId} --role \"{role}\" --scope /subscriptions/{azureInfo.Subscription.Id}",
                !Configuration.VerboseLogging
            );
        }
    }

    private void CreateAzureSqlServerSecurityGroup(AzureInfo azureInfo)
    {
        if (!azureInfo.SqlAdminsSecurityGroupExists)
        {
            azureInfo.SqlAdminsSecurityGroupId = RunAzureCliCommand(
                $"""ad group create --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --mail-nickname "{azureInfo.SqlAdminsSecurityGroupNickName}" --query "id" -o tsv"""
            ).Trim();
        }

        RunAzureCliCommand(
            $"ad group member add --group {azureInfo.SqlAdminsSecurityGroupId} --member-id {azureInfo.ServicePrincipalObjectId}",
            !Configuration.VerboseLogging
        );

        AnsiConsole.MarkupLine(
            $"[green]Successfully created AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' and granted the App Registration {azureInfo.AppRegistrationName} owner.[/]"
        );
    }

    private void CreateGithubSecretsAndVariables(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        ProcessHelper.StartProcess(
            $"gh secret set AZURE_TENANT_ID -b\"{azureInfo.Subscription.TenantId}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh secret set AZURE_SUBSCRIPTION_ID -b\"{azureInfo.Subscription.Id}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh secret set AZURE_SERVICE_PRINCIPAL_ID -b\"{azureInfo.AppRegistrationId}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh secret set ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID -b\"{azureInfo.SqlAdminsSecurityGroupId}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh variable set CONTAINER_REGISTRY_NAME -b\"{azureInfo.ContainerRegistry.Name}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh variable set DOMAIN_NAME_PRODUCTION -b\"{azureInfo.ProductionDomainName}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh variable set DOMAIN_NAME_STAGING -b\"{azureInfo.StagingDomainName}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh variable set UNIQUE_CLUSTER_PREFIX -b\"{azureInfo.UniquePrefix}\" --repo={githubInfo.Path}"
        );

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
            "[green]Successfully created [bold]shared[/], [bold]staging[/], and [bold]production[/] environments in GitHub repository.[/]"
        );
    }

    private void ShowSuccessMessage(GithubInfo githubInfo)
    {
        var setupIntroPrompt =
            $"""
             We're almost done.

             [yellow]Please follow the instructions below to complete configuration:[/]

             1. Run the [blue]Cloud Infrastructure - Deployment[/] to deploy Azure Infrastructure. Navigate to [blue]{githubInfo.GithubUrl}/actions/workflows/cloud-infrastructure.yml[/] and click ""Run workflow"". The process usually takes approximately 30-45 minutes to complete initially.

             2. Run the [blue]Application - Build and Deploy[/] to deploy the create and deploy container images. Navigate to [blue]{githubInfo.GithubUrl}/actions/workflows/application.yml[/] and click ""Run workflow"". This process should be completed in less than 5 minutes.

             [bold]Optional but recommended configurations:[/]

             - For protecting the [blue]main[/] branch, configure branch protection rules to necessitate pull request reviews before merging can occur. Visit [blue]{githubInfo.GithubUrl}/settings/branches[/], click ""Add Branch protection rule"", and set it up for the [bold]main[/] branch.

             - To add a step for manual approval during infrastructure deployment to the Staging and Production environments, set up required reviewers on GitHub environments. This requires a GitHub Teams or Enterprise Cloud subscription for private repositories. Visit [blue]{githubInfo.GithubUrl}/settings/environments[/] and enable [blue]Required reviewers[/] for the [bold]staging[/] and [bold]production[/] environments.

             - Configure the Domain Name for the Staging and Production environments. This involves two steps:
                 
                 a. Go to [blue]{githubInfo.GithubUrl}/settings/variables/actions[/] to set the [blue]DOMAIN_NAME_STAGING[/] and [blue]DOMAIN_NAME_PRODUCTION[/] variables. E.g. [blue]staging.your-saas-company.com[/] and [blue]your-saas-company.com[/].
                 
                 b. Run the [blue]Cloud Infrastructure - Deployment[/] workflow again. Note that it might fail with an error message to set up a DNS TXT and CNAME record. Once done, rerun the failed jobs.

             - Set up SonarCloud for code quality and security analysis. This service is free for public repositories. Visit [blue]https://sonarcloud.io[/] to connect your GitHub account. Add the [blue]SONAR_TOKEN[/] secret, and the [blue]SONAR_ORGANIZATION[/] and [blue]SONAR_PROJECT_KEY[/] variables to the GitHub repository. The workflows are already configured for SonarCloud analysis.

             - Enable Microsoft Defender for Cloud (also known as Azure Security Center) once the system evolves for added security recommendations. This costs about $10-15 per month per cluster.

             You can rerun this command to update the configuration. Use the [yellow]--skip-azure-login[/] flag to bypass Azure login.
             """;

        AnsiConsole.MarkupLine($"{setupIntroPrompt}");
        AnsiConsole.WriteLine();
    }

    private void PrintHeader(string heading)
    {
        var separator = new string('-', Console.WindowWidth - heading.Length - 1);
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }
}

public class GithubInfo
{
    public GithubInfo(string gitUri)
    {
        string remote;
        if (gitUri.StartsWith("https://github.com/"))
        {
            remote = gitUri.Replace("https://github.com/", "").Replace(".git", "");
        }
        else if (gitUri.StartsWith("git@github.com:"))
        {
            remote = gitUri.Replace("git@github.com:", "").Replace(".git", "");
        }
        else
        {
            throw new ArgumentException($"Invalid Git URI: {gitUri}. Only https:// and git@ formatted is supported.", nameof(gitUri));
        }

        var parts = remote.Split("/");
        OrganizationName = parts[0];
        RepositoryName = parts[1];

        Path = $"{OrganizationName}/{RepositoryName}";
    }

    public string OrganizationName { get; }

    public string RepositoryName { get; }

    public string GithubUrl => $"https://github.com/{OrganizationName}/{RepositoryName}";

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
