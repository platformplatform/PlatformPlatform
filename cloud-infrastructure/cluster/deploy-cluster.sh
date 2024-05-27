#!/bin/bash

UNIQUE_PREFIX=$1
ENVIRONMENT=$2
CLUSTER_LOCATION=$3
CLUSTER_LOCATION_ACRONYM=$4
SQL_ADMIN_OBJECT_ID=$5
DOMAIN_NAME=$6

get_active_version()
{
   local image=$(az containerapp revision list --name "$1" --resource-group "$2" --query "[0].properties.template.containers[0].image" --output tsv 2>/dev/null)
   
   if [[ -z "$image" ]] || [[ "$image" = "ghcr.io/platformplatform/quickstart:latest" ]]; then
      echo "initial"
   else
      local version=${image##*:}
      echo $version
   fi
}

function is_domain_configured() {
  # Get details about the container apps
  local app_details=$(az containerapp show --name "$1" --resource-group "$2" 2>&1)
  if [[ "$app_details" == *"ResourceNotFound"* ]] || [[ "$app_details" == *"ResourceGroupNotFound"* ]]; then
    echo "false"
  else
    local result=$(echo "$app_details" | jq -r '.properties.configuration.ingress.customDomains')
    [[ "$result" != "null" ]] && echo "true" || echo "false"
  fi
}

if [[ "$DOMAIN_NAME" == "-" ]]; then
  # "-" is used to indicate that the domain is not configured
  DOMAIN_NAME=""
fi

CONTAINER_REGISTRY_NAME=$UNIQUE_PREFIX$ENVIRONMENT
ENVIRONMENT_RESOURCE_GROUP_NAME="$UNIQUE_PREFIX-$ENVIRONMENT"
RESOURCE_GROUP_NAME="$ENVIRONMENT_RESOURCE_GROUP_NAME-$CLUSTER_LOCATION_ACRONYM"
IS_DOMAIN_CONFIGURED=$(is_domain_configured "app-gateway" "$RESOURCE_GROUP_NAME")

APP_GATEWAY_VERSION=$(get_active_version "app-gateway" $RESOURCE_GROUP_NAME)
ACTIVE_ACCOUNT_MANAGEMENT_VERSION=$(get_active_version "account-management-api" $RESOURCE_GROUP_NAME) # The version from the API is use for both API and Workers
ACTIVE_BACK_OFFICE_VERSION=$(get_active_version "back-office-api" $RESOURCE_GROUP_NAME) # The version from the API is use for both API and Workers

az extension add --name application-insights --allow-preview true
APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show --app $UNIQUE_PREFIX-$ENVIRONMENT --resource-group $UNIQUE_PREFIX-$ENVIRONMENT --query connectionString --output tsv)

CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')
DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $CLUSTER_LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p resourceGroupName=$RESOURCE_GROUP_NAME environmentResourceGroupName=$ENVIRONMENT_RESOURCE_GROUP_NAME environment=$ENVIRONMENT containerRegistryName=$CONTAINER_REGISTRY_NAME domainName=$DOMAIN_NAME isDomainConfigured=$IS_DOMAIN_CONFIGURED sqlAdminObjectId=$SQL_ADMIN_OBJECT_ID appGatewayVersion=$APP_GATEWAY_VERSION accountManagementVersion=$ACTIVE_ACCOUNT_MANAGEMENT_VERSION backOfficeVersion=$ACTIVE_BACK_OFFICE_VERSION applicationInsightsConnectionString=$APPLICATIONINSIGHTS_CONNECTION_STRING"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh

# When initially creating the Azure Container App with SSL and a custom domain, we need to run the deployment three times (see https://github.com/microsoft/azure-container-apps/tree/main/docs/templates/bicep/managedCertificates):
# 1. On the initial run, the deployment will fail, providing instructions on how to manually create DNS TXT and CNAME records. After doing so, the workflow must be run again.
# 2. The second time, the DNS will be configured, and a certificate will be created. However, they will not be bound together, as this is a two-step process and they cannot be created in a single deployment.
# 3. The third deployment will bind the SSL Certificate to the Domain. This step will be triggered automatically.
if [[ "$*" == *"--apply"* ]]
then
  RED='\033[0;31m'
  RESET='\033[0m' # Reset formatting

  cleaned_output=$(echo "$output" | sed '/^WARNING/d')
  # Check for the specific error message indicating that DNS Records are missing
  if [[ $cleaned_output == *"InvalidCustomHostNameValidation"* ]] || [[ $cleaned_output == *"FailedCnameValidation"* ]] || [[ $cleaned_output == *"-certificate' under resource group '$RESOURCE_GROUP_NAME' was not found"* ]]; then
    # Get details about the container apps environment. Although the creation of the container app fails, the verification ID on the container apps environment is consistent across all container apps.
    env_details=$(az containerapp env show --name $RESOURCE_GROUP_NAME --resource-group $RESOURCE_GROUP_NAME)
    
    # Extract the customDomainVerificationId and defaultDomain from the container apps environment
    custom_domain_verification_id=$(echo "$env_details" | jq -r '.properties.customDomainConfiguration.customDomainVerificationId')
    default_domain=$(echo "$env_details" | jq -r '.properties.defaultDomain')

    # Display instructions for setting up DNS entries
    echo -e "${RED}$(date +"%Y-%m-%dT%H:%M:%S") Please add the following DNS entries and then retry:${RESET}"
    echo -e "${RED}- A TXT record with the name 'asuid.$DOMAIN_NAME' and the value '$custom_domain_verification_id'.${RESET}"
    echo -e "${RED}- A CNAME record with the Host name '$DOMAIN_NAME' that points to address 'app-gateway.$default_domain'.${RESET}"
    exit 1
  elif [[ $cleaned_output == *"ERROR:"* ]]; then
    echo -e "${RED}$output${RESET}"
    exit 1
  fi

  # If the domain was not configured during the first run and we didn't receive any warnings about missing DNS entries, we trigger the deployment again to complete the binding of the SSL Certificate to the domain.
  if [[ "$IS_DOMAIN_CONFIGURED" == "false" ]] && [[ "$DOMAIN_NAME" != "" ]]; then
    echo "Running deployment again to finalize setting up SSL certificate for $DOMAIN_NAME"
    IS_DOMAIN_CONFIGURED=$(is_domain_configured "app-gateway" $RESOURCE_GROUP_NAME)
    DEPLOYMENT_PARAMETERS="-l $CLUSTER_LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p resourceGroupName=$RESOURCE_GROUP_NAME environmentResourceGroupName=$ENVIRONMENT_RESOURCE_GROUP_NAME environment=$ENVIRONMENT containerRegistryName=$CONTAINER_REGISTRY_NAME domainName=$DOMAIN_NAME isDomainConfigured=$IS_DOMAIN_CONFIGURED sqlAdminObjectId=$SQL_ADMIN_OBJECT_ID appGatewayVersion=$APP_GATEWAY_VERSION accountManagementVersion=$ACTIVE_ACCOUNT_MANAGEMENT_VERSION backOfficeVersion=$ACTIVE_BACK_OFFICE_VERSION applicationInsightsConnectionString=$APPLICATIONINSIGHTS_CONNECTION_STRING"
    . ../deploy.sh

    cleaned_output=$(echo "$output" | sed '/^WARNING/d')
    if [[ $cleaned_output == "ERROR:"* ]]; then
      echo -e "${RED}$output"
      exit 1
    fi
  fi

  # Extract the ID of the Managed Identities, which can be used to grant access to SQL Database
  ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$(echo "$cleaned_output" | jq -r '.properties.outputs.accountManagementIdentityClientId.value')
  BACK_OFFICE_IDENTITY_CLIENT_ID=$(echo "$cleaned_output" | jq -r '.properties.outputs.backOfficeIdentityClientId.value')
  if [[ -n "$GITHUB_OUTPUT" ]]; then
    echo "ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
    echo "BACK_OFFICE_IDENTITY_CLIENT_ID=$BACK_OFFICE_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
  else
    . ./grant-database-permissions.sh 'account-management' $ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID
    . ./grant-database-permissions.sh 'back-office' $BACK_OFFICE_IDENTITY_CLIENT_ID
  fi
fi
