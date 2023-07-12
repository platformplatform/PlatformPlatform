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

## Infrastructure Deployment

When pull-requests are made the GitHub Actions  [`azure-infrastructure-change-preview.yml`](/.github/workflows/azure-infrastructure-change-preview.yml/) is showing the affected changes to the Azure resources, and when changes are merged into the main branch the [`azure-infrastructure-deployment.yml`](/.github/workflows/azure-infrastructure-deployment.yml/) will kick off the deployment to Azure.

To grant GitHub Actions access to the Azure subscription, secrets need to be configured in GitHub. Please run this command from a prompt (requires [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli))

``` bash
az login
az account set --subscription <SubscriptionId>
az ad sp create-for-rbac --name "GitHub Workflows" --role owner --scopes /subscriptions/<SubscriptionId> --sdk-auth
```

The output will look something like this:

```json
{
  "clientId": "<the app id>",
  "clientSecret": "<the secret>",
  "subscriptionId": "<your subscription id>",
  "tenantId": "<the tenant id>",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

Ignore the warning: `Option '--sdk-auth' has been deprecated and will be removed in a future release.`. As of this writing, the format above is required to create secrets in a format that allows GitHub to connect to Azure.

Create a GitHub repository secret named `AZURE_CREDENTIALS` using the output as the value.

## Bicep and Bicep Modules

The [`main-shared.bicep`](/cloud-infrastructure/shared/main-shared.bicep), [`main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep), and [`main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep) files act as the top-level Bicep file (entry point) for infrastructure deployments. Each Azure resource type (Azure SQL Server, Azure Storage, Azure Key Vault, Azure Service Bus, etc.) has its own reusable Bicep module, ensuring a modular and manageable IaC structure.

## Testing and debugging

The `Testing` scripts can be used for easy testing and debugging of the Bicep and bash scripts from localhost (requires [Azure CLI](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli)).

1. Set the `CLUSTER_UNIQUE_NAME` enviroment varible. This will be used as a prefix to resources that requires global unique name (like SQL Server, Blob storage acconts). IMPORTANT, this must be no longer than 6 characters to avoid running in to naming limitations in Azure:
    - On MacOS: Add the following line to your shell's configuration file (`~/.zshrc`, `~/.bashrc`, or `~/.bash_profile` depending on your terminal): `export CLUSTER_UNIQUE_NAME='cnts'`
    - On Windows: In PowerShell run the following command: `[Environment]::SetEnvironmentVariable("CLUSTER_UNIQUE_NAME", "cnts", "User")`
2. Set the `CONTAINER_REGISTRY_NAME` to a global unique name:
    - On MacOS: Add the following line to your shell's configuration file (`~/.zshrc`, `~/.bashrc`, or `~/.bash_profile` depending on your terminal): `export CONTAINER_REGISTRY_NAME='contosotest'`
    - On Windows: In PowerShell run the following command: `[Environment]::SetEnvironmentVariable("CONTAINER_REGISTRY_NAME", "contosotest", "User")`
3. Restart the terminal to have the changes take effect
4. Login to Azure Run the following scripts from the prompt: `az account set --subscription <SubscriptionId>`
5. Run [`shared-testing.sh`](/cloud-infrastructure/shared/config/shared-testing.sh)
6. Run [`testing.sh`](/cloud-infrastructure/environment/config/testing.sh)
7. Run [`testing-west-europe.sh`](/cloud-infrastructure/cluster/config/testing-west-europe.sh)
