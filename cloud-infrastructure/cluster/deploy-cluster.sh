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


if [[ "$DOMAIN_NAME" == "-" ]]; then
  # "-" is used to indicate that the domain is not configured
  DOMAIN_NAME=""
fi

export UNIQUE_PREFIX
export ENVIRONMENT
export LOCATION=$CLUSTER_LOCATION
export DOMAIN_NAME
export SQL_ADMIN_OBJECT_ID

export CONTAINER_REGISTRY_NAME=$UNIQUE_PREFIX$ENVIRONMENT
export ENVIRONMENT_RESOURCE_GROUP_NAME="$UNIQUE_PREFIX-$ENVIRONMENT"
export RESOURCE_GROUP_NAME="$ENVIRONMENT_RESOURCE_GROUP_NAME-$CLUSTER_LOCATION_ACRONYM"

export APP_GATEWAY_VERSION=$(get_active_version "app-gateway" $RESOURCE_GROUP_NAME)
export ACCOUNT_MANAGEMENT_VERSION=$(get_active_version "account-management-api" $RESOURCE_GROUP_NAME) # The version from the API is use for both API and Workers
export BACK_OFFICE_VERSION=$(get_active_version "back-office-api" $RESOURCE_GROUP_NAME) # The version from the API is use for both API and Workers

az extension add --name application-insights --allow-preview true
export APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show --app $UNIQUE_PREFIX-$ENVIRONMENT --resource-group $UNIQUE_PREFIX-$ENVIRONMENT --query connectionString --output tsv)

CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')

cd "$(dirname "${BASH_SOURCE[0]}")"

# Build the .bicepparam file to generate parameters.json
bicep build-params ./main-cluster.bicepparam --outfile ./main-cluster.parameters.json

DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $CLUSTER_LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p ./main-cluster.parameters.json"

. ../deploy.sh

# When initially creating the Azure Container App with SSL and a custom domain, the deployment may fail if DNS records are not configured.
# With bindingType: 'Auto' (API version 2025-07-01), certificates are created and bound in a single deployment.
# If the deployment fails, ensure DNS records are properly configured:
# - A TXT record: asuid.<domain> with the customDomainVerificationId value
# - A CNAME record: <domain> pointing to the container app's default domain
if [[ "$*" == *"--apply"* ]]
then
  RED='\033[0;31m'
  RESET='\033[0m' # Reset formatting

  cleaned_output=$(echo "$output" | sed '/^WARNING/d' | sed '/^\/home\/runner\/work\//d')
  # Check for the specific error message indicating that DNS Records are missing
  if [[ $cleaned_output == *"InvalidCustomHostNameValidation"* ]] || [[ $cleaned_output == *"FailedCnameValidation"* ]]; then
    # Get details about the container apps environment to provide DNS configuration instructions
    env_details=$(az containerapp env show --name $RESOURCE_GROUP_NAME --resource-group $RESOURCE_GROUP_NAME)

    # Extract the customDomainVerificationId and defaultDomain from the container apps environment
    custom_domain_verification_id=$(echo "$env_details" | jq -r '.properties.customDomainConfiguration.customDomainVerificationId')
    default_domain=$(echo "$env_details" | jq -r '.properties.defaultDomain')

    # Display instructions for setting up DNS entries
    echo -e "${RED}$(date +"%Y-%m-%dT%H:%M:%S") Please add the following DNS entries and then retry:${RESET}"
    echo -e "${RED}- A TXT record with the name 'asuid.$DOMAIN_NAME' and the value '$custom_domain_verification_id'.${RESET}"
    echo -e "${RED}- A CNAME record with the Host name '$DOMAIN_NAME' that points to address 'app-gateway.$default_domain'.${RESET}"
    exit 1
  elif [[ $output == *"ERROR:"* ]]; then
    echo -e "${RED}$output${RESET}"
    exit 1
  fi

  # Extract the ID of the Managed Identities, which can be used to grant access to SQL Database
  ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$(echo "$cleaned_output" | jq -r '.properties.outputs.accountManagementIdentityClientId.value')
  BACK_OFFICE_IDENTITY_CLIENT_ID=$(echo "$cleaned_output" | jq -r '.properties.outputs.backOfficeIdentityClientId.value')
  if [[ -n "$GITHUB_OUTPUT" ]]; then
    echo "ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
    echo "BACK_OFFICE_IDENTITY_CLIENT_ID=$BACK_OFFICE_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
  else
    . ./grant-database-permissions.sh $UNIQUE_PREFIX $ENVIRONMENT $CLUSTER_LOCATION_ACRONYM 'account-management' $ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID
    . ./grant-database-permissions.sh $UNIQUE_PREFIX $ENVIRONMENT $CLUSTER_LOCATION_ACRONYM 'back-office' $BACK_OFFICE_IDENTITY_CLIENT_ID
  fi
fi
