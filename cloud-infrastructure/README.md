## Cloud Infrastructure

This folder contains Bash and [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview) scripts used by [GitHub Actions](https://github.com/features/actions) to deploy resources to Azure.

Bicep is an Infrastructure-as-Code (IaC) language specific to Azure. While Bicep is less mature than Terraform, it is a great choice for Azure-only projects. Unlike Terraform which often is many months behind, Bicep is always up to date with the latest Azure features, and as it matures, it will become the better choice for Azure infrastructure. The tooling is already much better than Terraform, and since Bicep is a typed language, you get intellisense and the compiler will catch many errors while writing the code.

## Getting started

Please follow the simple instructions in [Getting started](/README.md#setting-up-cicd-with-passwordless-deployments-from-github-to-azure-in-minutes) to setup passwordless deployments from GitHub to Azure.

## Folder structure

- `environment`: Each environment (like `Staging` and `Production`) has resources that are shared between clusters, e.g., Azure Log Analytics workspace and Application Insights. This allows for central tracking and monitoring across clusters. No Personally Identifiable Information (PII) is tracked, which ensures compliance with data protection laws. See the [`environment/main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep).
- `cluster`: Scripts to deploy a cluster into clearly named resource groups like `ppdemo-stage-weu`, `ppdemo-prod-weu`, and `ppdemo-prod-eus2`. A cluster has its own Azure Container Apps environment (managed Kubernetes), SQL Server, Azure Blob Storage, etc. Tenants (a.k.a. a customer) are created in a dedicated cluster that contains all data belonging to that tenant. This ensures compliance with data protection laws like GDPR, CCPA, PIPEDA, APPs, etc., through geo-isolation. See the [`cluster/main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep).

- `modules`: Each Azure Resource is created by a separate Bicep module file, ensuring a modular, reusable, and manageable infrastructure.

All Azure resources are tagged with `environment` (e.g., `stage`, `prod`) and `managed-by` (e.g., `bicep`, `manual`) for easier cost tracking and resource management.
## Cloud Infrastructure

This folder contains Bash and [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview) scripts used by [GitHub Actions](https://github.com/features/actions) to deploy resources to Azure.

Bicep is an Infrastructure-as-Code (IaC) language specific to Azure. While Bicep is less mature than Terraform, it is a great choice for Azure-only projects. Unlike Terraform which often is many months behind, Bicep is always up to date with the latest Azure features, and as it matures, it will become the better choice for Azure infrastructure. The tooling is already much better than Terraform, and since Bicep is a typed language, you get intellisense and the compiler will catch many errors while writing the code.

## Getting started

Please follow the simple instructions in [Getting started](/README.md#setting-up-cicd-with-passwordless-deployments-from-github-to-azure-in-minutes) to setup passwordless deployments from GitHub to Azure.

## Folder structure

- `environment`: Each environment (like `Staging` and `Production`) has resources that are shared between clusters, e.g., Azure Log Analytics workspace and Application Insights. This allows for central tracking and monitoring across clusters. No Personally Identifiable Information (PII) is tracked, which ensures compliance with data protection laws. See the [`environment/main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep).
- `cluster`: Scripts to deploy a cluster into clearly named resource groups like `ppdemo-stage-weu`, `ppdemo-prod-weu`, and `ppdemo-prod-eus2`. A cluster has its own Azure Container Apps environment (managed Kubernetes), SQL Server, Azure Blob Storage, etc. Tenants (a.k.a. a customer) are created in a dedicated cluster that contains all data belonging to that tenant. This ensures compliance with data protection laws like GDPR, CCPA, PIPEDA, APPs, etc., through geo-isolation. See the [`cluster/main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep).

- `modules`: Each Azure Resource is created by a separate Bicep module file, ensuring a modular, reusable, and manageable infrastructure.

### Naming Conventions

Azure resources are named using the following convention: `uniquePrefix-environment-locationAcronym-name`.

- `uniquePrefix` (2-6 characters): e.g., `pp`, `ppdemo`
- `environment` (max 5 characters): e.g., `prod`, `dev`, `qa`, `stage`
- `locationAcronym` (max 4 characters): e.g., `weu`, `eus2`
- `name` (for some resources like storage accounts there is a max of 24 characters, so depending on the length of `uniquePrefix` this allows for 9-13 characters)

There are a couple of exceptions:
- Azure Storage Accounts, Azure Container Apps, etc., do not allow `-` in names
- Child resources like Azure Container Apps (ACA) and SQL databases are not prefixed, as ACA has a limit of 32 characters, making names too cryptic

Examples of cluster-specific resources:
- Resource Group: `ppdemo-stage-weu`, `ppdemo-prod-eus2`
- SQL Server: `ppdemo-stage-weu`, `ppdemo-prod-eus2`
- SQL Server database: `account-management`, `back-office`
- Azure Container App Environment: `ppdemo-stage-weu`, `ppdemo-prod-eus2`
- Azure Container Apps: `account-management-api`, `back-office-worker`
- Managed Identity: `ppdemo-stage-weu-account-management`, `ppdemo-prod-eus2-back-office`
- Key Vault: `ppdemo-stage-weu`, `ppdemo-prod-eus2`
- Virtual Network: `ppdemo-stage-weu`, `ppdemo-prod-eus2`
- Communication Service: `ppdemo-stage-weu`, `ppdemo-prod-eus2`
- Storage Accounts: `ppdemostageweuacctmgmt`, `ppdemoprodweudiagnostic`

Examples of environment-specific resources:
- Application Insights: `ppdemo-stage`, `ppdemo-prod`
- Log Analytics workspace: `ppdemo-stage`, `ppdemo-prod`
- Container Registry: `ppdemostage`, `ppdemoprod`

All Azure resources are tagged with `environment` (e.g., `stage`, `prod`) and `managed-by` (e.g., `bicep`, `manual`) for easier cost tracking and resource management.