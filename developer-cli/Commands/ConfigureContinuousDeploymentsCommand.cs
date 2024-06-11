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
    
    private static readonly Config Config = new();
    
    private static readonly Dictionary<string, string> AzureLocations = GetAzureLocations();
    
    public ConfigureContinuousDeploymentsCommand() : base(
        "configure-continuous-deployments",
        "Set up trust between Azure and GitHub for passwordless deployments using OpenID."
    )
    {
        AddOption(new Option<bool>(["--verbose-logging"], "Print Azure and GitHub CLI commands and output"));
        
        Handler = CommandHandler.Create<bool>(Execute);
    }
    
    private int Execute(bool verboseLogging = false)
    {
        PrerequisitesChecker.Check("dotnet", "az", "gh");
        
        Configuration.VerboseLogging = verboseLogging;
        
        PrintHeader("Introduction");
        
        ShowIntroPrompt();
        
        PrintHeader("Collecting data");
        
        SetGithubInfo();
        
        LoginToGithub();
        
        PublishExistingGithubVariables();
        
        ShowWarningIfGithubRepositoryIsAlreadyInitialized();
        
        SelectAzureSubscriptions();
        
        CollectLocations();
        
        CollectUniquePrefix();
        
        ConfirmReuseIfAppRegistrationsExists();
        
        ConfirmReuseIfSqlAdminSecurityGroupsExists();
        
        PrintHeader("Confirm changes");
        
        ConfirmChangesPrompt();
        
        var startNew = Stopwatch.StartNew();
        
        PrintHeader("Configuring Azure and GitHub");
        
        PrepareSubscriptionsForContainerAppsEnvironment();
        
        CreateAppRegistrationsIfNotExists();
        
        CreateAppRegistrationCredentials();
        
        GrantSubscriptionPermissionsToServicePrincipals();
        
        CreateAzureSqlServerSecurityGroups();
        
        CreateGithubEnvironments();
        
        CreateGithubSecretsAndVariables();
        
        DisableReusableWorkflows();
        
        TriggerAndMonitorWorkflows();
        
        PrintHeader($"Configuration of GitHub and Azure completed in {startNew.Elapsed:g} 🎉");
        
        ShowSuccessMessage();
        
        return 0;
    }
    
    private void PrintHeader(string heading)
    {
        var separator = new string('-', Console.WindowWidth - heading.Length - 1);
        AnsiConsole.MarkupLine($"\n[bold][green]{heading}[/] {separator}[/]\n");
    }
    
    private void ShowIntroPrompt()
    {
        var loginToGitHub = Config.IsLoggedIn() ? "" : " * Prompt you to log in to GitHub\n";
        
        var setupIntroPrompt =
            $"""
             This command will configure passwordless deployments from GitHub to Azure. If you continue, this command will do the following:
              
             {loginToGitHub} * Prompt you to log in to Azure and select a subscription
              * Collect information about your Azure subscription and other settings for setting up continuous deployments
              * Confirm before you continue
              
             [bold]Would you like to continue?[/]
             """;
        
        if (!AnsiConsole.Confirm(setupIntroPrompt.Replace("\n\n", "\n"))) Environment.Exit(0);
        AnsiConsole.WriteLine();
    }
    
    private void SetGithubInfo()
    {
        // Get all Git remotes
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
        
        Config.InitializeFromUri(githubUri);
    }
    
    private void LoginToGithub()
    {
        if (!Config.IsLoggedIn())
        {
            ProcessHelper.StartProcess("gh auth login --git-protocol https --web");
            
            if (!Config.IsLoggedIn()) Environment.Exit(0);
            
            AnsiConsole.WriteLine();
        }
        
        var githubApiJson = ProcessHelper.StartProcess($"gh api repos/{Config.GithubInfo?.Path}", redirectOutput: true);
        
        using var githubApi = JsonDocument.Parse(githubApiJson);
        
        githubApi.RootElement.TryGetProperty("permissions", out var githubRepositoryPermissions);
        if (!githubRepositoryPermissions.GetProperty("admin").GetBoolean())
        {
            AnsiConsole.MarkupLine("[red]ERROR: You do not have admin permissions on the repository. Please ensure you have the required permissions and try again. Run 'gh auth logout' to log in with a different account.[/]");
            Environment.Exit(0);
        }
    }
    
    private static void PublishExistingGithubVariables()
    {
        var githubVariablesJson = ProcessHelper.StartProcess(
            $"gh api repos/{Config.GithubInfo?.Path}/actions/variables --paginate",
            redirectOutput: true
        );
        
        var configGithubVariables = JsonDocument.Parse(githubVariablesJson).RootElement.GetProperty("variables").EnumerateArray();
        foreach (var variable in configGithubVariables)
        {
            var variableName = variable.GetProperty("name").GetString()!;
            var variableValue = variable.GetProperty("value").GetString()!;
            
            Config.GithubVariables.Add(variableName, variableValue);
        }
    }
    
    private static void ShowWarningIfGithubRepositoryIsAlreadyInitialized()
    {
        if (Config.GithubVariables.Count(variable => Enum.GetNames(typeof(VariableNames)).Contains(variable.Key)) == 0)
        {
            return;
        }
        
        AnsiConsole.MarkupLine("[yellow]This Github Repository has already been initialized. If you continue existing GitHub variables will be overridden.[/]");
        if (AnsiConsole.Confirm("Do you want to continue, and override existing GitHub variables?"))
        {
            AnsiConsole.WriteLine();
            return;
        }
        
        Environment.Exit(0);
    }
    
    private void SelectAzureSubscriptions()
    {
        // `az login` returns a JSON array of subscriptions
        var subscriptionListJson = RunAzureCliCommand("login");
        
        // Regular expression to match JSON part
        var jsonRegex = new Regex(@"\[.*\]", RegexOptions.Singleline);
        var match = jsonRegex.Match(subscriptionListJson);
        
        List<AzureSubscription>? azureSubscriptions = null;
        if (match.Success)
        {
            azureSubscriptions = JsonSerializer.Deserialize<List<AzureSubscription>>(match.Value, JsonSerializerOptions);
        }
        
        if (azureSubscriptions == null)
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] No subscriptions found.");
            Environment.Exit(1);
        }
        
        Config.StagingSubscription = SelectSubscription("Staging");
        Config.ProductionSubscription = SelectSubscription("Production");
        
        return;
        
        Subscription SelectSubscription(string environmentName)
        {
            var activeSubscriptions = azureSubscriptions.Where(s => s.State == "Enabled").ToList();
            
            var selectedDisplayName = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title($"[bold]Please select an Azure subscription for [yellow]{environmentName}[/][/]")
                .AddChoices(activeSubscriptions.Select(s => s.Name))
            );
            
            var selectedSubscriptions = activeSubscriptions.Where(s => s.Name == selectedDisplayName).ToArray();
            if (selectedSubscriptions.Length > 1)
            {
                AnsiConsole.MarkupLine($"[red]ERROR:[/] Found two subscriptions with the name {selectedDisplayName}.");
                Environment.Exit(1);
            }
            
            var azureSubscription = selectedSubscriptions.Single();
            
            return new Subscription(
                azureSubscription.Id,
                azureSubscription.Name,
                azureSubscription.TenantId,
                Config.GithubInfo!,
                environmentName
            );
        }
    }
    
    private void CollectLocations()
    {
        var location = CollectLocation();
        
        Config.StagingLocation = location;
        Config.ProductionLocation = location;
        
        Location CollectLocation()
        {
            var locationDisplayName = AnsiConsole.Prompt(new SelectionPrompt<string>()
                .Title("[bold]Please select a location where Azure Resource can be deployed [/]")
                .AddChoices(AzureLocations.Keys)
            );
            
            var locationAcronym = AzureLocations[locationDisplayName];
            var locationCode = locationDisplayName.Replace(" ", "").ToLower();
            return new Location(locationCode, locationCode, locationAcronym);
        }
    }
    
    private void CollectUniquePrefix()
    {
        var uniquePrefix = Config.GithubVariables.GetValueOrDefault(nameof(VariableNames.UNIQUE_PREFIX));
        
        AnsiConsole.MarkupLine(
            "When creating Azure resources like Azure Container Registry, SQL Server, Blob storage, Service Bus, Key Vaults, etc., a global unique name is required. To do this we use a prefix of 2-6 characters, which allows for flexibility for the rest of the name. E.g. if you select 'acme' the production SQL Server in West Europe will be named 'acme-prod-euw'."
        );
        
        if (uniquePrefix is not null)
        {
            AnsiConsole.MarkupLine($"[yellow]The unique prefix '{uniquePrefix}' already specified. Changing this will recreate all Azure resources![/]");
        }
        else
        {
            uniquePrefix = Config.GithubInfo!.OrganizationName.ToLower().Substring(0, Math.Min(6, Config.GithubInfo.OrganizationName.Length));
        }
        
        while (true)
        {
            uniquePrefix = AnsiConsole.Prompt(
                new TextPrompt<string>("[bold]Please enter a unique prefix between 2-6 characters (e.g. an acronym for your product or company).[/]")
                    .DefaultValue(uniquePrefix)
                    .Validate(input =>
                        Regex.IsMatch(input, "^[a-z0-9]{2,6}$")
                            ? ValidationResult.Success()
                            : ValidationResult.Error("[red]ERROR:[/]The unique prefix must be 2-6 characters and contain only lowercase letters a-z or numbers 0-9.")
                    )
            );
            
            if (IsContainerRegistryConflicting(Config.StagingSubscription.Id, Config.StagingLocation.SharedLocation, $"{uniquePrefix}-stage", $"{uniquePrefix}stage") ||
                IsContainerRegistryConflicting(Config.ProductionSubscription.Id, Config.ProductionLocation.SharedLocation, $"{uniquePrefix}-prod", $"{uniquePrefix}prod"))
            {
                AnsiConsole.MarkupLine(
                    "[red]ERROR:[/]Azure resources conflicting with this prefix is already in use, possibly in [bold]another subscription[/] or in [bold]another location[/]. Please enter a unique name."
                );
                continue;
            }
            
            AnsiConsole.WriteLine();
            Config.UniquePrefix = uniquePrefix;
            return;
        }
        
        bool IsContainerRegistryConflicting(string subscriptionId, string location, string resourceGroup, string azureContainerRegistryName)
        {
            var checkAvailability = RunAzureCliCommand($"acr check-name --name {azureContainerRegistryName} --query \"nameAvailable\" -o tsv");
            if (bool.Parse(checkAvailability)) return false;
            
            var showExistingRegistry = RunAzureCliCommand($"acr show --name {azureContainerRegistryName} --subscription {subscriptionId} --output json");
            
            var jsonRegex = new Regex(@"\{.*\}", RegexOptions.Singleline);
            var match = jsonRegex.Match(showExistingRegistry);
            
            if (!match.Success) return true;
            var jsonDocument = JsonDocument.Parse(match.Value);
            var sameSubscription = jsonDocument.RootElement.GetProperty("id").GetString()?.Contains(subscriptionId) == true;
            var sameResourceGroup = jsonDocument.RootElement.GetProperty("resourceGroup").GetString() == resourceGroup;
            var sameLocation = jsonDocument.RootElement.GetProperty("location").GetString() == location;
            
            return !(sameSubscription && sameResourceGroup && sameLocation);
        }
    }
    
    private void ConfirmReuseIfAppRegistrationsExists()
    {
        ConfirmReuseIfAppRegistrationExist(Config.StagingSubscription.AppRegistration);
        ConfirmReuseIfAppRegistrationExist(Config.ProductionSubscription.AppRegistration);
        return;
        
        void ConfirmReuseIfAppRegistrationExist(AppRegistration appRegistration)
        {
            appRegistration.AppRegistrationId = RunAzureCliCommand(
                $"""ad app list --display-name "{appRegistration.Name}" --query "[].appId" -o tsv"""
            ).Trim();
            
            appRegistration.ServicePrincipalId = RunAzureCliCommand(
                $"""ad sp list --display-name "{appRegistration.Name}" --query "[].appId" -o tsv"""
            ).Trim();
            
            appRegistration.ServicePrincipalObjectId = RunAzureCliCommand(
                $"""ad sp list --filter "appId eq '{appRegistration.AppRegistrationId}'" --query "[].id" -o tsv"""
            ).Trim();
            
            if (appRegistration.AppRegistrationId != string.Empty && appRegistration.ServicePrincipalId != string.Empty)
            {
                AnsiConsole.MarkupLine(
                    $"[yellow]The App Registration '{appRegistration.Name}' already exists with App ID: {appRegistration.ServicePrincipalId}[/]"
                );
                
                if (AnsiConsole.Confirm("The existing App Registration will be reused. Do you want to continue?"))
                {
                    AnsiConsole.WriteLine();
                    return;
                }
                
                AnsiConsole.MarkupLine("[red]Please delete the existing App Registration and try again.[/]");
                Environment.Exit(1);
            }
            
            if (appRegistration.AppRegistrationId != string.Empty || appRegistration.ServicePrincipalId != string.Empty)
            {
                AnsiConsole.MarkupLine($"[red]The App Registration or Service Principal '{appRegistration}' exists, but not both. Please manually delete and retry.[/]");
                Environment.Exit(1);
            }
        }
    }
    
    private void ConfirmReuseIfSqlAdminSecurityGroupsExists()
    {
        Config.StagingSubscription.SqlAdminsGroup.ObjectId = ConfirmReuseIfSqlAdminSecurityGroupExist(Config.StagingSubscription.SqlAdminsGroup.Name);
        Config.ProductionSubscription.SqlAdminsGroup.ObjectId = ConfirmReuseIfSqlAdminSecurityGroupExist(Config.ProductionSubscription.SqlAdminsGroup.Name);
        
        string? ConfirmReuseIfSqlAdminSecurityGroupExist(string sqlAdminsSecurityGroupName)
        {
            var sqlAdminsObjectId = RunAzureCliCommand(
                $"""ad group list --display-name "{sqlAdminsSecurityGroupName}" --query "[].id" -o tsv"""
            ).Trim();
            
            if (sqlAdminsObjectId == string.Empty)
            {
                return null;
            }
            
            AnsiConsole.MarkupLine(
                $"[yellow]The AD Security Group '{sqlAdminsSecurityGroupName}' already exists with ID: {sqlAdminsObjectId}[/]"
            );
            
            if (AnsiConsole.Confirm("The existing AD Security Group will be reused. Do you want to continue?") == false)
            {
                AnsiConsole.MarkupLine("[red]Please delete the existing AD Security Group and try again.[/]");
                Environment.Exit(0);
            }
            
            AnsiConsole.WriteLine();
            
            return sqlAdminsObjectId;
        }
    }
    
    private void ConfirmChangesPrompt()
    {
        var stagingServicePrincipal = Config.StagingSubscription.AppRegistration.Exists
            ? Config.StagingSubscription.AppRegistration.ServicePrincipalId
            : "Will be generated";
        var productionServicePrincipal = Config.ProductionSubscription.AppRegistration.Exists
            ? Config.ProductionSubscription.AppRegistration.ServicePrincipalId
            : "Will be generated";
        var stagingSqlAdminObject = Config.StagingSubscription.SqlAdminsGroup.Exists
            ? Config.StagingSubscription.SqlAdminsGroup.ObjectId
            : "Will be generated";
        var productionSqlAdminObject = Config.ProductionSubscription.SqlAdminsGroup.Exists
            ? Config.ProductionSubscription.SqlAdminsGroup.ObjectId
            : "Will be generated";
        
        var setupConfirmPrompt =
            $"""
             [bold]Please review planned changes before continuing.[/]
             
             1. The following will be created or updated in Azure:
             
                [bold]Active Directory App Registrations/Service Principals:[/]
                * [blue]{Config.StagingSubscription.AppRegistration.Name}[/] with access to the [blue]{Config.StagingSubscription.Name}[/] subscription.
                * [blue]{Config.ProductionSubscription.AppRegistration.Name}[/] with access to the [blue]{Config.ProductionSubscription.Name}[/] subscription.
                
                [yellow]** The Service Principals will get 'Contributor' and 'User Access Administrator' role on the Azure Subscriptions.[/]
                
                [bold]Active Directory Security Groups:[/]
                * [blue]{Config.StagingSubscription.SqlAdminsGroup.Name}[/]
                * [blue]{Config.ProductionSubscription.SqlAdminsGroup.Name}[/]
                
                [yellow]** The SQL Admins Security Groups are used to grant Managed Identities and CI/CD permissions to SQL Databases.[/]
             
             2. The following GitHub environments will be created if not exists:
                * [blue]staging[/]
                * [blue]production[/]
             
                [yellow]** Environments are used to require approval when infrastructure is deployed. In private GitHub repositories, this requires a paid plan.[/]
             
             3. The following GitHub repository variables will be created:
             
                [bold]Shared Variables:[/]
                * TENANT_ID: [blue]{Config.TenantId}[/]
                * UNIQUE_PREFIX: [blue]{Config.UniquePrefix}[/]
              
                [bold]Staging Shared Variables:[/]
                * STAGING_SUBSCRIPTION_ID: [blue]{Config.StagingSubscription.Id}[/]
                * STAGING_SHARED_LOCATION: [blue]{Config.StagingLocation.SharedLocation}[/]
                * STAGING_SERVICE_PRINCIPAL_ID: [blue]{stagingServicePrincipal}[/]
                * STAGING_SQL_ADMIN_OBJECT_ID: [blue]{stagingSqlAdminObject}[/] 
                * STAGING_DOMAIN_NAME: [blue]-[/] ([yellow]Manually changed this and triggered deployment to set up the domain[/])
              
                [bold]Staging Cluster Variables:[/]
                * STAGING_CLUSTER_ENABLED: [blue]true[/]
                * STAGING_CLUSTER_LOCATION: [blue]{Config.StagingLocation.ClusterLocation}[/]
                * STAGING_CLUSTER_LOCATION_ACRONYM: [blue]{Config.StagingLocation.ClusterLocationAcronym}[/]
              
                [bold]Production Shared Variables:[/]
                * PRODUCTION_SUBSCRIPTION_ID: [blue]{Config.ProductionSubscription.Id}[/]
                * PRODUCTION_SHARED_LOCATION: [blue]{Config.ProductionLocation.SharedLocation}[/]
                * PRODUCTION_SERVICE_PRINCIPAL_ID: [blue]{productionServicePrincipal}[/]
                * PRODUCTION_SQL_ADMIN_OBJECT_ID: [blue]{productionSqlAdminObject}[/] 
                * PRODUCTION_DOMAIN_NAME: [blue]-[/] ([yellow]Manually changed this and triggered deployment to set up the domain[/])
                
                [bold]Production Cluster 1 Variables:[/]
                * PRODUCTION_CLUSTER1_ENABLED: [blue]false[/] ([yellow]Change this to 'true' when ready to deploy to production[/])
                * PRODUCTION_CLUSTER1_LOCATION: [blue]{Config.ProductionLocation.ClusterLocation}[/]
                * PRODUCTION_CLUSTER1_LOCATION_ACRONYM: [blue]{Config.ProductionLocation.ClusterLocationAcronym}[/]
                 
                [yellow]** All variables can be changed on the GitHub Settings page. For example, if you want to deploy production or staging to different locations.[/]
             
             4. Disable the reusable GitHub workflows [blue]Deploy Container[/] and [blue]Plan and Deploy Infrastructure[/].
             
             5. The [blue]Cloud Infrastructure - Deployment[/] GitHub Actions will be triggered deployment of Azure Infrastructure. This will take [yellow]between 15 and 45 minutes[/].
             
             6. The [blue]Build and Deploy[/] GitHub Action will be triggered to deploy the Application Code. This will take [yellow]between 5 and 10 minutes[/].
             
             7. You will receive recommendations on how to further secure and optimize your setup.
             
             [bold]Would you like to continue?[/]
             """;
        
        if (!AnsiConsole.Confirm($"{setupConfirmPrompt}", false)) Environment.Exit(0);
    }
    
    private void PrepareSubscriptionsForContainerAppsEnvironment()
    {
        PrepareSubscription(Config.StagingSubscription.Id);
        PrepareSubscription(Config.ProductionSubscription.Id);
        
        AnsiConsole.MarkupLine("[green]Successfully ensured deployment of Azure Container Apps Environment is enabled on Azure Subscriptions.[/]");
        return;
        
        void PrepareSubscription(string subscriptionId)
        {
            RunAzureCliCommand(
                $"provider register --namespace Microsoft.ContainerService --subscription {subscriptionId}",
                !Configuration.VerboseLogging
            );
        }
    }
    
    private void CreateAppRegistrationsIfNotExists()
    {
        if (!Config.StagingSubscription.AppRegistration.Exists)
        {
            CreateAppRegistration(Config.StagingSubscription.AppRegistration);
        }
        
        if (!Config.ProductionSubscription.AppRegistration.Exists)
        {
            CreateAppRegistration(Config.ProductionSubscription.AppRegistration);
        }
        
        return;
        
        void CreateAppRegistration(AppRegistration appRegistration)
        {
            appRegistration.AppRegistrationId = RunAzureCliCommand(
                $"""ad app create --display-name "{appRegistration.Name}" --query appId -o tsv"""
            ).Trim();
            
            appRegistration.ServicePrincipalId = RunAzureCliCommand(
                $"ad sp create --id {appRegistration.AppRegistrationId} --query appId -o tsv"
            ).Trim();
            
            appRegistration.ServicePrincipalObjectId = RunAzureCliCommand(
                $"""ad sp list --filter "appId eq '{appRegistration.AppRegistrationId}'" --query "[].id" -o tsv"""
            ).Trim();
            
            AnsiConsole.MarkupLine(
                $"[green]Successfully created an App Registration '{appRegistration.Name}' ({appRegistration.AppRegistrationId}).[/]"
            );
        }
    }
    
    private void CreateAppRegistrationCredentials()
    {
        // Staging
        CreateFederatedCredential(Config.StagingSubscription.AppRegistration.AppRegistrationId!, "MainBranch", "ref:refs/heads/main");
        CreateFederatedCredential(Config.StagingSubscription.AppRegistration.AppRegistrationId!, "StagingEnvironment", "environment:staging");
        CreateFederatedCredential(Config.StagingSubscription.AppRegistration.AppRegistrationId!, "PullRequests", "pull_request");
        
        // Production
        CreateFederatedCredential(Config.ProductionSubscription.AppRegistration.AppRegistrationId!, "MainBranch", "ref:refs/heads/main");
        CreateFederatedCredential(Config.ProductionSubscription.AppRegistration.AppRegistrationId!, "ProductionEnvironment", "environment:production");
        
        AnsiConsole.MarkupLine(
            $"[green]Successfully created App Registration with Federated Credentials allowing passwordless deployments from {Config.GithubInfo?.Url}.[/]"
        );
        
        void CreateFederatedCredential(string appRegistrationId, string displayName, string refRefsHeadsMain)
        {
            var parameters = JsonSerializer.Serialize(new
                {
                    name = displayName,
                    issuer = "https://token.actions.githubusercontent.com",
                    subject = $"""repo:{Config.GithubInfo?.Path}:{refRefsHeadsMain}""",
                    audiences = new[] { "api://AzureADTokenExchange" }
                }
            );
            
            ProcessHelper.StartProcess(new ProcessStartInfo
                {
                    FileName = Configuration.IsWindows ? "cmd.exe" : "az",
                    Arguments =
                        $"{(Configuration.IsWindows ? "/C az" : string.Empty)} ad app federated-credential create --id {appRegistrationId} --parameters  @-",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = !Configuration.VerboseLogging,
                    RedirectStandardError = !Configuration.VerboseLogging
                }, parameters
            );
        }
    }
    
    private void GrantSubscriptionPermissionsToServicePrincipals()
    {
        GrantAccess(Config.StagingSubscription, Config.StagingSubscription.AppRegistration.Name);
        GrantAccess(Config.ProductionSubscription, Config.ProductionSubscription.AppRegistration.Name);
        
        void GrantAccess(Subscription subscription, string appRegistrationName)
        {
            var servicePrincipalId = subscription.AppRegistration.ServicePrincipalId!;
            
            RunAzureCliCommand(
                $"role assignment create --assignee {servicePrincipalId} --role \"Contributor\" --scope /subscriptions/{subscription.Id}",
                !Configuration.VerboseLogging
            );
            RunAzureCliCommand(
                $"role assignment create --assignee {servicePrincipalId} --role \"User Access Administrator\" --scope /subscriptions/{subscription.Id}",
                !Configuration.VerboseLogging
            );
            
            AnsiConsole.MarkupLine(
                $"[green]Successfully granted Service Principal ('{appRegistrationName}') 'Contributor' and `User Access Administrator` rights to Azure Subscription '{subscription.Name}'.[/]"
            );
        }
    }
    
    private void CreateAzureSqlServerSecurityGroups()
    {
        CreateAzureSqlServerSecurityGroup(Config.StagingSubscription.SqlAdminsGroup, Config.StagingSubscription.AppRegistration);
        CreateAzureSqlServerSecurityGroup(Config.ProductionSubscription.SqlAdminsGroup, Config.ProductionSubscription.AppRegistration);
        
        void CreateAzureSqlServerSecurityGroup(SqlAdminsGroup sqlAdminGroup, AppRegistration appRegistration)
        {
            if (!sqlAdminGroup.Exists)
            {
                sqlAdminGroup.ObjectId = RunAzureCliCommand(
                    $"""ad group create --display-name "{sqlAdminGroup.Name}" --mail-nickname "{sqlAdminGroup.NickName}" --query "id" -o tsv"""
                ).Trim();
            }
            
            RunAzureCliCommand(
                $"ad group member add --group {sqlAdminGroup.ObjectId} --member-id {appRegistration.ServicePrincipalObjectId}",
                !Configuration.VerboseLogging
            );
            
            AnsiConsole.MarkupLine(
                $"[green]Successfully created AD Security Group '{sqlAdminGroup.Name}' and assigned the App Registration '{appRegistration.Name}' owner role.[/]"
            );
        }
    }
    
    private void CreateGithubEnvironments()
    {
        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{Config.GithubInfo?.Path}/environments/staging""",
            redirectOutput: true
        );
        
        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{Config.GithubInfo?.Path}/environments/production""",
            redirectOutput: true
        );
        
        AnsiConsole.MarkupLine(
            "[green]Successfully created 'staging' and 'production' environments in the GitHub repository.[/]"
        );
    }
    
    private void CreateGithubSecretsAndVariables()
    {
        SetGithubVariable(VariableNames.TENANT_ID, Config.TenantId);
        SetGithubVariable(VariableNames.UNIQUE_PREFIX, Config.UniquePrefix);
        
        SetGithubVariable(VariableNames.STAGING_SUBSCRIPTION_ID, Config.StagingSubscription.Id);
        SetGithubVariable(VariableNames.STAGING_SERVICE_PRINCIPAL_ID, Config.StagingSubscription.AppRegistration.ServicePrincipalId!);
        SetGithubVariable(VariableNames.STAGING_SHARED_LOCATION, Config.StagingLocation.SharedLocation);
        SetGithubVariable(VariableNames.STAGING_SQL_ADMIN_OBJECT_ID, Config.StagingSubscription.SqlAdminsGroup.ObjectId!);
        SetGithubVariable(VariableNames.STAGING_DOMAIN_NAME, "-");
        
        SetGithubVariable(VariableNames.STAGING_CLUSTER_ENABLED, "true");
        SetGithubVariable(VariableNames.STAGING_CLUSTER_LOCATION, Config.StagingLocation.ClusterLocation);
        SetGithubVariable(VariableNames.STAGING_CLUSTER_LOCATION_ACRONYM, Config.StagingLocation.ClusterLocationAcronym);
        
        SetGithubVariable(VariableNames.PRODUCTION_SUBSCRIPTION_ID, Config.ProductionSubscription.Id);
        SetGithubVariable(VariableNames.PRODUCTION_SERVICE_PRINCIPAL_ID, Config.ProductionSubscription.AppRegistration.ServicePrincipalId!);
        SetGithubVariable(VariableNames.PRODUCTION_SHARED_LOCATION, Config.ProductionLocation.SharedLocation);
        SetGithubVariable(VariableNames.PRODUCTION_SQL_ADMIN_OBJECT_ID, Config.ProductionSubscription.SqlAdminsGroup.ObjectId!);
        SetGithubVariable(VariableNames.PRODUCTION_DOMAIN_NAME, "-");
        
        SetGithubVariable(VariableNames.PRODUCTION_CLUSTER1_ENABLED, "false");
        SetGithubVariable(VariableNames.PRODUCTION_CLUSTER1_LOCATION, Config.ProductionLocation.ClusterLocation);
        SetGithubVariable(VariableNames.PRODUCTION_CLUSTER1_LOCATION_ACRONYM, Config.ProductionLocation.ClusterLocationAcronym);
        
        AnsiConsole.MarkupLine("[green]Successfully created secrets in GitHub.[/]");
        return;
        
        void SetGithubVariable(VariableNames name, string value)
        {
            ProcessHelper.StartProcess($"gh variable set {Enum.GetName(name)} -b\"{value}\" --repo={Config.GithubInfo?.Path}");
        }
    }
    
    private void DisableReusableWorkflows()
    {
        // Disable reusable workflows
        DisableActiveWorkflow("Deploy Container");
        DisableActiveWorkflow("Plan and Deploy Infrastructure");
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
                
                AnsiConsole.MarkupLine($"[green]Reusable Git Workflow '{workflowName}' has been disabled.[/]");
                
                break;
            }
        }
    }
    
    private void TriggerAndMonitorWorkflows()
    {
        AnsiConsole.Status().Start("Begin deployment.", ctx =>
            {
                for (var i = 60; i >= 0; i--)
                {
                    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Enter)
                    {
                        break;
                    }
                    
                    ctx.Status($"Deployment of Cloud Infrastructure and Application code will automatically start in {i} seconds. Press 'Ctrl+C' to exit or 'Enter' to continue.");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        );
        
        StartGithubWorkflow("Cloud Infrastructure - Deployment", "cloud-infrastructure.yml");
        StartGithubWorkflow("Account Management - Build and Deploy", "account-management.yml");
        StartGithubWorkflow("AppGateway - Build and Deploy", "app-gateway.yml");
        return;
        
        void StartGithubWorkflow(string workflowName, string workflowFileName)
        {
            try
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
                        
                        if (status.Equals("in_progress", StringComparison.OrdinalIgnoreCase))
                        {
                            workflowId = element.GetProperty("databaseId").GetInt64();
                            break;
                        }
                    }
                }
                
                if (workflowId is null)
                {
                    AnsiConsole.MarkupLine("[red]Failed to retrieve a running workflow ID. Please check the GitHub Actions page for more info.[/]");
                    Environment.Exit(1);
                }
                
                var watchWorkflowRunCommand = $"gh run watch {workflowId.Value}";
                ProcessHelper.StartProcessWithSystemShell(watchWorkflowRunCommand, Configuration.GetSourceCodeFolder());
                
                // Run the command one more time to get the result
                var runResult = ProcessHelper.StartProcess(watchWorkflowRunCommand, Configuration.GetSourceCodeFolder(), true);
                if (runResult.Contains("completed") && runResult.Contains("success")) return;
                
                AnsiConsole.MarkupLine($"[red]Error: Failed to run the {workflowName} GitHub workflow.[/]");
                AnsiConsole.MarkupLine($"[red]{runResult}[/]");
                Environment.Exit(1);
            }
            catch (Exception)
            {
                AnsiConsole.MarkupLine($"[red]Error: Failed to run the '{workflowName}' GitHub workflow.[/]");
                Environment.Exit(1);
            }
        }
    }
    
    private void ShowSuccessMessage()
    {
        var setupIntroPrompt =
            $"""
             So far so good. The configuration of GitHub and Azure is now complete. Here are some recommendations to further secure and optimize your setup:
             
             - For protecting the [blue]main[/] branch, configure branch protection rules to necessitate pull request reviews before merging can occur. Visit [blue]{Config.GithubInfo?.Url}/settings/branches[/], click ""Add Branch protection rule"", and set it up for the [bold]main[/] branch. Requires a paid GitHub plan for private repositories.
             
             - To add a step for manual approval during infrastructure deployment to the staging and production environments, set up required reviewers on GitHub environments. Visit [blue]{Config.GithubInfo?.Url}/settings/environments[/] and enable [blue]Required reviewers[/] for the [bold]staging[/] and [bold]production[/] environments. Requires a paid GitHub plan for private repositories.
             
             - Configure the Domain Name for the staging and production environments. This involves two steps:
                 
                 a. Go to [blue]{Config.GithubInfo?.Url}/settings/variables/actions[/] to set the [blue]DOMAIN_NAME_STAGING[/] and [blue]DOMAIN_NAME_PRODUCTION[/] variables. E.g. [blue]staging.your-saas-company.com[/] and [blue]your-saas-company.com[/].
                 
                 b. Run the [blue]Cloud Infrastructure - Deployment[/] workflow again. Note that it might fail with an error message to set up a DNS TXT and CNAME record. Once done, re-run the failed jobs.
             
             - Set up SonarCloud for code quality and security analysis. This service is free for public repositories. Visit [blue]https://sonarcloud.io[/] to connect your GitHub account. Add the [blue]SONAR_TOKEN[/] secret, and the [blue]SONAR_ORGANIZATION[/] and [blue]SONAR_PROJECT_KEY[/] variables to the GitHub repository. The workflows are already configured for SonarCloud analysis.
             
             - Enable Microsoft Defender for Cloud (also known as Azure Security Center) once the system evolves for added security recommendations.
             """;
        
        AnsiConsole.MarkupLine($"{setupIntroPrompt}");
        AnsiConsole.WriteLine();
    }
    
    private string RunAzureCliCommand(string arguments, bool redirectOutput = true)
    {
        var azureCliCommand = Configuration.IsWindows ? "cmd.exe /C az" : "az";
        
        return ProcessHelper.StartProcess($"{azureCliCommand} {arguments}", redirectOutput: redirectOutput);
    }
    
    private static Dictionary<string, string> GetAzureLocations()
    {
        // List of global available regions extracted by running:
        //  "az account list-locations --query "[?metadata.regionType == 'Physical'].{DisplayName:displayName}" --output table
        // Location Acronyms are taken from here https://learn.microsoft.com/en-us/azure/backup/scripts/geo-code-list
        return new Dictionary<string, string>
        {
            { "Australia Central", "acl" },
            { "Australia Central 2", "acl2" },
            { "Australia East", "ae" },
            { "Australia Southeast", "ase" },
            { "Brazil South", "brs" },
            { "Brazil Southeast", "bse" },
            { "Canada Central", "cnc" },
            { "Canada East", "cne" },
            { "Central India", "inc" },
            { "Central US", "cus" },
            { "East Asia", "ea" },
            { "East US", "eus" },
            { "East US 2", "eus2" },
            { "France Central", "frc" },
            { "France South", "frs" },
            { "Germany North", "gn" },
            { "Germany West Central", "gwc" },
            { "Japan East", "jpe" },
            { "Japan West", "jpw" },
            { "Jio India Central", "jic" },
            { "Jio India West", "jiw" },
            { "Korea Central", "krc" },
            { "Korea South", "krs" },
            { "North Central US", "ncus" },
            { "North Europe", "ne" },
            { "Norway East", "nwe" },
            { "Norway West", "nww" },
            { "South Africa North", "san" },
            { "South Africa West", "saw" },
            { "South Central US", "scus" },
            { "South India", "ins" },
            { "Southeast Asia", "sea" },
            { "Sweden Central", "sdc" },
            { "Switzerland North", "szn" },
            { "Switzerland West", "szw" },
            { "UAE Central", "uac" },
            { "UAE North", "uan" },
            { "UK South", "uks" },
            { "UK West", "ukw" },
            { "West Central US", "wcus" },
            { "West Europe", "we" },
            { "West India", "inw" },
            { "West US", "wus" },
            { "West US 2", "wus2" },
            { "West US 3", "wus3" }
        };
    }
}

public class Config
{
    public string TenantId => StagingSubscription.TenantId;
    
    public string UniquePrefix { get; set; } = default!;
    
    public GithubInfo? GithubInfo { get; private set; }
    
    public Subscription StagingSubscription { get; set; } = default!;
    
    public Location StagingLocation { get; set; } = default!;
    
    public Subscription ProductionSubscription { get; set; } = default!;
    
    public Location ProductionLocation { get; set; } = default!;
    
    public Dictionary<string, string> GithubVariables { get; set; } = new();
    
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
        GithubInfo = new GithubInfo(parts[0], parts[1]);
    }
    
    public bool IsLoggedIn()
    {
        var githubAuthStatus = ProcessHelper.StartProcess("gh auth status", redirectOutput: true);
        
        return githubAuthStatus.Contains("Logged in to github.com");
    }
}

public class GithubInfo(string organizationName, string repositoryName)
{
    public string OrganizationName { get; } = organizationName;
    
    public string RepositoryName { get; } = repositoryName;
    
    public string Path => $"{OrganizationName}/{RepositoryName}";
    
    public string Url => $"https://github.com/{Path}";
}

public record AzureSubscription(string Id, string Name, string TenantId, string State);

public class Subscription(string id, string name, string tenantId, GithubInfo githubInfo, string environmentName)
{
    public string Id { get; } = id;
    
    public string Name { get; } = name;
    
    public string TenantId { get; } = tenantId;
    
    public AppRegistration AppRegistration { get; } = new(githubInfo, environmentName);
    
    public SqlAdminsGroup SqlAdminsGroup { get; } = new(githubInfo, environmentName);
}

public class AppRegistration(GithubInfo githubInfo, string environmentName)
{
    public string Name => $"GitHub - {githubInfo.OrganizationName}/{githubInfo.RepositoryName} - {environmentName}";
    
    public bool Exists => !string.IsNullOrEmpty(AppRegistrationId);
    
    public string? AppRegistrationId { get; set; }
    
    public string? ServicePrincipalId { get; set; }
    
    public string? ServicePrincipalObjectId { get; set; }
}

public class SqlAdminsGroup(GithubInfo githubInfo, string enviromentName)
{
    public string Name => $"SQL Admins - {githubInfo.OrganizationName}/{githubInfo.RepositoryName} - {enviromentName}";
    
    public string NickName => $"SQLServerAdmins{githubInfo.OrganizationName}{githubInfo.RepositoryName}{enviromentName}";
    
    public bool Exists => !string.IsNullOrEmpty(ObjectId);
    
    public string? ObjectId { get; set; }
}

public record Location(string SharedLocation, string ClusterLocation, string ClusterLocationAcronym);

public enum VariableNames
{
    // ReSharper disable InconsistentNaming
    TENANT_ID,
    UNIQUE_PREFIX,
    
    STAGING_SUBSCRIPTION_ID,
    STAGING_SERVICE_PRINCIPAL_ID,
    STAGING_SHARED_LOCATION,
    STAGING_SQL_ADMIN_OBJECT_ID,
    STAGING_DOMAIN_NAME,
    
    STAGING_CLUSTER_ENABLED,
    STAGING_CLUSTER_LOCATION,
    STAGING_CLUSTER_LOCATION_ACRONYM,
    
    PRODUCTION_SUBSCRIPTION_ID,
    PRODUCTION_SERVICE_PRINCIPAL_ID,
    PRODUCTION_SHARED_LOCATION,
    PRODUCTION_SQL_ADMIN_OBJECT_ID,
    PRODUCTION_DOMAIN_NAME,
    
    PRODUCTION_CLUSTER1_ENABLED,
    PRODUCTION_CLUSTER1_LOCATION,
    
    PRODUCTION_CLUSTER1_LOCATION_ACRONYM
    // ReSharper restore InconsistentNaming
}
