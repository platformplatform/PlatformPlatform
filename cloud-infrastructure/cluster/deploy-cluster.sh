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

DEPLOYMENT_PARAMETERS="-l $LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output json -f ./main-cluster.bicep -p environment=$ENVIRONMENT locationPrefix=$LOCATION_PREFIX resourceGroupName=$RESOURCE_GROUP_NAME clusterUniqueName=$CLUSTER_UNIQUE_NAME useMssqlElasticPool=$USE_MSSQL_ELASTIC_POOL containerRegistryName=$CONTAINER_REGISTRY_NAME sqlAdminObjectId=$ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh

if [[ "$*" == *"--plan"* ]]
then
    exit 0
fi

if [[ "$*" == *"--apply"* ]]
then
    #Grant permissions to the account management manged identity to the account-management database
    SQL_SERVER_NAME=$CLUSTER_UNIQUE_NAME

    trap '. ./firewall.sh close' EXIT # Ensure that the firewall is closed no matter if other commands fail
    . ./firewall.sh open

    accountManagementIdentityClientId=$(echo "$output" | jq -r '.properties.outputs.accountManagementIdentityClientId.value')
    . ./grant-database-permissions.sh 'account-management' $accountManagementIdentityClientId
fi
