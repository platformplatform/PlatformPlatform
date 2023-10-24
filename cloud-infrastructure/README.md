## Cloud Infrastructure

This folder contains Bash and [Bicep](https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/overview) scripts used by [GitHub Actions](https://github.com/features/actions) to deploy resources to Azure.

Bicep is an Infrastructure-as-Code (IaC) language specific to Azure. While Bicep is less mature than Terraform, it is a great choice for Azure-only projects. Unlike Terraform which often is many months behind, Bicep is always up to date with the latest Azure features, and as it matures, it will become the better choice for Azure infrastructure. The tooling is already much better than Terraform, and since Bicep is a typed language, you get intellisense and the compiler will catch many errors while writing the code.

## Folder structure

- `cluster`: Scripts to deploy a cluster into clearly named resource groups like `staging-west-europe`, `production-west-europe`, and `production-east-us`. A cluster has its own Azure Container Apps environment (managed Kubernetes), SQL Server, Azure Service Bus, Azure Blob Storage, etc. Tenants (a.k.a. a customer) are created in a dedicated cluster that contains all data belonging to that tenant. This ensures compliance with data protection laws like GDPR, CCPA, PIPEDA, APPs, etc., through geo-isolation. See the [`cluster/main-cluster.bicep`](/cloud-infrastructure/cluster/main-cluster.bicep).
- `environment`: Each environment (like `Staging` and `Production`) has resources that are shared between clusters, e.g. Azure Log Analytics workspace and Application Insights. This allows for central tracking and monitoring across clusters. No Personally Identifiable Information (PII) is tracked, which ensures complying with data protection laws. See the [`environment/main-environment.bicep`](/cloud-infrastructure/environment/main-environment.bicep).
- `shared`: These scripts deploy resources shared between environments such as Azure Container Registry (ACR) within the `Shared` resource group. See the [`shared/main-shared.bicep`](/cloud-infrastructure/shared/main-shared.bicep).
- `modules`: Each Azure Resource is created by a separate Bicep module file, ensuring a modular, reusable, and manageable infrastructure.

These are the resource groups created when deploying one staging cluster, and two production clusters:

![PlatformPlatform Resource Groups](https://camo.githubusercontent.com/cfe23aa287e301b2cc4d510a510a8ba6f718de1b295ab6e4c2ecc9ea99ac7978/68747470733a2f2f6d656469612e636c65616e73686f742e636c6f75642f6d656469612f34363533392f7137446d537378583330544e614e61636c6d4148456d4769706b6c644d6f7235583139356e5161322e6a7065673f457870697265733d31363935343932393131265369676e61747572653d473461595775634853706655524169776e66456778646874414a7e38413442713656417a6639427469526746496b5238646a754647577a754f4d70714244477675773435527e75452d7a312d786b30613375614271383835475649756a5064622d397e48636c3046367577734f3169644f486c77586431726f47672d6e616e4a6b4461694d43593056763934797675486a5362774a696a4954754161736e77627867444370376372793258584f626f525978706a4178763346616872576c2d306f63555554747a77706f764b454472564843547638567968746b354f44664344666437454530713943434f6e4678575242396e39736e565676503451495975634c4e4d6d6a774d2d6974495277482d336c614b7e355674347132496e6c75427a74745842666c70613570676f755839543776574a5a4c4b7a7a4c466550367a4e68477171456279705547794f6e7033426b46703250415f5f264b65792d506169722d49643d4b3236394a4d4154395a4634475a)

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

*What the script does*:

- Logs into your Azure account using a browser pop-up
- Prompts you for your Azure subscription ID
- Ensures the `Microsoft.ContainerService` service provider is registered on the Azure Subscription
- Prompts you for the GitHub repository URL that hosts the GitHub action used for deployment
- Creates a Service Principal called `GitHub Azure Infrastructure - [GitHubOrg] - [GitHubRepo]` using [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials)
- Grants subscription-level `Contributor` and `User Access Administrator` role to the Infrastructure Service Principal
- Creates an `Azure SQL Server Admins` Azure AD security group containing the Infrastructure service principal, allowing GitHub actions to grant Container Apps permissions to SQL databases
- Creates a Service Principal called `GitHub Azure Container Registry - [GitHubOrg] - [GitHubRepo]` using [federated credentials](https://learn.microsoft.com/en-us/azure/developer/github/connect-from-azure?tabs=azure-portal%2Clinux#add-federated-credentials)
- Grants subscription-level 'AcrPush' role to the Container Registry Service Principal
- Creates GitHub repository secrets and variables - While not truly secrets the TenantId, SubscriptionId, and Service Principal Ids are stored as GitHub repository secrets as it's best practice to not expose them

To run the script, issue this command (use the terminal on macOS and WSL on Windows):

```bash
bash ./cloud-infrastructure/initialize-azure.sh
```

*Manual steps required*:

Setting up a forked version of PlatformPlatform also requires configuring SonarCloud static code analysis. To set this up follow these steps:

- Sign up for a SonarCloud account here: https://sonarcloud.io using your GitHub account for authentication
- Create a new Project here: https://sonarcloud.io/projects/create and select your fork of PlatformPlatform
- Set up the following GitHub repository variables here:
  - `SONAR_ORGANIZATION`. E.g., `platformplatform`
  - `SONAR_PROJECT_KEY`. E.g., `PlatformPlatform_platformplatform`
- Set up the following GitHub repository secret here:
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

    - **Windows**:

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

![Running Bicep plan for shared](https://camo.githubusercontent.com/29de58a8823f48e16dcbaea5dd6732c4d7a5d74cecea8e5eeb4555483d8e1d5f/68747470733a2f2f6d656469612e636c65616e73686f742e636c6f75642f6d656469612f34363533392f32644d38784371304c7a476d4d4e506f35554366507879417344725067413677535253364168324c2e6a7065673f457870697265733d31363935353837303531265369676e61747572653d664c36527e4773587234556d38725835496f5669426a4a424c66543375364563546b492d675a7057694f53725a6b745269484e7277434d44564b6953457750635666557750584d70367a4249445a73304242576e425834347e64464231786c4b6f52336557714d507654655647414542397363587e4a647873626d64466e6b41376f4a597a6a44512d434c375137304b79477a584f6b64424e7e3964465370524f7462424d722d626531536c3642556f67387a53566f4c547061786b782d62786f71686a4444366744624c5243696b446b484a736a434c664e48615a4d7253436c4d4c346832476e6e6b573269564855393250516b7579662d3945794e6972637842476156576c5741616e44394b442d7867576950705368315265654c556c6b306c46594146716a7138704c426a6f6661643236705a6f536b7751766c78714477654551566d716f637a30626672694e7737765a65515f5f264b65792d506169722d49643d4b3236394a4d4154395a4634475a)
