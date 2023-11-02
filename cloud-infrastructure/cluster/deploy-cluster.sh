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
else
    echo "$(date +"%Y-%m-%dT%H:%M:%S") All environment variables are set."
fi

RESOURCE_GROUP_NAME="$ENVIRONMENT-$LOCATION_PREFIX"
DEPLOYMENT_COMMAND="az deployment sub create"
CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')

get_active_version() {
  local image=$(az containerapp revision list --name $1 --resource-group $RESOURCE_GROUP_NAME --query "[0].properties.template.containers[0].image" --output tsv 2>/dev/null)
  [ -z "$image" ] && echo "latest" || echo ${image##*:}
}

ACTIVE_ACCOUNT_MANAGEMENT_API=$(get_active_version account-management-api)
[ "$ACTIVE_ACCOUNT_MANAGEMENT_API" == "latest" ] && ACCOUNT_MANAGEMENT_API_CERTIFICATE_EXISTS=false || ACCOUNT_MANAGEMENT_API_CERTIFICATE_EXISTS=true

DEPLOYMENT_PARAMETERS="-l $LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p environment=$ENVIRONMENT locationPrefix=$LOCATION_PREFIX resourceGroupName=$RESOURCE_GROUP_NAME clusterUniqueName=$CLUSTER_UNIQUE_NAME useMssqlElasticPool=$USE_MSSQL_ELASTIC_POOL containerRegistryName=$CONTAINER_REGISTRY_NAME sqlAdminObjectId=$ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID accountManagementApiVersion=$ACTIVE_ACCOUNT_MANAGEMENT_API accountManagementApiCertificateExists=$ACCOUNT_MANAGEMENT_API_CERTIFICATE_EXISTS"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh

ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$(echo "$output" | jq -r '.properties.outputs.accountManagementIdentityClientId.value')
if [[ -n "$GITHUB_OUTPUT" ]]; then
    echo "ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID=$ACCOUNT_MANAGEMENT_IDENTITY_CLIENT_ID" >> $GITHUB_OUTPUT
fi