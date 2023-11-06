RESOURCE_GROUP_NAME="$ENVIRONMENT"
DEPLOYMENT_COMMAND="az deployment sub create"
CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')

DEPLOYMENT_PARAMETERS="-l $LOCATION -n "$CURRENT_DATE-$ENVIRONMENT" --output table -f ./main-environment.bicep -p environment=$ENVIRONMENT resourceGroupName=$RESOURCE_GROUP_NAME"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh

if [[ $output == "ERROR:"* ]]; then
  echo -e "${RED}$output"
  exit 1
fi
