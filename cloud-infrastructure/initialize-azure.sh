# Define some formatting
RED='\033[0;31m'
YELLOW='\033[0;33m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color
BOLD='\033[1m'
NO_BOLD='\033[22m'
SEPARATOR="${BOLD}---------------------------------------------------------------------------${NC}"

if [ -z "$BASH_VERSION" ]; then
    echo ""
    echo -e "${RED}This script must be run in Bash. Please run ${BOLD}'bash ./initialize-azure.sh'${NC}"
    return
fi

echo -e "${SEPARATOR}"
echo -e "${BOLD}Logging in to Azure${NC}"
echo -e "${SEPARATOR}"

sleep 1
az login || exit 1
tenantId=$(az account show --query 'tenantId' -o tsv) || exit 1

echo -e "${GREEN}Successfully logged in to Azure on Tenant ID: $tenantId${NC}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Prompting for Azure Subscription${NC}"
echo -e "${SEPARATOR}"

echo "Enter your Azure Subscription ID (find it in the list above, look for the 'id' property): "
read subscriptionId
az account set --subscription $subscriptionId || exit 1
az account show --query 'id' -o tsv | grep -q $subscriptionId || exit 1

if [ "$tenantId" != $(az account show --query 'tenantId' -o tsv) ]; then
    echo -e "${RED}The Azure Subscription ID: '$subscriptionId' is not on the Tenant with ID '$tenantId'. Did you select the 'id' property?${NC}"
    exit 1
fi

echo -e "${GREEN}Successfully set subscription to $subscriptionId on Tenant ID: $tenantId${NC}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Prompting GitHub Repository${NC}"
echo -e "${SEPARATOR}"

echo "Enter your GitHub repository URL (e.g., https://github.com/<Organization>/<Repository>): "
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
    echo -e "${RED}Invalid GitHub URL. Please use the format: https://github.com/<Organization>/<Repository>${NC}"
    exit 1
fi

echo -e "${GREEN}Successfully extracted GitHub Organization and Repository: $gitHubRepositoryPath${NC}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Configuring Azure AD Service Principal for Infrastructure${NC}"
echo -e "${SEPARATOR}"

servicePrincipalAppIdInfrastructure=$(az ad sp list --display-name "GitHub Azure Infrastructure - $gitHubOrganization - $gitHubRepositoryName" --query "[].appId" -o tsv) || exit 1
if [ -n "$servicePrincipalAppIdInfrastructure" ]; then
    echo -e "${YELLOW}The Service Principal (App registration) 'GitHub Azure Infrastructure - $gitHubOrganization - $gitHubRepositoryName' already exists with App ID: $servicePrincipalAppIdInfrastructure${NC}"

    echo "Would you like to continue using this Service Principal? (y/n)"
    read userChoiceForReuseServicePrincipalfrastructure

    if [ "$userChoiceForReuseServicePrincipalfrastructure" != "y" ]; then
        echo -e "${RED}Please delete the existing Service Principal and run this script again${NC}"
        exit 1
    fi
else
    servicePrincipalAppIdInfrastructure=$(az ad app create --display-name "GitHub Azure Infrastructure - $gitHubOrganization - $gitHubRepositoryName" --query 'appId' -o tsv) || exit 1
    az ad sp create --id $servicePrincipalAppIdInfrastructure || exit 1
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
if [ "$userChoiceForReuseServicePrincipalfrastructure" == "y" ]; then
   echo -e "${YELLOW}You are reusing the Service Principal. Please ignore the error: 'FederatedIdentityCredential with name xxxx already exists'${NC}"
fi
echo $mainCredential | az ad app federated-credential create --id $servicePrincipalAppIdInfrastructure --parameters @-
echo $pullRequestCredential | az ad app federated-credential create --id $servicePrincipalAppIdInfrastructure --parameters @-
echo $sharedEnvironmentCredentials | az ad app federated-credential create --id $servicePrincipalAppIdInfrastructure --parameters @-
echo $stagingEnvironmentCredentials | az ad app federated-credential create --id $servicePrincipalAppIdInfrastructure --parameters @-
echo $productionEnvironmentCredentials | az ad app federated-credential create --id $servicePrincipalAppIdInfrastructure --parameters @-

echo -e "${GREEN}Successfully configured Service Principal with Federated Credentials${NC}"

echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Grant subscription level 'Contributor' and 'User Access Administrator' role to the Infrastructure Service Principal${NC}"
echo -e "${SEPARATOR}"

az role assignment create --assignee $servicePrincipalAppIdInfrastructure --role Contributor --scope "/subscriptions/$subscriptionId" || exit 1
az role assignment create --assignee $servicePrincipalAppIdInfrastructure --role "User Access Administrator" --scope "/subscriptions/$subscriptionId" || exit 1

echo -e "${GREEN}Successfully granted the Service Principal $servicePrincipalAppIdInfrastructure 'Contributor' rights to the Azure Subscription $subscriptionId${NC}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Configuring Azure AD Service Principal for Azure Container Registry (ACR)${NC}"
echo -e "${SEPARATOR}"

servicePrincipalAppIdAcr=$(az ad sp list --display-name "GitHub Azure Container Registry - $gitHubOrganization - $gitHubRepositoryName" --query "[].appId" -o tsv) || exit 1
if [ -n "$servicePrincipalAppIdAcr" ]; then
    echo -e "${YELLOW}The Service Principal (App registration) 'GitHub Azure Container Registry - $gitHubOrganization - $gitHubRepositoryName' already exists with App ID: $servicePrincipalAppIdAcr${NC}"

    echo "Would you like to continue using this Service Principal? (y/n)"
    read userChoiceForReuseServicePrincipalAcr

    if [ "$userChoiceForReuseServicePrincipalAcr" != "y" ]; then
        echo -e "${RED}Please delete the existing Service Principal and run this script again${NC}"
        exit 1
    fi
else
    servicePrincipalAppIdAcr=$(az ad app create --display-name "GitHub Azure Container Registry - $gitHubOrganization - $gitHubRepositoryName" --query 'appId' -o tsv) || exit 1
    az ad sp create --id $servicePrincipalAppIdAcr || exit 1
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

if [ "$userChoiceForReuseServicePrincipalAcr" == "y" ]; then
   echo -e "${YELLOW}You are reusing the Service Principal. Please ignore the error: 'FederatedIdentityCredential with name MainBranch/PullRequests already exists'${NC}"
fi
echo $mainCredential | az ad app federated-credential create --id $servicePrincipalAppIdAcr --parameters @-
echo $pullRequestCredential | az ad app federated-credential create --id $servicePrincipalAppIdAcr --parameters @-

echo -e "${GREEN}Successfully configured Service Principal with Federated Credentials${NC}"

echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Assigning subscription level 'AcrPush' rights to the Service Principals${NC}"
echo -e "${SEPARATOR}"

az role assignment create --assignee $servicePrincipalAppIdAcr --role AcrPush --scope "/subscriptions/$subscriptionId" || exit 1

echo -e "${GREEN}Successfully granted the Service Principal $servicePrincipalAppIdAcr 'AcrPush' rights to the Azure Subscription $subscriptionId${NC}"


echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Configure GitHub secrets${NC}"
echo -e "${SEPARATOR}"

echo "The last step is to create these secrets in the GitHub repository here: $gitHubRepositoryUrl/settings/secrets/actions"
echo "- AZURE_TENANT_ID: $tenantId"
echo "- AZURE_SUBSCRIPTION_ID: $subscriptionId"
echo "- AZURE_SERVICE_PRINCIPAL_ID_INFRASTRUCTURE: $servicePrincipalAppIdInfrastructure"
echo "- AZURE_SERVICE_PRINCIPAL_ID_ACR: $servicePrincipalAppIdAcr"

isGitHubCLIInstalled=$(command -v gh > /dev/null 2>&1 && echo "true" || echo "false")
if [ "$isGitHubCLIInstalled" == "true" ]; then
    echo "Would you like to do this using GitHub CLI? (y/n)"
    read userChoiceForSecretCreation

    if [ "$userChoiceForSecretCreation" == "y" ]; then
        gh auth login --git-protocol https --web || exit 1
        gh secret set AZURE_TENANT_ID -b"$tenantId" --repo=$gitHubRepositoryPath || exit 1
        gh secret set AZURE_SUBSCRIPTION_ID -b"$subscriptionId" --repo=$gitHubRepositoryPath || exit 1
        gh secret set AZURE_SERVICE_PRINCIPAL_ID_INFRASTRUCTURE -b"$servicePrincipalAppIdInfrastructure" --repo=$gitHubRepositoryPath || exit 1
        gh secret set AZURE_SERVICE_PRINCIPAL_ID_ACR -b"$servicePrincipalAppIdAcr" --repo=$gitHubRepositoryPath || exit 1
        echo -e "${GREEN}Successfully created secrets in GitHub${NC}"
    fi
fi

echo -e "\n${SEPARATOR}"
echo -e "${BOLD}Setup completed${NC}"
echo -e "${SEPARATOR}"

echo -e "${BOLD}${GREEN}You are now ready to run these GitHub Action workflows:${NC}"
echo -e "${BOLD}${GREEN}- 'Azure Infrastructure - Deployment': azure-infrastructure.yml${NC}"
echo -e "${BOLD}${GREEN}- 'PlatformPlatform - Build and Test': platformplatform-build-and-test.yml${NC}"
echo -e "\n"
echo -e "${BOLD}${GREEN}Please first run the 'Shared' step of the Infrastructure to deploy the Azure Container Registry.${NC}"
echo -e "${BOLD}${GREEN}Then run the 'Build and Test' to push an image to it, before deploying the rest of the infrastructure.${NC}"

exit 0