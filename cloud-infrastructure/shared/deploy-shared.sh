RESOURCE_GROUP_NAME="shared"
DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $LOCATION -n $RESOURCE_GROUP_NAME --output table -f ./main-shared.bicep -p environment=$ENVIRONMENT resourceGroupName=$RESOURCE_GROUP_NAME containerRegistryName=$CONTAINER_REGISTRY_NAME"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh


if [[ "$1" == "" ]] || [[ "$*" == *"--apply"* ]]
then
    echo "Uploading dummy quickstart container image..."
    az acr login --name $CONTAINER_REGISTRY_NAME
    docker pull mcr.microsoft.com/k8se/quickstart:latest --platform linux/amd64
    docker tag mcr.microsoft.com/k8se/quickstart:latest $CONTAINER_REGISTRY_NAME.azurecr.io/quickstart:latest
    docker push $CONTAINER_REGISTRY_NAME.azurecr.io/quickstart:latest
fi  