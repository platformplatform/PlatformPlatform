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

        var githubInfo = new GithubInfo();

        PrintHeader("Introduction");

        ShowIntroPrompt(githubInfo, skipAzureLogin);

        PrintHeader("Collecting data");

        SetGithubInfo(githubInfo);

        LoginToGithub(githubInfo);

        PublishGithubVariables(githubInfo);

        CollectAzureSubscriptionInfo(azureInfo, skipAzureLogin, githubInfo);

        CollectUniquePrefix(githubInfo, azureInfo);

        CollectExistingAppRegistration(azureInfo);

        CollectExistingSqlAdminSecurityGroup(azureInfo, githubInfo);

        CollectDomainNames(githubInfo, azureInfo);

        PrintHeader("Confirm changes");

        ConfirmChangesPrompt(githubInfo, azureInfo);

        PrintHeader("Configuring Azure and GitHub");

        PrepareSubscriptionForContainerAppsEnvironment(azureInfo.Subscription.Id);

        CreateAppRegistrationIfNotExists(azureInfo);

        CreateAppRegistrationCredentials(azureInfo, githubInfo);

        GrantSubscriptionPermissionsToServicePrincipal(azureInfo);

        CreateAzureSqlServerSecurityGroup(azureInfo, githubInfo);

        CreateGithubSecretsAndVariables(githubInfo, azureInfo);

        CreateGithubEnvironments(githubInfo);

        DisableReusableWorkflows();

        TriggerAndMonitorWorkflows();

        PrintHeader("Configuration of GitHub and Azure completed 🎉");

        ShowSuccessMessage(githubInfo);

        return 0;
    }

    private void PrintHeader(string heading)
    {
        var separator = new string('-', Console.WindowWidth - heading.Length - 1);
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }

    private void ShowIntroPrompt(GithubInfo githubInfo, bool skipAzureLogin)
    {
        var loginToAzure = skipAzureLogin ? "" : "\n * Prompt you to log in to Azure and select a subscription";
        var loginToGitHub = githubInfo.IsLoggedIn() ? "" : "\n * Prompt you to log in to GitHub";

        var setupIntroPrompt =
            $"""
             This command will configure passwordless deployments from GitHub to Azure. If you continue, this command will do the following:

             {loginToAzure}{loginToGitHub}
              * Collect information about your Azure subscription and other settings for setting up continuous deployments
              * Confirm before you continue
              
             [bold]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm(setupIntroPrompt.Replace("\n\n", "\n"))) Environment.Exit(0);
        AnsiConsole.WriteLine();
    }

    private void SetGithubInfo(GithubInfo githubInfo)
    {
        var output = ProcessHelper.StartProcess("git remote -v", Configuration.GetSourceCodeFolder(), true);

        // Sort the output lines so that the "origin" is at the top
        output = string.Join('\n', output.Split('\n').OrderBy(line => line.Contains("origin") ? 0 : 1));

        var regex = new Regex(@"(?<githubUri>(https://github\.com/.*/.*\.git)|(git@github\.com:.*/.*\.git)) \(push\)");
        var matches = regex.Matches(output);

        var gitRemoteMatches = matches.Select(m => m.Groups["githubUri"].Value).ToArray();

        var githubUri = string.Empty;
        switch (gitRemoteMatches.Length)
        {
            case 0:
                AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. Please ensure you are within a Git repository with a GitHub.com as remote origin.[/]");
                Environment.Exit(0);
                break;
            case 1:
                githubUri = gitRemoteMatches.Single();
                break;
            case > 1:
                githubUri = AnsiConsole.Prompt(new SelectionPrompt<string>()
                    .Title("Select the GitHub remote")
                    .AddChoices(gitRemoteMatches)
                );
                ProcessHelper.StartProcess($"gh repo set-default {githubUri}");
                break;
        }

        githubInfo.InitializeFromUri(githubUri);
    }

    private void LoginToGithub(GithubInfo githubInfo)
    {
        if (!githubInfo.IsLoggedIn())
        {
            ProcessHelper.StartProcess("gh auth login --git-protocol https --web");

            if (!githubInfo.IsLoggedIn()) Environment.Exit(0);

            AnsiConsole.WriteLine();
        }

        var githubApiJson = ProcessHelper.StartProcess($"gh api repos/{githubInfo.Path}", redirectOutput: true);

        using var githubApi = JsonDocument.Parse(githubApiJson);

        githubApi.RootElement.TryGetProperty("permissions", out var githubRepositoryPermissions);
        if (!githubRepositoryPermissions.GetProperty("admin").GetBoolean())
        {
            AnsiConsole.MarkupLine("[red]ERROR: You do not have admin permissions on the repository. Please ensure you have the required permissions and try again. Run 'gh auth logout' to log in with a different account.[/]");
            Environment.Exit(0);
        }
    }

    private static void PublishGithubVariables(GithubInfo githubInfo)
    {
        var githubVariablesJson = ProcessHelper.StartProcess($"gh api repos/{githubInfo.Path}/actions/variables", redirectOutput: true);

        var githubVariables = JsonDocument.Parse(githubVariablesJson);
        foreach (var variable in githubVariables.RootElement.GetProperty("variables").EnumerateArray())
        {
            var variableName = variable.GetProperty("name").GetString()!;
            var variableValue = variable.GetProperty("value").GetString()!;
            githubInfo.Variables.Add(variableName, variableValue);
        }
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

        azureInfo.AppRegistrationName = $"GitHub - {githubInfo.OrganizationName}/{githubInfo.RepositoryName}";

        azureInfo.Subscription = subscription;
    }

    private void CollectUniquePrefix(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        githubInfo.Variables.TryGetValue("UNIQUE_PREFIX", out var uniquePrefix);

        AnsiConsole.MarkupLine(
            "When creating Azure resources like Azure Container Registry, SQL Server, Blob storage, Service Bus, Key Vaults, etc., a global unique name is required. To do this we use a prefix of 2-6 characters, which allows for flexibility for the rest of the name. E.g. if you select 'acme' the production SQL Server in West Europe will be named 'acme-prod-euw'."
        );

        var defaultValue = uniquePrefix
                           ?? githubInfo.OrganizationName!.ToLower().Substring(0, Math.Min(6, githubInfo.OrganizationName.Length));

        while (true)
        {
            uniquePrefix = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold]Please enter a unique prefix between 2-6 characters (e.g. an acronym for your product or company).[/]")
                    .DefaultValue(defaultValue)
                    .Validate(input =>
                        Regex.IsMatch(input, "^[a-z0-9]{2,6}$")
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]ERROR:[/]The unique prefix must be 2-6 characters and contain only lowercase characters a-z or 0-9.")
                    )
            );

            //  Check whether the Azure Container Registry name is available
            var checkAvailabilityStaging = RunAzureCliCommand($"acr check-name --name {uniquePrefix}stage --query \"nameAvailable\" -o tsv");
            var checkAvailabilityProduction = RunAzureCliCommand($"acr check-name --name {uniquePrefix}prod --query \"nameAvailable\" -o tsv");

            if (bool.Parse(checkAvailabilityStaging) && bool.Parse(checkAvailabilityProduction))
            {
                AnsiConsole.WriteLine();
                azureInfo.UniquePrefix = uniquePrefix;
                return;
            }

            AnsiConsole.MarkupLine(
                $"[red]ERROR:[/]An Azure Container Registry name [blue]{uniquePrefix}[/] is already in use, possibly in another subscription. Please enter a unique name."
            );
        }
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

    private void CollectExistingSqlAdminSecurityGroup(AzureInfo azureInfo, GithubInfo githubInfo)
    {
        var sqlAdminsSecurityGroupName = azureInfo.GetSqlAdminsSecurityGroupName(githubInfo);

        azureInfo.SqlAdminsSecurityGroupId = RunAzureCliCommand(
            $"""ad group list --display-name "{sqlAdminsSecurityGroupName}" --query "[].id" -o tsv"""
        ).Trim();

        if (azureInfo.SqlAdminsSecurityGroupId == string.Empty)
        {
            azureInfo.SqlAdminsSecurityGroupId = null;
            return;
        }

        AnsiConsole.MarkupLine(
            $"[yellow]The AD Security Group '{sqlAdminsSecurityGroupName}' already exists with ID: {azureInfo.SqlAdminsSecurityGroupId}[/]"
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

    private void CollectDomainNames(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        githubInfo.Variables.TryGetValue("DOMAIN_NAME_PRODUCTION", out var domainNameProduction);
        azureInfo.ProductionDomainName = domainNameProduction ?? "-";

        githubInfo.Variables.TryGetValue("DOMAIN_NAME_STAGING", out var domainNameStaging);
        azureInfo.StagingDomainName = domainNameStaging ?? "-";
    }

    private void ConfirmChangesPrompt(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        var appRegistrationAction = azureInfo.AppRegistrationExists ? "updated" : "created";
        var reuseSqlAdminsSecurityGroupAction = azureInfo.SqlAdminsSecurityGroupExists ? "updated" : "created";
        var sqlAdminsSecurityGroupName = azureInfo.GetSqlAdminsSecurityGroupName(githubInfo);

        var setupConfirmPrompt =
            $"""
             [bold]If you continue the following will happen:[/]

             1. The App Registration named [blue]{azureInfo.AppRegistrationName}[/] will be {appRegistrationAction} allowing GitHub to do passwordless deployments to Azure.

             2. The App Registration will be granted the 'Contributor' and 'User Access Administrator' roles in the Azure Subscription.

             3. The AD Security Group [blue]{sqlAdminsSecurityGroupName}[/] will be {reuseSqlAdminsSecurityGroupAction}, with the App Registration set as the owner.

             4. The GitHub Repository [blue]{githubInfo.GithubUrl}[/] will be configured with the following secrets and variables:
             
                GitHub Secrets (soft secrets):
                * AZURE_TENANT_ID: [blue]{azureInfo.Subscription.TenantId}[/]
                * AZURE_SUBSCRIPTION_ID: [blue]{azureInfo.Subscription.Id}[/]
                * AZURE_SERVICE_PRINCIPAL_ID: [blue]{azureInfo.AppRegistrationId ?? "will be generated"}[/]
                * ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID: [blue]{azureInfo.SqlAdminsSecurityGroupId ?? "will be generated"}[/]
             
                GitHub Variables:
                * UNIQUE_PREFIX: [blue]{azureInfo.UniquePrefix}[/]
                * DOMAIN_NAME_PRODUCTION: [blue]{azureInfo.ProductionDomainName}[/] ([yellow]set this manually in GitHub to add the production domain[/])
                * DOMAIN_NAME_STAGING: [blue]{azureInfo.StagingDomainName}[/] ([yellow]set this manually in GitHub to add the staging domain[/])

             5. The following environments will be created in the GitHub repository [blue]staging[/] and [blue]production[/] if they do not exist.

             6. The [blue]Cloud Infrastructure - Deployment[/] GitHub Action will be triggered to deploy Azure Infrastructure. This will take [yellow]between 30-45 minutes[/].

             7. Disable the reusable workflow [blue]Deploy Container[/].

             8. The [blue]Application - Build and Deploy[/] GitHub Action will be triggered to deploy the Application Code. This will take [yellow]less than 5 minutes[/].

             9. You will receive recommendations on how to further secure and optimize your setup.

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

    private void CreateAppRegistrationCredentials(AzureInfo azureInfo, GithubInfo githubInfo)
    {
        CreateFederatedCredential("MainBranch", "ref:refs/heads/main");
        CreateFederatedCredential("StagingEnvironment", "environment:staging");
        CreateFederatedCredential("ProductionEnvironment", "environment:production");

        AnsiConsole.MarkupLine(
            $"[green]Successfully created App Registration with Federated Credentials allowing passwordless deployments from {githubInfo.GithubUrl}.[/]"
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

    private void CreateAzureSqlServerSecurityGroup(AzureInfo azureInfo, GithubInfo githubInfo)
    {
        var sqlAdminsSecurityGroupName = azureInfo.GetSqlAdminsSecurityGroupName(githubInfo);
        if (!azureInfo.SqlAdminsSecurityGroupExists)
        {
            azureInfo.SqlAdminsSecurityGroupId = RunAzureCliCommand(
                $"""ad group create --display-name "{sqlAdminsSecurityGroupName}" --mail-nickname "{azureInfo.SqlAdminsSecurityGroupNickName}" --query "id" -o tsv"""
            ).Trim();
        }

        RunAzureCliCommand(
            $"ad group member add --group {azureInfo.SqlAdminsSecurityGroupId} --member-id {azureInfo.ServicePrincipalObjectId}",
            !Configuration.VerboseLogging
        );

        AnsiConsole.MarkupLine(
            $"[green]Successfully created AD Security Group '{sqlAdminsSecurityGroupName}' and granted the App Registration {azureInfo.AppRegistrationName} owner.[/]"
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
            $"gh variable set UNIQUE_PREFIX -b\"{azureInfo.UniquePrefix}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh variable set DOMAIN_NAME_PRODUCTION -b\"{azureInfo.ProductionDomainName}\" --repo={githubInfo.Path}"
        );
        ProcessHelper.StartProcess(
            $"gh variable set DOMAIN_NAME_STAGING -b\"{azureInfo.StagingDomainName}\" --repo={githubInfo.Path}"
        );

        AnsiConsole.MarkupLine("[green]Successfully created secrets in GitHub.[/]");
    }

    private void CreateGithubEnvironments(GithubInfo githubInfo)
    {
        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{githubInfo.Path}/environments/staging""",
            redirectOutput: true
        );

        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{githubInfo.Path}/environments/production""",
            redirectOutput: true
        );

        AnsiConsole.MarkupLine(
            "[green]Successfully created [bold]staging[/] and [bold]production[/] environments in GitHub repository.[/]"
        );
    }

    private void DisableReusableWorkflows()
    {
        // Disable reusable workflows
        DisableActiveWorkflow("Deploy Container");
        return;

        void DisableActiveWorkflow(string workflowName)
        {
            // Command to list workflows
            var listWorkflowsCommand = "gh workflow list --json name,state,id";
            var workflowsJson = ProcessHelper.StartProcess(listWorkflowsCommand, Configuration.GetSourceCodeFolder(), true);

            // Parse JSON to find the specific workflow and check if it's active
            using var jsonDocument = JsonDocument.Parse(workflowsJson);
            foreach (var element in jsonDocument.RootElement.EnumerateArray())
            {
                var name = element.GetProperty("name").GetString()!;
                var state = element.GetProperty("state").GetString()!;

                if (name != workflowName || state != "active") continue;

                // Disable the workflow if it is active
                var workflowId = element.GetProperty("id").GetInt64();
                var disableCommand = $"gh workflow disable {workflowId}";
                ProcessHelper.StartProcess(disableCommand, Configuration.GetSourceCodeFolder(), true);

                AnsiConsole.MarkupLine($"[green]Workflow {workflowName} has been disabled.[/]");

                break;
            }
        }
    }

    private void TriggerAndMonitorWorkflows()
    {
        StartGitHubWorkflow("Cloud Infrastructure - Deployment", "cloud-infrastructure.yml");
        StartGitHubWorkflow("Account Management - Build and Deploy", "account-management.yml");
        StartGitHubWorkflow("AppGateway - Build and Deploy", "app-gateway.yml");
        return;

        void StartGitHubWorkflow(string workflowName, string workflowFileName)
        {
            AnsiConsole.MarkupLine($"[green]Starting {workflowName} GitHub workflow...[/]");

            var runWorkflowCommand = $"gh workflow run {workflowFileName} --ref main";
            ProcessHelper.StartProcess(runWorkflowCommand, Configuration.GetSourceCodeFolder(), true);

            // Wait briefly to ensure the run has started
            Thread.Sleep(TimeSpan.FromSeconds(15));

            // Fetch and filter the workflows to find a "running" one
            var listWorkflowRunsCommand = $"gh run list --workflow={workflowFileName} --json databaseId,status";
            var workflowsJson = ProcessHelper.StartProcess(listWorkflowRunsCommand, Configuration.GetSourceCodeFolder(), true);

            long? workflowId = null;
            using (var jsonDocument = JsonDocument.Parse(workflowsJson))
            {
                foreach (var element in jsonDocument.RootElement.EnumerateArray())
                {
                    var status = element.GetProperty("status").GetString()!;
                    workflowId = element.GetProperty("databaseId").GetInt64();

                    if (status.Equals("in_progress", StringComparison.OrdinalIgnoreCase))
                    {
                        break;
                    }
                }
            }

            if (workflowId is null)
            {
                AnsiConsole.MarkupLine("[red]Failed to retrieve a running workflow ID.[/]");
                Environment.Exit(1);
            }

            var watchWorkflowRunCommand = $"gh run watch {workflowId.Value}";
            ProcessHelper.StartProcessWithSystemShell(watchWorkflowRunCommand, Configuration.GetSourceCodeFolder());
        }
    }

    private void ShowSuccessMessage(GithubInfo githubInfo)
    {
        var setupIntroPrompt =
            $"""
             So far so good. The configuration of GitHub and Azure is now complete. Here are some recommendations to further secure and optimize your setup:

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

    private string RunAzureCliCommand(string arguments, bool redirectOutput = true)
    {
        var azureCliCommand = Configuration.IsWindows ? "cmd.exe /C az" : "az";

        return ProcessHelper.StartProcess($"{azureCliCommand} {arguments}", redirectOutput: redirectOutput);
    }
}

public class GithubInfo
{
    public string? OrganizationName { get; private set; }

    public string? RepositoryName { get; private set; }

    public string? Path { get; private set; }

    public string GithubUrl => $"https://github.com/{OrganizationName}/{RepositoryName}";

    public Dictionary<string, string> Variables { get; } = new();

    public void InitializeFromUri(string gitUri)
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

    public bool IsLoggedIn()
    {
        var githubAuthStatus = ProcessHelper.StartProcess("gh auth status", redirectOutput: true);

        return githubAuthStatus.Contains("Logged in to github.com");
    }
}

public class AzureInfo
{
    public Subscription Subscription { get; set; } = default!;

    public string AppRegistrationName { get; set; } = default!;

    public string? AppRegistrationId { get; set; }

    public string ServicePrincipalId { get; set; } = default!;

    public string ServicePrincipalObjectId { get; set; } = default!;

    public bool AppRegistrationExists { get; set; }

    public string SqlAdminsSecurityGroupNickName => $"AzureSQLServerAdmins{UniquePrefix}";

    public string? SqlAdminsSecurityGroupId { get; set; }

    public bool SqlAdminsSecurityGroupExists { get; set; }

    public string ProductionDomainName { get; set; } = "-";

    public string StagingDomainName { get; set; } = "-";

    public string UniquePrefix { get; set; } = default!;

    public string GetSqlAdminsSecurityGroupName(GithubInfo githubInfo)
    {
        return $"Azure SQL Server Admins - {githubInfo.RepositoryName}";
    }
}

[UsedImplicitly]
public record Subscription(string Id, string Name, string TenantId, string State);
