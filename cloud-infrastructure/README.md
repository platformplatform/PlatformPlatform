## Cloud Infrastructure

This folder contains Bash and [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview) scripts used by [GitHub Actions](https://github.com/features/actions) to deploy resources to Azure.

Bicep is an Infrastructure-as-Code (IaC) language specific to Azure. While Bicep is less mature than Terraform, it is a great choice for Azure-only projects. Unlike Terraform which often is many months behind, Bicep is always up to date with the latest Azure features, and as it matures, it will become the better choice for Azure infrastructure. The tooling is already much better than Terraform, and since Bicep is a typed language, you get intellisense and the compiler will catch many errors while writing the code.

## Getting started

Please follow the simple instructions in [Getting started](/README.md#setting-up-cicd-with-passwordless-deployments-from-github-to-azure-in-minutes) to setup passwordless deployments from GitHub to Azure.

## Folder structure

- `cluster`: Scripts to deploy a cluster into clearly named resource groups like `staging-west-europe`, `production-west-europe`, and `production-east-us`. A cluster has its own Azure Container Apps environment (managed Kubernetes), SQL Server, Azure Service Bus, Azure Blob Storage, etc. Tenants (a.k.a. a customer) are created in a dedicated cluster that contains all data belonging to that tenant. This ensures compliance with data protection laws like GDPR, CCPA, PIPEDA, APPs, etc., through geo-isolation. See the [`cluster/main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep).
- `environment`: Each environment (like `Staging` and `Production`) has resources that are shared between clusters, e.g. Azure Log Analytics workspace and Application Insights. This allows for central tracking and monitoring across clusters. No Personally Identifiable Information (PII) is tracked, which ensures complying with data protection laws. See the [`environment/main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep).
- `shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` resource group. See the [`shared/main-shared.bicep`](/cloud-infrastructure/shared/main-shared.bicep).
- `modules`: Each Azure Resource is created by a separate Bicep module file, ensuring a modular, reusable, and manageable infrastructure.

These are the resource groups created when deploying one staging cluster, and two production clusters:

![PlatformPlatform Resource Groups](https://platformplatformgithub.blob.core.windows.net/PlatformPlatformResourceGroups.png)

All Azure resources are tagged with `environment` (e.g., `staging`, `production`) and `managed-by` (e.g., `bicep`, `manual`) for easier cost tracking and resource management.
