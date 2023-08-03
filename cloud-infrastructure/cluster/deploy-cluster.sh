# Check if environment variables are set
ENVIRONMENT_VARIBELS_MISSING=false

if [[ -z "$ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID" ]]; then
  echo "ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID is not set."
  ENVIRONMENT_VARIBELS_MISSING=true
fi

if [[ -z "$CONTAINER_REGISTRY_NAME" ]]; then
  echo "CONTAINER_REGISTRY_NAME is not set."
  ENVIRONMENT_VARIBELS_MISSING=true
fi

if [[ -z "$UNIQUE_CLUSTER_PREFIX" ]]; then
  echo "UNIQUE_CLUSTER_PREFIX is not set."
  ENVIRONMENT_VARIBELS_MISSING=true
fi

if [[ $ENVIRONMENT_VARIBELS_MISSING == true ]]; then
  echo -e "Please follow the instructions in the README.md for setting up the required environment variables and try again."
  exit 1
fi

RESOURCE_GROUP_NAME="$ENVIRONMENT-$LOCATION_PREFIX"
DEPLOYMENT_COMMAND="az deployment sub create"
CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')

DEPLOYMENT_PARAMETERS="-l $LOCATION -n $CURRENT_DATE-$RESOURCE_GROUP_NAME --output table -f ./main-cluster.bicep -p environment=$ENVIRONMENT locationPrefix=$LOCATION_PREFIX resourceGroupName=$RESOURCE_GROUP_NAME clusterUniqueName=$CLUSTER_UNIQUE_NAME useMssqlElasticPool=$USE_MSSQL_ELASTIC_POOL containerRegistryName=$CONTAINER_REGISTRY_NAME sqlAdminObjectId=$ACTIVE_DIRECTORY_SQL_ADMIN_OBJECT_ID"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh

if [[ "$*" != *"--plan"* ]]
then
  . ./firewall.sh open
  . ./grant-database-permissions.sh 'account-management'
  . ./firewall.sh close
fi
