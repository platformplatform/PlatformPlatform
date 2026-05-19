#!/bin/bash

UNIQUE_PREFIX=$1
ENVIRONMENT=$2
CLUSTER_LOCATION=$3
CLUSTER_LOCATION_ACRONYM=$4
POSTGRES_ADMIN_OBJECT_ID=$5
DOMAIN_NAME=$6
BACK_OFFICE_DOMAIN_NAME=$7

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

if [[ "$BACK_OFFICE_DOMAIN_NAME" == "-" ]]; then
  # "-" is used to indicate that the back-office domain is not configured
  BACK_OFFICE_DOMAIN_NAME=""
fi

if [[ "$BACK_OFFICE_ADMINS_GROUP_ID" == "-" ]]; then
  # "-" is used to indicate that the back-office admins group is not configured
  BACK_OFFICE_ADMINS_GROUP_ID=""
fi

if [[ -z "$BACK_OFFICE_ENTRA_CLIENT_ID" ]]; then
  echo "ERROR: BACK_OFFICE_ENTRA_CLIENT_ID is required. Run 'dotnet run --project developer-cli -- deploy' to bootstrap." >&2
  exit 1
fi

export UNIQUE_PREFIX
export ENVIRONMENT
export LOCATION=$CLUSTER_LOCATION
export DOMAIN_NAME
export BACK_OFFICE_DOMAIN_NAME
export BACK_OFFICE_ENTRA_CLIENT_ID
export BACK_OFFICE_ADMINS_GROUP_ID
export POSTGRES_ADMIN_OBJECT_ID
export GOOGLE_OAUTH_CLIENT_ID
export GOOGLE_OAUTH_CLIENT_SECRET
export STRIPE_PUBLISHABLE_KEY
export STRIPE_API_KEY
export STRIPE_WEBHOOK_SECRET
# Set to "true" by the deploy workflow's "Detect Email Custom Domain Verification" step once it
# observes the CustomerManaged email domain (eTLD+1 of DOMAIN_NAME) as fully verified. When true,
# Bicep links the domain to the CommunicationServices resource and the SENDER_EMAIL_ADDRESS env var
# on account-api/main-api flips from no-reply@<azurecomm.net> to no-reply@<apex of DOMAIN_NAME>.
# Defaults to "false" so mail keeps flowing on the AzureManaged sender during the verification window
# and so the first apply (which always precedes verification) does not fail trying to link an
# unverified domain. Operators do not flip this manually.
export USE_CUSTOM_EMAIL_DOMAIN="${USE_CUSTOM_EMAIL_DOMAIN:-false}"

export CONTAINER_REGISTRY_NAME=$UNIQUE_PREFIX$ENVIRONMENT
export GLOBAL_RESOURCE_GROUP_NAME="$UNIQUE_PREFIX-$ENVIRONMENT-global"
export CLUSTER_RESOURCE_GROUP_NAME="$UNIQUE_PREFIX-$ENVIRONMENT-$CLUSTER_LOCATION_ACRONYM"

export APP_GATEWAY_VERSION=$(get_active_version "app-gateway" $CLUSTER_RESOURCE_GROUP_NAME)
export ACCOUNT_VERSION=$(get_active_version "account-api" $CLUSTER_RESOURCE_GROUP_NAME) # The version from the API is use for both API and Workers
export MAIN_VERSION=$(get_active_version "main-api" $CLUSTER_RESOURCE_GROUP_NAME) # The version from the API is use for both API and Workers

az extension add --name application-insights --allow-preview true --only-show-errors

# Check if Application Insights exists before trying to get connection string
if az group exists --name $GLOBAL_RESOURCE_GROUP_NAME 2>/dev/null | grep -q "true"; then
  export APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show --app $UNIQUE_PREFIX-$ENVIRONMENT --resource-group $GLOBAL_RESOURCE_GROUP_NAME --query connectionString --output tsv)
else
  export APPLICATIONINSIGHTS_CONNECTION_STRING=""
fi

CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')
export REVISION_SUFFIX=$(printf "%04x" $RANDOM | head -c 4)

cd "$(dirname "${BASH_SOURCE[0]}")"

# Product name is the single source of truth for branding -- read it from platform-settings.jsonc.
# Strip whole-line // comments so jq can parse the JSONC.
export PRODUCT_NAME=$(sed '/^[[:space:]]*\/\//d' ../../application/platform-settings.jsonc | jq -r '.branding.productName')
if [[ -z "$PRODUCT_NAME" || "$PRODUCT_NAME" == "null" ]]; then
  echo "ERROR: Could not read branding.productName from application/platform-settings.jsonc." >&2
  exit 1
fi

# Build the .bicepparam file to generate parameters.json
bicep build-params ./main-cluster.bicepparam --outfile ./main-cluster.parameters.json

DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $CLUSTER_LOCATION -n $CURRENT_DATE-$CLUSTER_RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p ./main-cluster.parameters.json"

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
    env_details=$(az containerapp env show --name $CLUSTER_RESOURCE_GROUP_NAME --resource-group $CLUSTER_RESOURCE_GROUP_NAME)

    # Extract the customDomainVerificationId and defaultDomain from the container apps environment
    custom_domain_verification_id=$(echo "$env_details" | jq -r '.properties.customDomainConfiguration.customDomainVerificationId')
    default_domain=$(echo "$env_details" | jq -r '.properties.defaultDomain')

    # Display instructions for setting up DNS entries. The user-facing domain is served by the app-gateway
    # container, while the back-office domain is served by its own dedicated back-office container with
    # platform-level Easy Auth. Print both when configured so the user can see every missing record at
    # once instead of one per redeploy.
    echo -e "${RED}$(date +"%Y-%m-%dT%H:%M:%S") Please add the following DNS entries and then retry:${RESET}"
    if [[ -n "$DOMAIN_NAME" ]]; then
      echo -e "${RED}- A TXT record with the name 'asuid.$DOMAIN_NAME' and the value '$custom_domain_verification_id'.${RESET}"
      echo -e "${RED}- A CNAME record with the Host name '$DOMAIN_NAME' that points to address 'app-gateway.$default_domain'.${RESET}"
    fi
    if [[ -n "$BACK_OFFICE_DOMAIN_NAME" ]]; then
      echo -e "${RED}- A TXT record with the name 'asuid.$BACK_OFFICE_DOMAIN_NAME' and the value '$custom_domain_verification_id'.${RESET}"
      echo -e "${RED}- A CNAME record with the Host name '$BACK_OFFICE_DOMAIN_NAME' that points to address 'back-office.$default_domain'.${RESET}"
    fi
    exit 1
  elif [[ $output == *"ERROR:"* ]]; then
    echo -e "${RED}$output${RESET}"
    exit 1
  fi

  # Extract the ID of the Managed Identities, which can be used to grant access to PostgreSQL databases
  ACCOUNT_IDENTITY_CLIENT_ID=$(echo "$cleaned_output" | jq -r '.properties.outputs.accountIdentityClientId.value')
  MAIN_IDENTITY_CLIENT_ID=$(echo "$cleaned_output" | jq -r '.properties.outputs.mainIdentityClientId.value')
  if [[ -n "$GITHUB_OUTPUT" ]]; then
    echo "ACCOUNT_IDENTITY_CLIENT_ID=$ACCOUNT_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
    echo "MAIN_IDENTITY_CLIENT_ID=$MAIN_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
  else
    . ./grant-database-permissions.sh $UNIQUE_PREFIX $ENVIRONMENT $CLUSTER_LOCATION_ACRONYM 'account' $ACCOUNT_IDENTITY_CLIENT_ID
    . ./grant-database-permissions.sh $UNIQUE_PREFIX $ENVIRONMENT $CLUSTER_LOCATION_ACRONYM 'main' $MAIN_IDENTITY_CLIENT_ID
  fi
fi
