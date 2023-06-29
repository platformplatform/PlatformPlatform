# Cloud Infrastructure

## Overview

The Cloud Infrastructure directory contains Infrastructure as Code (IaC) scripts for deploying the application to Azure using Bicep and bash scripts using GitHub Actions when changes are merged into the main branch.

## Azure subscriptions and Environments

The setup operates with different environments and Azure Subscriptions. It is recommended to have a separate Azure Subscription for each environment (`Testing`, `Staging`, and `Production`) in addition to one `Shared` subscription. The description below assumes 4 different subscriptions. The `Testing` subscription is not strictly necessary, but it can be used to test the Bicep and bash scripts from localhost for easier debugging.

## Infrastructure structure

The infrastructure scripts are organized on multiple levels:

- `Shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` subscription.
- `Environment`: These scripts are designed to handle different environments (`Testing`, `Staging`, and `Production`). Each environment has shared resources like Azure Log Analytics workspace and an Application Insights resource. Although it's not currently in place, a global environment service could be created, for instance, to keep track of which cluster a tenant is located in.
- `Cluster`: These scripts create separate clusters within an environment. For example, the Production environment might have clusters in West Europe, East US, and South Australia. Each cluster will have an Azure Container Apps environment (managed Kubernetes), Azure Service Bus, Azure Key Vault, SQL Server, etc. Within a cluster, each self-contained system (microservice) will have a dedicated SQL Database and Blob Storage, while other resources like Azure Service Bus and Azure Key Vault are shared between all systems in a cluster.

All Azure resources are tagged with `environment` (e.g., `Testing`, `Staging`, and `Production`) and a `managed-by` tag (e.g., `Bicep` or `Manual`). This is valuable for cost analytics and understanding how a resource was created.

## Infrastructure Deployment

A shared `azure-infrastructure-deployment.yml` (in the `.github/workflows` folder) is used to deploy resources to Azure. To grant GitHub access to Azure, subscription secrets need to be configured in GitHub for each subscription (`Shared`, `Staging`, and `Production`). For each subscription, please run this command:

``` bash
az account set --subscription <SubscriptionId>
az ad sp create-for-rbac --name "GitHub-<SubscriptionName>" --role owner --scopes /subscriptions/<SubscriptionId> --sdk-auth
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

Ignore the warning: `Option '--sdk-auth' has been deprecated and will be removed in a future release.`. As of this writing, the format above is still required to allow GitHub to connect to Azure.

For each Azure subscription, create a GitHub repository secret named `AZURE_CREDENTIALS_SHARED`, `AZURE_CREDENTIALS_STAGING`, and `AZURE_CREDENTIALS_PRODUCTION`, respectively, using the output generated for that subscription.

## Create Active Directory Group to allow Container Apps to pull container images

Follow these steps to create an Active Directory Group that will have pull permission for the Active Directory:

1. Navigate to the Azure portal at [https://portal.azure.com/](https://portal.azure.com/) and log in with a user that has at least "User Access Administrator" rights
2. Navigate to Azure Active Directory
3. Select Groups (under Manage)
4. Select New group
5. Choose `Security` as the Group type
6. Use the name `Azure-Container-Registry-Pull`. This must match the name in the [deploy.sh](/cloud-infrastructure/deploy.sh) script
7. Click "No owners selected". Find and select the newly created service principals `GitHub-Staging` and `GitHub-Production`
8. Click Create to create the group

The Bicep configuration will add the Managed Identity for Container Apps from both Staging and Production to this group, allowing these to pull container images from the Azure Container Registry (ACR).

## Bicep and Bicep Modules

The `main.bicep` file acts as the composition root for all infrastructure deployments. Each resource (Azure Storage, Azure Key Vault, Azure Service Bus, etc.) has its own reusable Bicep module, ensuring a modular and manageable IaC structure.