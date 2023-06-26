# Cloud Infrastructure

## Overview

The Cloud Infrastructure directory contains Infrastructure as Code (IaC) scripts for deploying the application to Azure using Bicep and bash scripts using GitHub Actions when changes are merged into the main branch.

## Azure subscriptions and Environments

The setup operates with different environments and Azure Subscriptions. While all environments can be deployed to a single shared Azure Subscription, it is recommended to have a separate Azure Subscription per environment (`Testing`, `Staging`, and `Production`) plus one `Shared` subscription. In a real-world scenario, this makes managing permissions easier, and eventually, non-production subscriptions can be discounted Azure "Development & Test subscriptions" with lower SLA (requires that an Azure Master Service Agreement (MSA) is in place). The description below assumes 4 different subscriptions. The `Testing` subscription is not strictly needed, but this subscription can be used to test the Bicep and bash scripts from localhost, for easy debugging.

## Infrastructure structure

The infrastructure scripts are organized on multiple levels:

- `Shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` subscription.
- `Environment`: These scripts are designed to handle different environments (`Testing`, `Staging`, and `Production`). Each environment has shared resources like Azure Log Analytics workspace and an Application Insights resource. While currently not in place, a global environment service could be created to e.g., keep track of which cluster a tenant is created in.
- `Cluster`: These scripts create separate clusters within an environment. E.g., the Production environment might have clusters in West Europe, East US, and South Australia. Each cluster will have an Azure Container Apps environment (managed Kubernetes), Azure Service Bus, Azure Key Vault, SQL Server, etc. Within a cluster, each self-contained system (micro-service) will have a dedicated SQL Database and Blob Storage, while other resources like Azure Service Bus and Azure Key Vault are shared between all systems in a cluster.

All Azure resources are tagged with `environment` (e.g., `Testing`, `Staging`, and `Production`) and a `managed-by` tag (e.g., `Bicep` or `Manual`). This is valuable for cost analytics and understanding how a resource was created.

## Infrastructure Deployment

A shared `azure-infrastructure-deployment.yml` (in the `.github/workflows` folder) is used to deploy resources to Azure. To grant GitHub access to Azure subscription secrets need to be configured in GitHub for each Subscription (`Shared`, `Staging`, and `Production`). For each subscription, please run this command:

``` bash
az account set --subscription /{SubscriptionId}
az ad sp create-for-rbac --name "PlatformPlatform - GitHub - [Subscription]" --role owner --scopes /subscriptions/{SubscriptionId} --sdk-auth
```

The output will look something like this:

```json
{
  "clientId": "########-####-####-####-############",
  "clientSecret": "########################################",
  "subscriptionId": "########-####-####-####-############",
  "tenantId": "########-####-####-####-############",
  "activeDirectoryEndpointUrl": "https://login.microsoftonline.com",
  "resourceManagerEndpointUrl": "https://management.azure.com/",
  "activeDirectoryGraphResourceId": "https://graph.windows.net/",
  "sqlManagementEndpointUrl": "https://management.core.windows.net:8443/",
  "galleryEndpointUrl": "https://gallery.azure.com/",
  "managementEndpointUrl": "https://management.core.windows.net/"
}
```

For each Azure subscription, create a GitHub repository secret named `AZURE_CREDENTIALS_{SUBSCRIPTION}` (see names in `azure-infrastructure-deployment.yml`)

## Bicep and Bicep Modules

The `main.bicep` file acts as the composition root for all infrastructure deployments. Each resource (Azure Storage, Azure Key Vault, Azure Service Bus, etc.) has its own reusable Bicep module, ensuring a modular and manageable IaC structure.
