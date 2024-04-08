# Check if environment variables are set
ENVIRONMENT_VARIABLES_MISSING=false

if [[ -z "$ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID" ]]; then
  echo "ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID is not set."
  ENVIRONMENT_VARIABLES_MISSING=true
fi

if [[ -z "$CONTAINER_REGISTRY_NAME" ]]; then
  echo "CONTAINER_REGISTRY_NAME is not set."
  ENVIRONMENT_VARIABLES_MISSING=true
fi

if [[ -z "$UNIQUE_CLUSTER_PREFIX" ]]; then
  echo "UNIQUE_CLUSTER_PREFIX is not set."
  ENVIRONMENT_VARIABLES_MISSING=true
fi

if [[ $ENVIRONMENT_VARIABLES_MISSING == true ]]; then
  echo "Please follow the instructions in the README.md for setting up the required environment variables and try again."
  exit 1
fi

echo "$(date +"%Y-%m-%dT%H:%M:%S") All environment variables are set."

get_active_version()
{
   local image=$(az containerapp revision list --name $1 --resource-group $RESOURCE_GROUP_NAME --query "[0].properties.template.containers[0].image" --output tsv 2>/dev/null)
   
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

RESOURCE_GROUP_NAME="$ENVIRONMENT-$LOCATION_PREFIX"
APP_GATEWAY_VERSION=$(get_active_version app-gateway)
ACTIVE_ACCOUNT_MANAGEMENT_API_VERSION=$(get_active_version account-management-api)
IS_DOMAIN_CONFIGURED=$(is_domain_configured "app-gateway" "$RESOURCE_GROUP_NAME")

az extension add --name application-insights --allow-preview true
APPLICATIONINSIGHTS_CONNECTION_STRING=$(az monitor app-insights component show --app $ENVIRONMENT-application-insights --resource-group $ENVIRONMENT --query connectionString --output tsv)

DEPLOYMENT_COMMAND="az deployment sub create"
CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')
DEPLOYMENT_PARAMETERS="-l $LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p environment=$ENVIRONMENT locationPrefix=$LOCATION_PREFIX resourceGroupName=$RESOURCE_GROUP_NAME clusterUniqueName=$CLUSTER_UNIQUE_NAME useMssqlElasticPool=$USE_MSSQL_ELASTIC_POOL containerRegistryName=$CONTAINER_REGISTRY_NAME domainName=$DOMAIN_NAME isDomainConfigured=$IS_DOMAIN_CONFIGURED sqlAdminObjectId=$ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID appGatewayVersion=$APP_GATEWAY_VERSION accountManagementApiVersion=$ACTIVE_ACCOUNT_MANAGEMENT_API_VERSION applicationInsightsConnectionString=$APPLICATIONINSIGHTS_CONNECTION_STRING"

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

  # Check for the specific error message indicating that DNS Records are missing
  if [[ $output == *"InvalidCustomHostNameValidation"* ]] || [[ $output == *"FailedCnameValidation"* ]] || [[ $output == *"-certificate' under resource group '$RESOURCE_GROUP_NAME' was not found"* ]]; then
    # Get details about the container apps environment. Although the creation of the container app fails, the verification ID on the container apps environment is consistent across all container apps.
    env_details=$(az containerapp env show --name "$LOCATION_PREFIX-container-apps-environment" --resource-group "$RESOURCE_GROUP_NAME")
    
    # Extract the customDomainVerificationId and defaultDomain from the container apps environment
    custom_domain_verification_id=$(echo "$env_details" | jq -r '.properties.customDomainConfiguration.customDomainVerificationId')
    default_domain=$(echo "$env_details" | jq -r '.properties.defaultDomain')

    # Display instructions for setting up DNS entries
    echo -e "${RED}$(date +"%Y-%m-%dT%H:%M:%S") Please add the following DNS entries and then retry:${RESET}"
    echo -e "${RED}- A TXT record with the name 'asuid.$DOMAIN_NAME' and the value '$custom_domain_verification_id'.${RESET}"
    echo -e "${RED}- A CNAME record with the Host name '$DOMAIN_NAME' that points to address 'app-gateway.$default_domain'.${RESET}"
    exit 1
  elif [[ $output == "ERROR:"* ]]; then
    echo -e "${RED}$output${RESET}"
    exit 1
  fi

  # If the domain was not configured during the first run and we didn't receive any warnings about missing DNS entries, we trigger the deployment again to complete the binding of the SSL Certificate to the domain.
  if [[ "$IS_DOMAIN_CONFIGURED" == "false" ]] && [[ "$DOMAIN_NAME" != "" ]]; then
    echo "Running deployment again to finalize setting up SSL certificate for account-management"
    IS_DOMAIN_CONFIGURED=$(is_domain_configured "app-gateway" "$RESOURCE_GROUP_NAME")
    DEPLOYMENT_PARAMETERS="-l $LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p environment=$ENVIRONMENT locationPrefix=$LOCATION_PREFIX resourceGroupName=$RESOURCE_GROUP_NAME clusterUniqueName=$CLUSTER_UNIQUE_NAME useMssqlElasticPool=$USE_MSSQL_ELASTIC_POOL containerRegistryName=$CONTAINER_REGISTRY_NAME domainName=$DOMAIN_NAME isDomainConfigured=$IS_DOMAIN_CONFIGURED sqlAdminObjectId=$ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID appGatewayVersion=$APP_GATEWAY_VERSION accountManagementApiVersion=$ACTIVE_ACCOUNT_MANAGEMENT_API_VERSION applicationInsightsConnectionString=$APPLICATIONINSIGHTS_CONNECTION_STRING"

    . ../deploy.sh

    if [[ $output == "ERROR:"* ]]; then
      echo -e "${RED}$output"
      exit 1
    fi
  fi

  # Extract the ID of the Managed Identities, which can be used to grant access to SQL Database
  ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$(echo "$output" | jq -r '.properties.outputs.accountManagementIdentityClientId.value')
  if [[ -n "$GITHUB_OUTPUT" ]]; then
    echo "ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
  else
    . ./grant-database-permissions.sh 'account-management' $ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID
  fi
fi
