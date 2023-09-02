# Cloud Infrastructure

## Overview

The Cloud Infrastructure folder contains Infrastructure as Code (IaC) for deploying the application to Azure using Bicep and Bash scripts.

## Azure subscription and Environments

The setup operates with different environments like `Testing`, `Staging`, and `Production`, in addition to a `Shared` "environment" for global resources. Everything is deployed into one Azure Subscription with clearly named resource groups.

## Infrastructure structure

The infrastructure scripts are organized on multiple levels:

- `Cluster`: These scripts create separate clusters within an environment. For example, the Production environment might have clusters in different Azure regions like West Europe, East US, etc. When Tenants are created a cluster is selected, where all tenant resources exist. Each cluster will have its own Azure Container Apps environment (managed Kubernetes), Azure Service Bus, Azure Key Vault, SQL Server, etc. Within a cluster, each self-contained system (aka microservice) will have dedicated resources like SQL Database and Blob Storage.
- `Environment`: These scripts are designed to handle different environments (`Testing`, `Staging`, and `Production`). Each environment has resources that are shared between clusters like Azure Log Analytics workspace and an Application Insights resource. Although it's not currently in place, the environment could also have a service, to keep track of which cluster a tenant is located in.
- `Shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` resource group.

All Azure resources are tagged with `environment` (e.g., `Testing`, `Staging`, and `Production`) and a `managed-by` tag (e.g., `Bicep` or `Manual`). This is valuable for cost analytics and understanding how a resource was created.

## Grant GitHub Actions permissions to deploy Azure resources

Run the [`initialize-azure.sh`](/cloud-infrastructure/initialize-azure.sh) Bash script locally (Terminal on Mac, or WSL on Windows) to prepare your Azure Subscription for deployment from GitHub. This script requires the [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).

What the script does:

- Logs in to your Azure account using browser pop-up
- Prompts you for your Azure subscription ID **
- Prompts you for the GitHub repository url that hosts the GitHub action used for deployment ***
- Creates two Service Principals:
  - "GitHub Workflows - Reader": Used by workflows triggered by Pull Requests to detect pending infrastructure changes in Azure using Bicep
  - "GitHub Workflows - Writer": Used by workflow trigged by the main branch to deploy resources to Azure
- Creates a [custom Azure Role](https://learn.microsoft.com/en-us/azure/role-based-access-control/custom-roles-portal) on the subscription with permissions to read the state of the infrastructure (assigned to the "GitHub Workflows - Reader" service principal)

After successfully running the script, you will be prompted to create 4 GitHub repository secrets. Although these are not truly secrets, it's best practice to keep them private.

** Note: If possible, use an empty Azure subscription. The Service Principals (and hence GitHub) will be granted contributor rights to all resources in the subscription.

*** Deployment to GitHub uses [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials), establishing a trust based on the URL of your GitHub repository. This eliminates the need for secrets to deploy infrastructure.

## Bicep and Bicep Modules

The [`main-shared.bicep`](/cloud-infrastructure/shared/main-shared.bicep), [`main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep), and [`main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep) files act as the top-level Bicep file (entry point) for infrastructure deployments. Each Azure resource type (Azure SQL Server, Azure Storage, Azure Key Vault, Azure Service Bus, etc.) has its own reusable Bicep module, ensuring a modular and manageable IaC structure.

## Testing and debugging

The `Testing` scripts can be used for easy testing and debugging of the Bicep and bash scripts from localhost (requires [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)).

1. Set the `UNIQUE_CLUSTER_PREFIX` enviroment varible. This will be used as a prefix to resources that requires global unique name (like SQL Server, Blob storage acconts). IMPORTANT, this must be no longer than 6 characters to avoid running in to naming limitations in Azure:
    - On MacOS: Add the following line to your shell's configuration file (`~/.zshrc`, `~/.bashrc`, or `~/.bash_profile` depending on your terminal): `export UNIQUE_CLUSTER_PREFIX='cnts'`
    - On Windows: In PowerShell run the following command: `[Environment]::SetEnvironmentVariable("UNIQUE_CLUSTER_PREFIX", "cnts", "User")`
2. Set the `CONTAINER_REGISTRY_NAME` to a global unique name:
    - On MacOS: Add the following line to your shell's configuration file (`~/.zshrc`, `~/.bashrc`, or `~/.bash_profile` depending on your terminal): `export CONTAINER_REGISTRY_NAME='contosotest'`
    - On Windows: In PowerShell run the following command: `[Environment]::SetEnvironmentVariable("CONTAINER_REGISTRY_NAME", "contosotest", "User")`
3. Restart the terminal to have the changes take effect
4. Login to Azure Run the following scripts from the prompt: `az account set --subscription <SubscriptionId>`
5. Run [`shared-testing.sh`](/cloud-infrastructure/shared/config/shared-testing.sh)
6. Run [`testing.sh`](/cloud-infrastructure/environment/config/testing.sh)
7. Run [`testing-west-europe.sh`](/cloud-infrastructure/cluster/config/testing-west-europe.sh)
