using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PlatformPlatform.DeveloperCli.Installation;
using PlatformPlatform.DeveloperCli.Utilities;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

public class ConfigureContinuousDeploymentsCommand : Command
{
    private static readonly JsonSerializerOptions? JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly Config Config = new();

    private static readonly Dictionary<string, string> AzureLocations = GetAzureLocations();

    private List<ConfigureContinuousDeployments>? _configureContinuousDeploymentsExtensions;

    public ConfigureContinuousDeploymentsCommand() : base(
        "configure-continuous-deployments",
        "Set up trust between Azure and GitHub for passwordless deployments using OpenID Connect"
    )
    {
        AddOption(new Option<bool>(["--verbose-logging"], "Print Azure and GitHub CLI commands and output"));

        Handler = CommandHandler.Create<bool>(Execute);
    }

    private int Execute(bool verboseLogging = false)
    {
        Prerequisite.Ensure(Prerequisite.Dotnet, Prerequisite.AzureCli, Prerequisite.GithubCli);

        Configuration.VerboseLogging = verboseLogging;

        _configureContinuousDeploymentsExtensions = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(ConfigureContinuousDeployments)))
            .Select(t => Activator.CreateInstance(t) as ConfigureContinuousDeployments)
            .Where(t => t != null)
            .ToList()!;

        PrintHeader("Introduction");

        ShowIntroPrompt();

        PrintHeader("Collecting data");

        SetGithubInfo();

        LoginToGithub();

        EnsureGithubWorkflowsAreEnabled();

        PublishExistingGithubVariables();

        ShowWarningIfGithubRepositoryIsAlreadyInitialized();

        SelectAzureSubscriptions();

        CollectLocations();

        CollectUniquePrefix();

        ConfirmReuseIfAppRegistrationsExist();

        ConfirmReuseIfSqlAdminSecurityGroupExists();

        CollectAdditionalInfo();

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

        ApplyAdditionalConfigurations();

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
        Config.SetGithubInfo();
    }

    private void LoginToGithub()
    {
        if (!Config.IsLoggedIn())
        {
            ProcessHelper.StartProcess("gh auth login --git-protocol https --web");

            if (!Config.IsLoggedIn()) Environment.Exit(0);

            AnsiConsole.WriteLine();
        }

        var githubApiJson = ProcessHelper.StartProcess($"gh api repos/{Config.GithubInfo!.Path}", redirectOutput: true);

        using var githubApi = JsonDocument.Parse(githubApiJson);

        githubApi.RootElement.TryGetProperty("permissions", out var githubRepositoryPermissions);
        if (!githubRepositoryPermissions.GetProperty("admin").GetBoolean())
        {
            AnsiConsole.MarkupLine("[red]ERROR: You do not have admin permissions on the repository. Please ensure you have the required permissions and try again. Run 'gh auth logout' to log in with a different account.[/]");
            Environment.Exit(0);
        }
    }

    private void EnsureGithubWorkflowsAreEnabled()
    {
        while (true)
        {
            var listWorkflowsCommand = $"gh workflow list --json name,state,id --repo={Config.GithubInfo!.Path}";
            var result = ProcessHelper.StartProcess(listWorkflowsCommand, Configuration.CliFolder, true).Trim();

            if (result.StartsWith('[') && result.EndsWith(']'))
            {
                break;
            }

            if (AnsiConsole.Confirm("[yellow]GitHub Actions are currently disabled for this repository. Press Enter to open your browser and enable GitHub Actions.[/]"))
            {
                ProcessHelper.OpenBrowser($"https://github.com/{Config.GithubInfo!.Path}/actions");
                AnsiConsole.MarkupLine("[blue]Please enable the workflows and press any key to continue...[/]");
                Console.ReadKey();
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }

    private static void PublishExistingGithubVariables()
    {
        var githubVariablesJson = ProcessHelper.StartProcess(
            $"gh api repos/{Config.GithubInfo!.Path}/actions/variables --paginate",
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
        if (!Config.GithubVariables.Any(variable => Enum.GetNames<VariableNames>().Contains(variable.Key)))
        {
            return;
        }

        AnsiConsole.MarkupLine("[yellow]This Github Repository has already been initialized. If you continue existing GitHub variables will be overridden.[/]");
        if (AnsiConsole.Confirm("Do you want to continue and override existing GitHub variables?"))
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

        if (Config.StagingSubscription.TenantId != Config.ProductionSubscription.TenantId)
        {
            AnsiConsole.MarkupLine("[red]ERROR:[/] Please select two subscriptions from the same tenant, and try again.");
            Environment.Exit(1);
        }

        RunAzureCliCommand($"""account set --subscription "{Config.StagingSubscription.Id}" """);

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

            if (IsContainerRegistryNameConflicting(Config.StagingSubscription.Id, Config.StagingLocation.SharedLocation, $"{uniquePrefix}-stage", $"{uniquePrefix}stage") ||
                IsContainerRegistryNameConflicting(Config.ProductionSubscription.Id, Config.ProductionLocation.SharedLocation, $"{uniquePrefix}-prod", $"{uniquePrefix}prod"))
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

        bool IsContainerRegistryNameConflicting(string subscriptionId, string location, string resourceGroup, string azureContainerRegistryName)
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

    private void ConfirmReuseIfAppRegistrationsExist()
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

    private void ConfirmReuseIfSqlAdminSecurityGroupExists()
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

            if (!AnsiConsole.Confirm("The existing AD Security Group will be reused. Do you want to continue?"))
            {
                AnsiConsole.MarkupLine("[red]Please delete the existing AD Security Group and try again.[/]");
                Environment.Exit(0);
            }

            AnsiConsole.WriteLine();

            return sqlAdminsObjectId;
        }
    }

    private void CollectAdditionalInfo()
    {
        foreach (var instance in _configureContinuousDeploymentsExtensions!)
        {
            var method = instance.GetType().GetMethod("CollectDetails");
            method?.Invoke(instance, [Config]);
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
                * PRODUCTION_SERVICE_PRINCIPAL_OBJECT_ID: [blue]{Config.ProductionSubscription.AppRegistration.ServicePrincipalObjectId}[/]
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

             {ConfirmAdditionalInfo()}
             [bold]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm($"{setupConfirmPrompt}", false)) Environment.Exit(0);

        return;

        string ConfirmAdditionalInfo()
        {
            var stringBuilder = new StringBuilder();
            foreach (var instance in _configureContinuousDeploymentsExtensions!)
            {
                var method = instance.GetType().GetMethod("ConfirmChanges");
                var result = method?.Invoke(instance, [Config]);
                stringBuilder.AppendLine(result?.ToString());
            }

            return stringBuilder.ToString();
        }
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
            $"[green]Successfully created App Registration with Federated Credentials allowing passwordless deployments from {Config.GithubInfo!.Url}.[/]"
        );

        void CreateFederatedCredential(string appRegistrationId, string displayName, string refRefsHeadsMain)
        {
            var parameters = JsonSerializer.Serialize(new
                {
                name = displayName,
                issuer = "https://token.actions.githubusercontent.com",
                subject = $"""repo:{Config.GithubInfo!.Path}:{refRefsHeadsMain}""",
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
            },
                parameters,
                exitOnError: false
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
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{Config.GithubInfo!.Path}/environments/staging""",
            redirectOutput: true
        );

        ProcessHelper.StartProcess(
            $"""gh api --method PUT -H "Accept: application/vnd.github+json" repos/{Config.GithubInfo!.Path}/environments/production""",
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
        SetGithubVariable(VariableNames.PRODUCTION_SERVICE_PRINCIPAL_OBJECT_ID, Config.ProductionSubscription.AppRegistration.ServicePrincipalObjectId!);
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
            ProcessHelper.StartProcess($"gh variable set {Enum.GetName(name)} -b\"{value}\" --repo={Config.GithubInfo!.Path}");
        }
    }

    private void DisableReusableWorkflows()
    {
        // Disable reusable workflows
        DisableActiveWorkflow("Deploy Container");
        DisableActiveWorkflow("Deploy Infrastructure");
        DisableActiveWorkflow("Migrate Database");
        return;

        void DisableActiveWorkflow(string workflowName)
        {
            // Command to list workflows
            var listWorkflowsCommand = $"gh workflow list --json name,state,id --repo={Config.GithubInfo!.Path}";
            var workflowsJson = ProcessHelper.StartProcess(listWorkflowsCommand, Configuration.CliFolder, true);

            // Parse JSON to find the specific workflow and check if it's active
            using var jsonDocument = JsonDocument.Parse(workflowsJson);
            foreach (var element in jsonDocument.RootElement.EnumerateArray())
            {
                var name = element.GetProperty("name").GetString()!;
                var state = element.GetProperty("state").GetString()!;

                if (name != workflowName || state != "active") continue;

                // Disable the workflow if it is active
                var workflowId = element.GetProperty("id").GetInt64();
                var disableCommand = $"gh workflow disable {workflowId} --repo={Config.GithubInfo!.Path}";
                ProcessHelper.StartProcess(disableCommand, Configuration.CliFolder, true);

                AnsiConsole.MarkupLine($"[green]Reusable Git Workflow '{workflowName}' has been disabled.[/]");

                break;
            }
        }
    }

    // ReSharper disable once MemberCanBeMadeStatic.Local
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

                var runWorkflowCommand = $"gh workflow run {workflowFileName} --ref main --repo={Config.GithubInfo!.Path}";
                ProcessHelper.StartProcess(runWorkflowCommand, Configuration.CliFolder, true);

                // Wait briefly to ensure the run has started
                Thread.Sleep(TimeSpan.FromSeconds(15));

                // Fetch and filter the workflows to find a "running" one
                var listWorkflowRunsCommand =
                    $"gh run list --workflow={workflowFileName} --json databaseId,status --repo={Config.GithubInfo!.Path}";
                var workflowsJson = ProcessHelper.StartProcess(listWorkflowRunsCommand, Configuration.CliFolder, true);

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

                var watchWorkflowRunCommand = $"gh run watch {workflowId.Value} --repo={Config.GithubInfo!.Path}";
                ProcessHelper.StartProcessWithSystemShell(watchWorkflowRunCommand, Configuration.CliFolder);

                // Run the command one more time to get the result
                var runResult = ProcessHelper.StartProcess(watchWorkflowRunCommand, Configuration.CliFolder, true);
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

    private void ApplyAdditionalConfigurations()
    {
        foreach (var instance in _configureContinuousDeploymentsExtensions!)
        {
            var method = instance.GetType().GetMethod("ApplyConfigure");
            method?.Invoke(instance, [Config]);
        }
    }

    private void ShowSuccessMessage()
    {
        var setupIntroPrompt =
            $"""
             So far so good. The configuration of GitHub and Azure is now complete. Here are some recommendations to further secure and optimize your setup:

             - For protecting the [blue]main[/] branch, configure branch protection rules to necessitate pull request reviews before merging can occur. Visit [blue]{Config.GithubInfo!.Url}/settings/branches[/], click ""Add Branch protection rule"", and set it up for the [bold]main[/] branch. Requires a paid GitHub plan for private repositories.

             - To add a step for manual approval during infrastructure deployment to the staging and production environments, set up required reviewers on GitHub environments. Visit [blue]{Config.GithubInfo!.Url}/settings/environments[/] and enable [blue]Required reviewers[/] for the [bold]staging[/] and [bold]production[/] environments. Requires a paid GitHub plan for private repositories.

             - Configure the Domain Name for the staging and production environments. This involves two steps:

                 a. Go to [blue]{Config.GithubInfo!.Url}/settings/variables/actions[/] to set the [blue]DOMAIN_NAME_STAGING[/] and [blue]DOMAIN_NAME_PRODUCTION[/] variables. E.g. [blue]staging.your-saas-company.com[/] and [blue]your-saas-company.com[/].

                 b. Run the [blue]Cloud Infrastructure - Deployment[/] workflow again. Note that it might fail with an error message to set up a DNS TXT and CNAME record. Once done, re-run the failed jobs.

             - Set up SonarCloud for code quality and security analysis. This service is free for public repositories. Visit [blue]https://sonarcloud.io[/] to connect your GitHub account. Add the [blue]SONAR_TOKEN[/] secret, and the [blue]SONAR_ORGANIZATION[/] and [blue]SONAR_PROJECT_KEY[/] variables to the GitHub repository. The workflows are already configured for SonarCloud analysis.

             - Enable Microsoft Defender for Cloud (also known as Azure Security Center) once the system evolves for added security recommendations.

             {ShowAdditionalInfoSuccessMessage()}
             """;

        AnsiConsole.MarkupLine($"{setupIntroPrompt}");
        AnsiConsole.WriteLine();

        string ShowAdditionalInfoSuccessMessage()
        {
            var stringBuilder = new StringBuilder();
            foreach (var instance in _configureContinuousDeploymentsExtensions!)
            {
                var method = instance.GetType().GetMethod("ShowSuccessMessage");
                var result = method?.Invoke(instance, [Config]);
                stringBuilder.AppendLine(result?.ToString());
            }

            return stringBuilder.ToString();
        }
    }

    private string RunAzureCliCommand(string arguments, bool redirectOutput = true)
    {
        var azureCliCommand = Configuration.IsWindows ? "cmd.exe /C az" : "az";

        return ProcessHelper.StartProcess($"{azureCliCommand} {arguments}", redirectOutput: redirectOutput, exitOnError: false);
    }

    private static Dictionary<string, string> GetAzureLocations()
    {
        // List of global available regions extracted by running:
        //  "az account list-locations --query "[?metadata.regionType == 'Physical'].{DisplayName:displayName}" --output table
        // Location Acronyms are taken from here https://learn.microsoft.com/en-us/azure/backup/scripts/geo-code-list
        return new Dictionary<string, string>
        {
            { "Australia Central", "au" },
            { "Australia Central 2", "au" },
            { "Australia East", "au" },
            { "Australia Southeast", "au" },
            { "Brazil South", "br" },
            { "Brazil Southeast", "br" },
            { "Canada Central", "ca" },
            { "Canada East", "ca" },
            { "Central India", "in" },
            { "Central US", "us" },
            { "East Asia", "as" },
            { "East US", "us" },
            { "East US 2", "us" },
            { "France Central", "eu" },
            { "France South", "eu" },
            { "Germany North", "eu" },
            { "Germany West Central", "eu" },
            { "Japan East", "jp" },
            { "Japan West", "jp" },
            { "Jio India Central", "in" },
            { "Jio India West", "in" },
            { "Korea Central", "kr" },
            { "Korea South", "kr" },
            { "North Central US", "us" },
            { "North Europe", "eu" },
            { "Norway East", "no" },
            { "Norway West", "no" },
            { "South Africa North", "za" },
            { "South Africa West", "za" },
            { "South Central US", "us" },
            { "South India", "in" },
            { "Southeast Asia", "as" },
            { "Sweden Central", "eu" },
            { "Switzerland North", "ch" },
            { "Switzerland West", "ch" },
            { "UAE Central", "ae" },
            { "UAE North", "ae" },
            { "UK South", "uk" },
            { "UK West", "uk" },
            { "West Central US", "us" },
            { "West Europe", "eu" },
            { "West India", "in" },
            { "West US", "us" },
            { "West US 2", "us" },
            { "West US 3", "us" }
        };
    }
}

public class Config
{
    public string TenantId => StagingSubscription.TenantId;

    public string UniquePrefix { get; set; } = null!;

    public GithubInfo? GithubInfo { get; private set; }

    public Subscription StagingSubscription { get; set; } = null!;

    public Location StagingLocation { get; set; } = null!;

    public Subscription ProductionSubscription { get; set; } = null!;

    public Location ProductionLocation { get; set; } = null!;

    public Dictionary<string, string> GithubVariables { get; set; } = new();

    public void SetGithubInfo()
    {
        var githubUri = GithubHelper.GetGithubUri();
        GithubInfo = GithubHelper.GetGithubInfo(githubUri);
    }

    public bool IsLoggedIn()
    {
        var githubAuthStatus = ProcessHelper.StartProcess("gh auth status", redirectOutput: true);

        return githubAuthStatus.Contains("Logged in to github.com");
    }
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
    public string Name => $"GitHub - {environmentName} - {githubInfo.OrganizationName}/{githubInfo.RepositoryName}";

    public bool Exists => !string.IsNullOrEmpty(AppRegistrationId);

    public string? AppRegistrationId { get; set; }

    public string? ServicePrincipalId { get; set; }

    public string? ServicePrincipalObjectId { get; set; }
}

public class SqlAdminsGroup(GithubInfo githubInfo, string environmentName)
{
    public string Name => $"SQL Admins - {environmentName} - {githubInfo.OrganizationName}/{githubInfo.RepositoryName}";

    public string NickName => $"SQLServerAdmins{environmentName}{githubInfo.OrganizationName}{githubInfo.RepositoryName}";

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
    PRODUCTION_SERVICE_PRINCIPAL_OBJECT_ID,
    PRODUCTION_SHARED_LOCATION,
    PRODUCTION_SQL_ADMIN_OBJECT_ID,
    PRODUCTION_DOMAIN_NAME,

    PRODUCTION_CLUSTER1_ENABLED,
    PRODUCTION_CLUSTER1_LOCATION,

    PRODUCTION_CLUSTER1_LOCATION_ACRONYM
    // ReSharper restore InconsistentNaming
}

public abstract class ConfigureContinuousDeployments
{
    public virtual void CollectDetails(Config? config)
    {
    }

    public virtual string ConfirmChanges(Config? config)
    {
        return string.Empty;
    }

    public virtual void ApplyConfigure(Config? config)
    {
    }

    public virtual string ShowSuccessMessage(Config? config = null)
    {
        return string.Empty;
    }
}
