## Cloud Infrastructure

This folder contains Bash and [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview) scripts used by [GitHub Actions](https://github.com/features/actions) to deploy resources to Azure.

Bicep is an Infrastructure-as-Code (IaC) language specific to Azure. While Bicep is less mature than Terraform, it is a great choice for Azure-only projects. Unlike Terraform which often is many months behind, Bicep is always up to date with the latest Azure features, and as it matures, it will become the better choice for Azure infrastructure. The tooling is already much better than Terraform, and since Bicep is a typed language, you get intellisense and the compiler will catch many errors while writing the code.

## Folder structure

- `cluster`: Scripts to deploy a cluster into clearly named resource groups like `staging-west-europe`, `production-west-europe`, and `production-east-us`. A cluster has its own Azure Container Apps environment (managed Kubernetes), SQL Server, Azure Service Bus, Azure Blob Storage, etc. Tenants (a.k.a. a customer) are created in a dedicated cluster that contains all data belonging to that tenant. This ensures compliance with data protection laws like GDPR, CCPA, PIPEDA, APPs, etc., through geo-isolation. See the [`cluster/main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep).
- `environment`: Each environment (like `Staging` and `Production`) has resources that are shared between clusters, e.g. Azure Log Analytics workspace and Application Insights. This allows for central tracking and monitoring across clusters. No Personally Identifiable Information (PII) is tracked, which ensures complying with data protection laws. See the [`environment/main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep).
- `shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` resource group. See the [`shared/main-shared.bicep`](/cloud-infrastructure/shared/main-shared.bicep).
- `modules`: Each Azure Resource is created by a separate Bicep module file, ensuring a modular, reusable, and manageable infrastructure.

These are the resource groups created when deploying one staging cluster, and two production clusters:

![PlatformPlatform Resource Groups](https://platformplatformgithub.blob.core.windows.net/PlatformPlatformResourceGroups.png)

All Azure resources are tagged with `environment` (e.g., `staging`, `production`) and `managed-by` (e.g., `bicep`, `manual`) for easier cost tracking and resource management.

## Set up automatic deployment of Azure infrastructure and code from GitHub

### Prerequisites

Before setting up PlatformPlatform, ensure you have:

- Forked the PlatformPlatform GitHub repository
- An Azure Subscription (preferably an empty subscription since the GitHub workflows will be granted full contributor rights to the Azure Subscription - sign up for a [free subscription here](https://azure.microsoft.com/en-gb/free))
- Owner rights of the Azure Subscription
- Permissions to Create Service Principals in Azure Active Directory
- [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli) - Mac: `brew install azure-cli` - Windows: `choco install azure-cli`
- Optional: [GitHub CLI](https://cli.github.com/) installed. Mac: `brew install gh`. Windows: `choco install gh`
- Owner permissions in the GitHub repository
- Windows only: [Windows Subsystem for Linux (WSL)](https://learn.microsoft.com/en-us/windows/wsl/install) (not tested)

### Run Bash script to create service principals and grant access to GitHub

The [initialize-azure.sh](/cloud-infrastructure/initialize-azure.sh) script will automatically prepare your Azure Subscription and GitHub repository to allow running the GitHub action workflows.

_What the script does_:

- Logs into your Azure account using a browser pop-up
- Prompts you for your Azure subscription ID
- Ensures the `Microsoft.ContainerService` service provider is registered on the Azure Subscription
- Prompts you for the GitHub repository URL that hosts the GitHub action used for deployment
- Creates a Service Principal called `GitHub Azure - [GitHubOrg] - [GitHubRepo]` using [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials)
- Grants subscription-level `Contributor`, `User Access Administrator`, and `AcrPush` role to the Service Principal
- Creates an `Azure SQL Server Admins` Azure AD security group containing the Infrastructure service principal, allowing GitHub actions to grant Container Apps permissions to SQL databases
- Creates GitHub repository secrets and variables - While not truly secrets the TenantId, SubscriptionId, and Service Principal Id are stored as GitHub repository secrets as it's best practice to not expose them

To run the script, issue this command the follwoing command from a terminal on macOS (or WSL on Windows - not tested):

```bash
bash ./cloud-infrastructure/initialize-azure.sh
```

![Run initialize-azure bash script](https://platformplatformgithub.blob.core.windows.net/AzureGitHubBashScript.gif)

_Manual steps required_:

Setting up a forked version of PlatformPlatform also requires configuring SonarCloud static code analysis. To set this up follow these steps:

- Sign up for a SonarCloud account here: https://sonarcloud.io using your GitHub account for authentication
- Create a new project here: https://sonarcloud.io/projects/create and select your fork of PlatformPlatform
- Set up the following GitHub repository variables here:
  - `SONAR_ORGANIZATION`. E.g., `platformplatform`
  - `SONAR_PROJECT_KEY`. E.g., `PlatformPlatform_platformplatform`
- Set up the following GitHub repository secret:
  - `SONAR_TOKEN`

Alternatively, delete the `test-with-code-coverage` from the [application.yml](/.github/workflows/application.yml) workflow.

## Development and debugging

The development scripts can be used for easy development, testing, and debugging of the Bicep and bash scripts from localhost.

1. **Set Environment Variables**: Configure the following environment variables according to your operating system:

   - **`UNIQUE_CLUSTER_PREFIX`**: This prefix is used for resources requiring a globally unique name like SQL Server and Blob storage accounts. Ensure the prefix is no longer than 6 characters to comply with Azure's naming limitations. Examples include `acme`, `mstf`, and `appl`. You can and should use the same prefix as in your production environment.
   - **`CONTAINER_REGISTRY_NAME`**: Choose a globally unique name for your container registry. Ensure it's different from what you use on production. Examples include `acmedev`, `microsoftdev`, and `appeldev`. The 6-character limit does not apply here.

   - **macOS**:

     ```bash
     echo "export UNIQUE_CLUSTER_PREFIX='acme'" >> ~/.zshrc  # or ~/.bashrc, ~/.bash_profile
     echo "export CONTAINER_REGISTRY_NAME='acmedev'" >> ~/.zshrc  # or ~/.bashrc, ~/.bash_profile
     ```

   - **Windows (not tested)**:

     ```powershell
     [Environment]::SetEnvironmentVariable("UNIQUE_CLUSTER_PREFIX", "acme", "User")
     [Environment]::SetEnvironmentVariable("CONTAINER_REGISTRY_NAME", "acmedev", "User")
     ```

2. **Reload Terminal**: To apply the changes, restart your terminal
3. Log in to Azure using `az login` (this will prompt you to log in)
4. Run the following command from the command prompt: `az account set --subscription <SubscriptionId>`
5. Run [`./shared-development.sh --plan`](/cloud-infrastructure/shared/config/shared-development.sh)
6. Run [`./development.sh --plan`](/cloud-infrastructure/environment/config/development.sh)
7. Run [`./development-west-europe.sh --plan`](/cloud-infrastructure/cluster/config/development-west-europe.sh)

When ready to deploy to Azure, replace the `--plan` flag from the commands above with `--apply`.
