# Cloud Infrastructure

Refer to the [Set up automatic deployment of Azure infrastructure and code from GitHub](#set-up-automatic-deployment-of-azure-infrastructure-and-code-from-github) section below for instructions on how to set up deployment.

## Enterprise-grade security

The `cloud-infrastructure` folder contains Infrastructure as Code (IaC) for deploying resources to Azure using [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview) and Bash scripts that run in [GitHub Actions](https://github.com/features/actions).

The Azure Infrastructure *does not use any secrets*. All communication to Databases, Blob Storage Accounts, Service Bus, etc. is done using [Managed Identities](https://learn.microsoft.com/en-us/azure/active-directory/managed-identities-azure-resources/overview). This follows Azure's best practices. This also means that there are no secrets to rotate and no secrets that can accidentally be exposed or stolen. In fact, the current version of the infrastructure code will create an Azure subscription with a 100% Security Score, which is anything but trivial to accomplish.

This screenshot shows the Security Recommendations in Azure after PlatformPlatform resources are deployed:
![PlatformPlatform Azure Security Recommendations](https://media.cleanshot.cloud/media/46539/8ZGcZYrr043z1SXFvNqkilTeTLoRw7rfcqwU3Tr9.jpeg?Expires=1694395264&Signature=Osk3jD~58y9lFk2qFMHCWZN9EK7L3Eidd~pmYPjh0qoz~gRC3lm98QQHdk3kjaqfARjbmfPoMHUCyWg84EcKUd34x1RW0COhEF7BjxuhwNd6RhU~DKaeEqxQPExrQvsbvoRZTrE0A6k7pKbyVg3TV8XTRTK~DaM9oUtbeqTZmbpZJi-VFgOdQWrLTW3YU3UnqjBD70V5MCTDJNFmel3sGU-rr1lRa7VsG8KDFsD1viuCQwhv-XFvpIbPkXLn7NLsE83iSTgjv2LBmpguMCvLImyUZIBIazxSLB5B8xLs1oQAtfaIZaH0HuRH4bhKg-PK7BOsvZi40KTV~2q76jPd7w__&Key-Pair-Id=K269JMAT9ZF4GZ)

Likewise, the deployment from GitHub to Azure is using [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials), which establishes a trust between the GitHub repository and Azure subscription based on the URL of your GitHub repository. This eliminates the need for secrets to deploy infrastructure, also minimizing the attack surface.

## Azure subscription and environments

The setup operates with different environments like `Development`, `Staging`, and `Production`. In addition, there is a `Shared` resource group for global resources. Everything is deployed into one Azure Subscription with clearly named resource groups.

## Structure of infrastructure code

The infrastructure scripts are organized on multiple levels:

- `Cluster`: These scripts create separate clusters within an environment. For example, the Production environment might (but it's optional) have clusters in different Azure regions like West Europe, East US, etc. (in resource groups like `staging-west-europe`, `production-west-europe`, and `production-east-us`). Each new customer/tenant is created in a dedicated cluster, where all tenant resources exist. Each cluster will have its own Azure Container Apps environment (managed Kubernetes), Azure Service Bus, Azure Blob Storage, SQL Server, etc. This ensures geo isolation complying with data protection laws like GDPR, CCPA, PIPEDA, APPs, etc. It also supports dedicated scale units. Within a cluster, each self-contained system (aka microservice) will have dedicated resources like a SQL Database and a Blob Storage account.
- `Environment`: These scripts are designed to handle different environments (`Development`, `Staging`, and `Production`). Each environment has resources that are shared between clusters like Azure Log Analytics workspace and an Application Insights resource. Although it's not currently in place, the environment could also have a service, to keep track of which cluster a tenant is located in.
- `Shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` resource group.

All Azure resources are tagged with `environment` (e.g., `development`, `staging`, and `production`) and a `managed-by` tag (e.g., `bicep` or `manual`). This is valuable for cost analytics and understanding how a resource was created.

This screenshot shows how the resource groups look with two production clusters (one in West Europe and one in East US):
![PlatformPlatform Azure Resource Groups](https://media.cleanshot.cloud/media/46539/ekfChxG0r2WxfaahuhfIio0Vz6oOfu5Wz8YFo4Yt.jpeg?Expires=1694399536&Signature=BGJIM-strpdE~lw-0qHSrs2aKPADHq8~cYfAo4sGNHM6NMt7imTk7aO~X-uzc7jAOhN1-30YK05azFfhXqa-mvm7BmiJvxOfb9JzQAJBNskHV-veAwp33UkTWXKsOO02eau1bDDlvsrNDOqvVXuQRa2AVgWUOpSPgvUDzi1jRJZQs9OjwVqekeGkw72Vurn3Qb~iQffgZRpqbjf-kCMz1wP8LJR31PQjywGDwlh9smWM-LZzOAQJA9f~Q8QJ2GCsMU3S9wrDXEu776NII9~cC6Rghy4matfmhTD1IBm~p~QfvWJkvf0s-W4Acu-eIWqdkFy-cy5OAe2ZYzhdJhnW5g__&Key-Pair-Id=K269JMAT9ZF4GZ)

## Set up automatic deployment of Azure infrastructure and code from GitHub

### Prerequisites

Before setting up PlatformPlatform you need the following:

- Clone the GitHub repository.
- An Azure Subscription (preferably an empty subscription since the GitHub workflows will be granted full contributor rights to the Azure Subscription). Sign up for a [free subscription here](https://azure.microsoft.com/en-gb/free).
- Owner rights of the Azure Subscription.
- Permissions to Create Service Principals in Azure Active Directory.
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli). Mac: `brew install azure-cli`. Windows: `choco install azure-cli`.
- Optional: [GitHub CLI](https://cli.github.com/). Mac: `brew install gh`. Windows: `choco install gh`.
- Permission to create secrets in the GitHub repository.
- Windows only: [Windows Subsystem for Linux (WSL)](https://learn.microsoft.com/en-us/windows/wsl/install). Not tested.

### Run Bash script to create service principals and grant access to GitHub

Run the [initialize-azure.sh](/cloud-infrastructure/initialize-azure.sh) locally using this command: `bash ./cloud-infrastructure/initialize-azure.sh` to prepare your Azure Subscription for deployment from GitHub. Mac: Use the terminal. Windows: Use WSL (Not tested).

*What the script does*:

- Logs into your Azure account using a browser pop-up.
- Prompts you for your Azure subscription ID.
- Ensures the `Microsoft.ContainerService` service provider is registered on the Azure Subscription.
- Prompts you for the GitHub repository URL that hosts the GitHub action used for deployment.
- Creates a Service Principal called `GitHub Azure Infrastructure - [GitHubOrg] - [GitHubRepo]` using [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials).
- Grants subscription level 'Contributor' and 'User Access Administrator' role to the Infrastructure Service Principal.
- Creates `Azure SQL Server Admins` Azure AD security group containing the Infrastructure service principal. This allows GitHub actions to grant Container Apps permissions in SQL databases.
- Creates a Service Principal called `GitHub Azure Container Registry - [GitHubOrg] - [GitHubRepo]` using [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials).
- Grants subscription level 'AcrPush' role to the Container Registry Service Principal.
- Creates GitHub repository secrets. Manual instructions are provided if GitHub CLI is not installed.

*Manual steps required*:
Setting up a forked version of PlatformPlatform also requires the configuration of SonarCloud static code analysis. To set this up follow these steps:

- Sign up for a SonarCloud account here: https://sonarcloud.io. Use your GitHub account for authentication.
- Create a new Project here: https://sonarcloud.io/projects/create. Select your fork of PlatformPlatform.
- Set up the following GitHub repository variables here:
  - `SONAR_ORGANIZATION`. E.g., `platformplatform`
  - `SONAR_PROJECT_KEY`. E.g., `PlatformPlatform_platformplatform`
- Set up the following GitHub repository secrets here:
  - `SONAR_TOKEN`

Alternatively, delete the `test-with-code-coverage` from the [platformplatform-build-and-test.yml](/.github/workflows/platformplatform-build-and-test.yml) workflow.

## Bicep and Bicep Modules

The [`main-shared.bicep`](/cloud-infrastructure/shared/main-shared.bicep), [`main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep), and [`main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep) files act as the top-level Bicep file (entry point) for infrastructure deployments. Each Azure resource type (Azure SQL Server, Azure Storage, Azure Container App, Azure Service Bus, etc.) has its own reusable Bicep module, ensuring a modular and manageable IaC structure.

## Development and debugging

The `Development` scripts can be used for easy development, testing, and debugging of the Bicep and bash scripts from localhost (requires [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)).

1. Set the `UNIQUE_CLUSTER_PREFIX` environment variable. This will be used as a prefix to resources that require a globally unique name (like SQL Server, Blob storage accounts). IMPORTANT: this must be no longer than 6 characters to avoid running into naming limitations in Azure.
    - On MacOS: Add the following line to your shell's configuration file (`~/.zshrc`, `~/.bashrc`, or `~/.bash_profile` depending on your terminal): `export UNIQUE_CLUSTER_PREFIX='cnts'`
    - On Windows: In PowerShell run the following command: `[Environment]::SetEnvironmentVariable("UNIQUE_CLUSTER_PREFIX", "cnts", "User")`
2. Set the `CONTAINER_REGISTRY_NAME` to a globally unique name:
    - On MacOS: Add the following line to your shell's configuration file (`~/.zshrc`, `~/.bashrc`, or `~/.bash_profile` depending on your terminal): `export CONTAINER_REGISTRY_NAME='contosotest'`
    - On Windows: In PowerShell run the following command: `[Environment]::SetEnvironmentVariable("CONTAINER_REGISTRY_NAME", "contosotest", "User")`
3. Restart the terminal to have the changes take effect.
4. Login to Azure using `az login` (this will prompt you to login).
5. Run the following scripts from the prompt: `az account set --subscription <SubscriptionId>`
6. Run [`shared-development.sh`](/cloud-infrastructure/shared/config/shared-development.sh)
7. Run [`development.sh`](/cloud-infrastructure/environment/config/development.sh)
8. Run [`development-west-europe.sh`](/cloud-infrastructure/cluster/config/development-west-europe.sh)