# Define some formatting
RED='\033[0;31m'
YELLOW='\033[0;33m'
GREEN='\033[0;32m'
RESET='\033[0m' # Reset formatting
BOLD='\033[1m'
NO_BOLD='\033[22m'
SEPARATOR="${BOLD}---------------------------------------------------------------------------${RESET}"

if [[ -z "$BASH_VERSION" ]]; then
  echo ""
  echo -e "${RED}This script must be run in Bash. Please run ${BOLD}'bash ./initialize-azure.sh'.${RESET}"
  return
fi

echo -e "${SEPARATOR}"
echo -e "${BOLD}Logging in to Azure${RESET}"
echo -e "${SEPARATOR}"

sleep 1
az login || exit 1
tenantId=$(az account show --query 'tenantId' -o tsv) || exit 1

echo -e "${GREEN}Successfully logged in to Azure on Tenant ID: $tenantId.${RESET}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Prompting for Azure Subscription${RESET}"
echo -e "${SEPARATOR}"

echo "Enter your Azure Subscription ID (find it in the list above, look for the 'id' property):"
read subscriptionId
az account set --subscription $subscriptionId || exit 1
az account show --query 'id' -o tsv | grep -q $subscriptionId || exit 1

if [[ "$tenantId" != $(az account show --query 'tenantId' -o tsv) ]]; then
  echo -e "${RED}The Azure Subscription ID: '$subscriptionId' is not on the Tenant with ID '$tenantId'. Did you select the 'id' property?.${RESET}"
  exit 1
fi

echo -e "${GREEN}Successfully set subscription to $subscriptionId on Tenant ID: $tenantId.${RESET}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Prompting GitHub Repository${RESET}"
echo -e "${SEPARATOR}"

echo "Enter your GitHub repository URL (e.g., https://github.com/<Organization>/<Repository>):"
read gitHubRepositoryUrl

if [[ $gitHubRepositoryUrl =~ ^https://github\.com/([a-zA-Z0-9_-]+)/([a-zA-Z0-9_-]+)$ ]]; then
  if [[ -n $ZSH_VERSION ]]; then
    gitHubOrganization="$match[1]"
    gitHubRepositoryName="$match[2]"
  else
    gitHubOrganization="${BASH_REMATCH[1]}"
    gitHubRepositoryName="${BASH_REMATCH[2]}"
  fi
  gitHubRepositoryPath="$gitHubOrganization/$gitHubRepositoryName"
else
  echo -e "${RED}Invalid GitHub URL. Please use the format: https://github.com/<Organization>/<Repository>.${RESET}"
  exit 1
fi

echo -e "${GREEN}Successfully extracted GitHub Organization and Repository: $gitHubRepositoryPath.${RESET}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Ensure 'Microsoft.ContainerService' service provider is registered on Azure Subscription${RESET}"
echo -e "${SEPARATOR}"

az provider register --namespace Microsoft.ContainerService

echo -e "${GREEN}Successfully registered the 'Microsoft.ContainerService' on Subscription '$subscriptionId'.${RESET}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Configuring Azure AD Service Principal for passwordless deployments using OpenID Connect and federated credentials${RESET}"
echo -e "${SEPARATOR}"

servicePrincipalDisplayName="GitHub Azure - $gitHubOrganization - $gitHubRepositoryName"
servicePrincipalAppId=$(az ad sp list --display-name "$servicePrincipalDisplayName" --query "[].appId" -o tsv) || exit 1
if [[ -n "$servicePrincipalAppId" ]]; then
  echo -e "${YELLOW}The Service Principal (App registration) '$servicePrincipalDisplayName' already exists with App ID: $servicePrincipalAppId.${RESET}"

  echo "Would you like to continue using this Service Principal? (y/n)"
  read reuseServicePrincipal

  if [[ "$reuseServicePrincipal" != "y" ]]; then
    echo -e "${RED}Please delete the existing Service Principal and run this script again.${RESET}"
    exit 1
  fi
else
  servicePrincipalAppId=$(az ad app create --display-name "$servicePrincipalDisplayName" --query 'appId' -o tsv) || exit 1
  az ad sp create --id $servicePrincipalAppId || exit 1
fi

mainCredential=$(echo -n "{
  \"name\": \"MainBranch\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$gitHubRepositoryPath:ref:refs/heads/main\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
pullRequestCredential=$(echo -n "{
  \"name\": \"PullRequests\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$gitHubRepositoryPath:pull_request\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
sharedEnvironmentCredentials=$(echo -n "{
  \"name\": \"SharedEnvironment\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$gitHubRepositoryPath:environment:shared\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
stagingEnvironmentCredentials=$(echo -n "{
  \"name\": \"StagingEnvironment\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$gitHubRepositoryPath:environment:staging\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
productionEnvironmentCredentials=$(echo -n "{
  \"name\": \"ProductionEnvironment\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$gitHubRepositoryPath:environment:production\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")

echo $mainCredential | az ad app federated-credential create --id $servicePrincipalAppId --parameters @-
echo $pullRequestCredential | az ad app federated-credential create --id $servicePrincipalAppId --parameters @-
echo $sharedEnvironmentCredentials | az ad app federated-credential create --id $servicePrincipalAppId --parameters @-
echo $stagingEnvironmentCredentials | az ad app federated-credential create --id $servicePrincipalAppId --parameters @-
echo $productionEnvironmentCredentials | az ad app federated-credential create --id $servicePrincipalAppId --parameters @-
if [[ "$reuseServicePrincipal" == "y" ]]; then
   echo -e "${YELLOW}Please ignore the errors: 'FederatedIdentityCredential with name xxxx already exists'.${RESET}"
fi

echo -e "${GREEN}Successfully configured Service Principal with Federated Credentials.${RESET}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Grant subscription level 'Contributor' and 'User Access Administrator' role to the Infrastructure Service Principal${RESET}"
echo -e "${SEPARATOR}"

az role assignment create --assignee $servicePrincipalAppId --role "Contributor" --scope "/subscriptions/$subscriptionId" || exit 1
az role assignment create --assignee $servicePrincipalAppId --role "User Access Administrator" --scope "/subscriptions/$subscriptionId" || exit 1
az role assignment create --assignee $servicePrincipalAppId --role "AcrPush" --scope "/subscriptions/$subscriptionId" || exit 1

echo -e "${GREEN}Successfully granted the Service Principal '$servicePrincipalDisplayName' 'Contributor' rights to the Azure Subscription $subscriptionId.${RESET}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Configuring Azure AD 'Azure SQL Server Admins' Security Group${RESET}"
echo -e "${SEPARATOR}"

azureSqlServerAdmins="Azure SQL Server Admins"
sqlServerAdminsGroupId=$(az ad group list --filter "displayname eq '$azureSqlServerAdmins'" --query "[].id" -o tsv) || exit 1
if [[ -n "$sqlServerAdminsGroupId" ]]; then
  echo -e "${YELLOW}The Azure AD Group '$azureSqlServerAdmins' already exists with Group ID: $sqlServerAdminsGroupId.${RESET}"

  echo "Would you like to continue using this group? (y/n)"
  read reuseSQLServerAdminsSecurityGroup

  if [[ "$reuseSQLServerAdminsSecurityGroup" != "y" ]]; then
    echo -e "${RED}Please delete the existing group and run this script again.${RESET}"
    exit 1
  fi
else
  sqlServerAdminsGroupId=$(az ad group create --display-name "$azureSqlServerAdmins" --mail-nickname "AzureSQLServerAdmins" --query "id" -o tsv) || exit 1
fi

servicePrincipalObjectId=$(az ad sp list --filter "appId eq '$servicePrincipalAppId'" --query "[].id" -o tsv) || exit 1
az ad group member add --group $sqlServerAdminsGroupId --member-id $servicePrincipalObjectId ||  echo -e "${YELLOW}Please ignore member already exists error."

echo -e "${GREEN}Successfully added '$servicePrincipalDisplayName' to '$azureSqlServerAdmins' Security Group.${RESET}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Configure GitHub secrets and variables${RESET}"
echo -e "${SEPARATOR}"

echo -e "The following GitHub repository ${BOLD}secrets${NO_BOLD} must be created:"
echo -e "- AZURE_TENANT_ID: $tenantId"
echo -e "- AZURE_SUBSCRIPTION_ID: $subscriptionId"
echo -e "- AZURE_SERVICE_PRINCIPAL_ID: $servicePrincipalAppId"
echo -e "- ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID: $sqlServerAdminsGroupId"
echo -e "\n"
echo -e "The following GitHub repository ${BOLD}variables${NO_BOLD} must be created:"
echo -e "- CONTAINER_REGISTRY_NAME: <unique name for your Azure Container Registry (ACR)>"
echo -e "- UNIQUE_CLUSTER_PREFIX: <your unique perfix for azure resources. Max 8 alphanumeric characters."

isGitHubCLIInstalled=$(command -v gh > /dev/null 2>&1 && echo "true" || echo "false")
if [[ "$isGitHubCLIInstalled" == "true" ]]; then
  echo "Would you like to do this using GitHub CLI? (y/n)"
  read userChoiceForSecretCreation

  if [[ "$userChoiceForSecretCreation" == "y" ]]; then
    echo "Enter your Azure Container Registry (ACR) name (leave blank to use exiting value):"
    read acrName

    echo "Enter a unique cluster prefix. Max 8 alphanumeric lowercase characters (e.g. acme, contoso, mstf). - (leave blank to use exiting value):"
    read clusterPrefix

    gh auth login --git-protocol https --web || exit 1
    gh secret set AZURE_TENANT_ID -b"$tenantId" --repo=$gitHubRepositoryPath || exit 1
    gh secret set AZURE_SUBSCRIPTION_ID -b"$subscriptionId" --repo=$gitHubRepositoryPath || exit 1
    gh secret set AZURE_SERVICE_PRINCIPAL_ID -b"$servicePrincipalAppId" --repo=$gitHubRepositoryPath || exit 1
    gh secret set ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID -b"$sqlServerAdminsGroupId" --repo=$gitHubRepositoryPath || exit 1

    if [[ -n "$acrName" ]]; then
      gh variable set CONTAINER_REGISTRY_NAME -b"$acrName" --repo=$gitHubRepositoryPath || exit 1
    fi
    if [[ -n "$clusterPrefix" ]]; then
      gh variable set UNIQUE_CLUSTER_PREFIX -b"$clusterPrefix" --repo=$gitHubRepositoryPath || exit 1
    fi
     
    echo -e "${GREEN}Successfully created secrets in GitHub.${RESET}"
  fi
fi

echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Setup completed${RESET}"
echo -e "${SEPARATOR}"


if [[ "$isGitHubCLIInstalled" != "true" ]] || [[ "$userChoiceForSecretCreation" != "y" ]]; then
  echo -e "\n${YELLOW}Please manually create the GitHub repository secrets and varibles:${RESET}"
  echo -e "- Create secrets here: $gitHubRepositoryUrl/settings/${BOLD}secrets${NO_BOLD}/actions"
  echo -e "- Create varibles here: $gitHubRepositoryUrl/settings/${BOLD}variables${NO_BOLD}/actions"
fi

echo -e "\n${YELLOW}To finalize setting configuration of GitHub, please follow these instructions to setup environments:${RESET}"
echo -e "- Navigate to: $gitHubRepositoryUrl/settings/environments"
echo -e "- Create three environments named: ${BOLD}production${NO_BOLD}, ${BOLD}staging${NO_BOLD}, and ${BOLD}shared${NO_BOLD}."
echo -e "- For the ${BOLD}production${NO_BOLD} and ${BOLD}staging${NO_BOLD} environments, optionally create an environment variable named ${BOLD}DOMAIN_NAME${NO_BOLD} to set up a Custom Domain name and SSL Certificate."
echo -e "- It's also recommended to set up 'Required reviewers' and 'Branch protection rules' for each environment to ensure only code from the main branch are deployed."


echo -e "\n${YELLOW}Please manually set up SonarCloud to enable static code analysis. Alternativly disable the test-with-code-coverage job in the application.yml workflow.${RESET}"
echo -e "- Sign up for a SonarCloud account here: https://sonarcloud.io. Use your GitHub account for authentication."
echo -e "- Set up the following GitHub repository variables here: $gitHubRepositoryUrl/settings/${BOLD}variables${NO_BOLD}/actions:"
echo -e "  - SONAR_ORGANIZATION"
echo -e "  - SONAR_PROJECT_KEY"
echo -e "- Set up the following GitHub repository secret here: $gitHubRepositoryUrl/settings/${BOLD}secrets${NO_BOLD}/actions:"
echo -e "  - SONAR_TOKEN to the token generated in SonarCloud."

echo -e "\n${BOLD}${GREEN}You are now ready to run these GitHub Action workflows from the main branch:${RESET}"
echo -e "${GREEN}- 'Cloud Infrastructure - Deployment': cloud-infrastructure.yml${RESET}"
echo -e "${GREEN}- 'Application - Build and Deploy': application.yml${RESET}"

echo -e "\n${YELLOW}${BOLD}TIP:${NO_BOLD} First run the 'Shared' step of the Infrastructure to deploy the Azure Container Registry (ACR).${RESET}"
echo -e "${YELLOW}Then run the 'Build and Test' to push an image to it, before deploying the rest of the infrastructure.${RESET}"

exit 0
