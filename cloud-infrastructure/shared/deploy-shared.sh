# Check if environment variables are set
ENVIRONMENT_VARIBELS_MISSING=false

if [[ -z "$CONTAINER_REGISTRY_NAME" ]]; then
  echo "CONTAINER_REGISTRY_NAME is not set."
  ENVIRONMENT_VARIBELS_MISSING=true
fi

if [[ $ENVIRONMENT_VARIBELS_MISSING == true ]]; then
  echo -e "Please follow the instructions in the README.md for setting up the required environment variables and try again."
  exit 1
fi

RESOURCE_GROUP_NAME="shared"
DEPLOYMENT_COMMAND="az deployment sub create"
CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')

DEPLOYMENT_PARAMETERS="-l $LOCATION -n "$CURRENT_DATE-$RESOURCE_GROUP_NAME" --output table -f ./main-shared.bicep -p environment=$ENVIRONMENT resourceGroupName=$RESOURCE_GROUP_NAME containerRegistryName=$CONTAINER_REGISTRY_NAME"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh