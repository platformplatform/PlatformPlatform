# Cloud Infrastructure

## Overview

The Cloud Infrastructure folder contains Infrastructure as Code (IaC) for deploying the application to Azure using Bicep and bash scripts.

## Azure subscription and Environments

The setup operates with different environments like `Testing`, `Staging`, and `Production`, in addition to a `Shared` "environment" for global resources. Everything is deployed into one Azure Subscription with clearly named resource groups.

## Infrastructure structure

The infrastructure scripts are organized on multiple levels:

- `Cluster`: These scripts create separate clusters within an environment. For example, the Production environment might have clusters in different Azure regions like West Europe, East US, etc. When Tenants are created a cluster is selected, where all tenant resources exist. Each cluster will have its own Azure Container Apps environment (managed Kubernetes), Azure Service Bus, Azure Key Vault, SQL Server, etc. Within a cluster, each self-contained system (aka microservice) will have dedicated resources like SQL Database and Blob Storage.
- `Environment`: These scripts are designed to handle different environments (`Testing`, `Staging`, and `Production`). Each environment has resources that are shared between clusters like Azure Log Analytics workspace and an Application Insights resource. Although it's not currently in place, the environment could also have a service, to keep track of which cluster a tenant is located in.
- `Shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` resource group.

All Azure resources are tagged with `environment` (e.g., `Testing`, `Staging`, and `Production`) and a `managed-by` tag (e.g., `Bicep` or `Manual`). This is valuable for cost analytics and understanding how a resource was created.

## Grant GitHub Actions access to Azure without secrets

This guide explains how to set up GitHub Actions to use an Azure Service Principal configured with [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials) to interact with Azure resources without using secrets.

The [`azure-infrastructure.yml`](/.github/workflows/azure-infrastructure.yml/) is triggered on pull requests to show affected changes to Azure resources. When changes are merged into the main branch this pipeline is also used to kick off deployment to Azure.

Please run these commands from a prompt (requires [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)). Replace `<SubscriptionId>`, `<YourGitHubOrg>`, and `<YourGitHubRepo>` with your specific details.

``` bash
# Login to Azure
az login

# Set Azure Subscription
az account set --subscription <SubscriptionId>

# Create Azure AD Application Registration
appId=$(az ad app create --display-name "GitHub Workflows" --query 'appId' -o tsv)

# Create Federated Credentials for Main Branch
mainCredential=$(echo -n "{
  \"name\": \"MainBranch\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:<YourGitHubOrg>/<YourGitHubRepo>:ref:refs/heads/main\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
echo $mainCredential | az ad app federated-credential create --id $appId --parameters @-

# Create Federated Credentials for Pull Requests
pullRequestCredential=$(echo -n "{
  \"name\": \"PullRequests\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:<YourGitHubOrg>/<YourGitHubRepo>:pull_request\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
echo $pullRequestCredential | az ad app federated-credential create --id $appId --parameters @-

# Output App ID
echo "Created federated credentials for App ID: $appId"
```

Create the following GitHub repository secrets: `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`, and `AZURE_CLIENT_ID`. Although these are not truly secrets, it's best practice to keep them private.

Finally, grant the new `GitHub Workflows` service principal owner permissions to the Azure subscription to allow it to create and maintain Azure resources.

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
