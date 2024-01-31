using System.CommandLine;
using System.CommandLine.NamingConventionBinder;
using Azure.Core;
using JetBrains.Annotations;
using Spectre.Console;

namespace PlatformPlatform.DeveloperCli.Commands;

[UsedImplicitly]
public class InitializeGitHubAndAzureWorkflow : Command
{
    public InitializeGitHubAndAzureWorkflow() : base(
        "initialize-github-and-azure-workflow",
        "Set up trust between Azure and GitHub for passwordless deployments using Azure Service Principals with OpenID (aka. Federated Credentials)."
    )
    {
        Handler = CommandHandler.Create(Execute);
    }

    private int Execute()
    {
        // Prompting for Azure Subscription

        // Prompting GitHub Repository

        // Ensure 'Microsoft.ContainerService' service provider is registered on Azure Subscription

        // Configuring Azure AD Service Principal for passwordless deployments using OpenID Connect and federated credentials

        //Grant subscription level 'Contributor' and 'User Access Administrator' role to the Infrastructure Service Principal

        // Configuring Azure AD 'Azure SQL Server Admins' Security Group

        // Configure GitHub secrets and variables

        return 0;
    }
}