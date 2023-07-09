RESOURCE_GROUP_NAME="$ENVIRONMENT-monitor"
DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $LOCATION -n $ENVIRONMENT --output table -f ./main-environment.bicep -p environment=$ENVIRONMENT resourceGroupName=$RESOURCE_GROUP_NAME"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh