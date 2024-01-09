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
        azureInfo.ContainerRegistry = GetAzureContainerRegistryName(azureInfo.Subscription);

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
            WorkingDirectory = Environment.SolutionFolder,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        var gitRemoteRegex = new Regex(@"(?<url>https://github\.com/.*\.git)");
        var gitRemoteMatches = gitRemoteRegex.Match(gitRemotes);
        if (!gitRemoteMatches.Success)
        {
            AnsiConsole.MarkupLine("[red]ERROR: No GitHub remote found. This tool only works with GitHub remotes.[/]");
            System.Environment.Exit(0);
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

            You need owner permissions on the Azure subscription and GitHub repository. Plus you need permissions to create Directory Groups and App Registrations (aka Service Principals) in Microsoft Entra ID.

            [bold]Would you like to continue?[/]
            """;

        if (!AnsiConsole.Confirm(setupIntroPrompt, false)) System.Environment.Exit(0);

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
        });

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
        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"account set --subscription {subscription.Id}"
        });

        AnsiConsole.MarkupLine($"{title}: {subscription.Name}\n");
        return subscription;
    }

    private void PublishExistingAppRegistration(AzureInfo azureInfo)
    {
        var existingAppRegistrationId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad app list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }).Trim();

        var existingServicePrincipalId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad sp list --display-name "{azureInfo.AppRegistrationName}" --query "[].appId" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }).Trim();

        if (existingAppRegistrationId != string.Empty && existingServicePrincipalId != string.Empty)
        {
            AnsiConsole.MarkupLine(
                $"[yellow]The App Registration '{azureInfo.AppRegistrationName}' already exists with App ID: {existingAppRegistrationId}[/]");

            if (AnsiConsole.Confirm("The existing App Registration will be reused. Do you want to continue?"))
            {
                AnsiConsole.WriteLine();
                azureInfo.AppRegistrationExists = true;
                azureInfo.AppRegistrationId = existingAppRegistrationId;
                azureInfo.ServicePrincipalId = existingServicePrincipalId;
                return;
            }

            AnsiConsole.MarkupLine("[red]Please delete the existing App Registration and try again.[/]");
            System.Environment.Exit(1);
        }

        if (!string.IsNullOrEmpty(existingAppRegistrationId) || !string.IsNullOrEmpty(existingServicePrincipalId))
        {
            AnsiConsole.MarkupLine(
                $"[red]The App Registration or Service Principal '{azureInfo.AppRegistrationName}' exists but not both. Please manually delete and retry.[/]");
            System.Environment.Exit(1);
        }
    }

    private void PublishExistingSqlAdminSecurityGroup(AzureInfo azureInfo)
    {
        var existingSqlAdminSecurityGroupId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments =
                $"""ad group list --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --query "[].id" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }).Trim();

        if (existingSqlAdminSecurityGroupId == string.Empty) return;

        AnsiConsole.MarkupLine(
            $"[yellow]The AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' already exists with ID: {existingSqlAdminSecurityGroupId}[/]");

        if (AnsiConsole.Confirm("The existing AD Security Group will be reused. Do you want to continue?"))
        {
            AnsiConsole.WriteLine();
            azureInfo.SqlAdminsSecurityGroupExists = true;
            azureInfo.SqlAdminsSecurityGroupId = existingSqlAdminSecurityGroupId;
            return;
        }

        AnsiConsole.MarkupLine("[red]Please delete the existing AD Security Group and try again.[/]");
        System.Environment.Exit(1);
    }

    private ContainerRegistry GetAzureContainerRegistryName(Subscription azureSubscription)
    {
        var existingContainerRegistryName = System.Environment.GetEnvironmentVariable("CONTAINER_REGISTRY_NAME") ?? "";

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
            });

            if (JsonDocument.Parse(checkAvailability).RootElement.GetProperty("nameAvailable").GetBoolean())
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
            });

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
        ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth login --git-protocol https --web"
            }
        );

        var output = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "gh",
            Arguments = "auth status",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        });

        if (!output.Contains("Logged in to github.com")) System.Environment.Exit(0);
        AnsiConsole.WriteLine();
    }

    private void ConfirmChangesPrompt(GithubInfo githubInfo, AzureInfo azureInfo)
    {
        var reuseContainerRegistry = azureInfo.ContainerRegistry.Exists ? " - [yellow]reuse existing[/]" : "";
        var reuseAppRegistration = azureInfo.AppRegistrationExists ? " - [yellow]reuse existing[/]" : "";
        var reuseSqlAdminsSecurityGroup = azureInfo.SqlAdminsSecurityGroupExists ? " - [yellow]reuse existing[/]" : "";

        var setupConfirmPrompt =
            $"""
             * GitHub Organization: [blue]{githubInfo.OrganizationName}[/]
             * GitHub Repository name: [blue]{githubInfo.RepositoryName}[/]
             * GitHub Repository URL: [blue]{githubInfo.GithubUrl}[/]
             * Azure Subscription: [blue]{azureInfo.Subscription.Name} ({azureInfo.Subscription.Id})[/]
             * Microsoft Entra ID Tenant ID: [blue]{azureInfo.Subscription.TenantId}[/]
             * Azure Container Registry name: [blue]{azureInfo.ContainerRegistry.Name}[/]{reuseContainerRegistry}
             * App Registration name: [blue]{azureInfo.AppRegistrationName}[/]{reuseAppRegistration}
             * SQL Admins AD Security Group name: [blue]Azure SQL Server Admins[/]{reuseSqlAdminsSecurityGroup}

             [bold]If you continue the following changes will be made:[/]
             1. Ensure deployment of Azure Container Apps Environment is enabled on Azure Subscription
             2. Create an App Registration (aka Service Principal) with Federated Credentials and trust deployments from Github
             3. Grant the App Registration 'Contributor' and 'User Access Administrator' to the Azure Subscription
             4. Create a 'Azure SQL Server Admins' AD Security Group and make the App Registration owner
             5. Configure GitHub Repository with info about Azure Tenant, Subscription, App Registration, Container Registry, etc.

             After this setup you can run GitHub workflows to deploy infrastructure and Docker containers to Azure.

             [bold]Would you like to continue?[/]
             """;

        if (!AnsiConsole.Confirm($"{setupConfirmPrompt}", false)) System.Environment.Exit(0);
    }

    private void PrepareSubscriptionForContainerAppsEnvironment(string subscriptionId)
    {
        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"provider register --namespace Microsoft.ContainerService --subscription {subscriptionId}",
            RedirectStandardOutput = !Environment.VerboseLogging,
            RedirectStandardError = !Environment.VerboseLogging
        });

        AnsiConsole.MarkupLine(
            "[green]Successfully ensured deployment of Azure Container Apps Environment is enabled on Azure Subscription.[/]");
    }

    private void CreateAppRegistration(AzureInfo azureInfo)
    {
        azureInfo.AppRegistrationId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"""ad app create --display-name "{azureInfo.AppRegistrationName}" --query appId -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }).Trim();

        azureInfo.ServicePrincipalId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments = $"ad sp create --id {azureInfo.AppRegistrationId} --query appId -o tsv",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }).Trim();

        azureInfo.ServicePrincipalObjectId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments =
                $"""ad sp list --filter "appId eq '{azureInfo.AppRegistrationId}'" --query "[].id" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }).Trim();

        AnsiConsole.MarkupLine(
            $"[green]Successfully create an App Registration {azureInfo.AppRegistrationName} ({azureInfo.AppRegistrationId}).[/]");
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
                subject = $"""repo:{githubInfo.OrganizationName}/{githubInfo.RepositoryName}:{refRefsHeadsMain}""",
                audiences = new[] { "api://AzureADTokenExchange" }
            });

            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "az",
                Arguments =
                    $@"ad app federated-credential create --id {azureInfo.AppRegistrationId} --parameters  @-",
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
            ProcessHelper.StartProcess(new ProcessStartInfo
            {
                FileName = "az",
                Arguments =
                    $"role assignment create --assignee {azureInfo.ServicePrincipalId} --role \"{role}\" --scope /subscriptions/{azureInfo.Subscription.Id}",
                RedirectStandardOutput = !Environment.VerboseLogging,
                RedirectStandardError = !Environment.VerboseLogging
            });
        }
    }

    private void CreateAzureSqlServerSecurityGroup(AzureInfo azureInfo)
    {
        azureInfo.SqlAdminsSecurityGroupId = ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments =
                $"""ad group create --display-name "{azureInfo.SqlAdminsSecurityGroupName}" --mail-nickname "AzureSQLServerAdmins" --query "id" -o tsv""",
            RedirectStandardOutput = true,
            RedirectStandardError = true
        }).Trim();

        ProcessHelper.StartProcess(new ProcessStartInfo
        {
            FileName = "az",
            Arguments =
                $"ad group member add --group {azureInfo.SqlAdminsSecurityGroupId} --member-id {azureInfo.ServicePrincipalObjectId}",
            RedirectStandardOutput = !Environment.VerboseLogging,
            RedirectStandardError = !Environment.VerboseLogging
        });

        AnsiConsole.MarkupLine(
            $"[green]Successfully created AD Security Group '{azureInfo.SqlAdminsSecurityGroupName}' and granted the App Registration {azureInfo.AppRegistrationName} owner.[/]");
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

    public string AppRegistrationName { get; set; } = default!;

    public string AppRegistrationId { get; set; } = default!;

    public object ServicePrincipalId { get; set; } = default!;

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