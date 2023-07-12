RESOURCE_GROUP_NAME="shared"
DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $LOCATION -n $RESOURCE_GROUP_NAME --output table -f ./main-shared.bicep -p environment=$ENVIRONMENT resourceGroupName=$RESOURCE_GROUP_NAME containerRegistryName=$CONTAINER_REGISTRY_NAME"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh


if [[ "$1" == "" ]] || [[ "$*" == *"--apply"* ]]
then
    echo "Uploading dummy hello world container image..."
    az acr login --name $CONTAINER_REGISTRY_NAME
    docker pull --platform linux/amd64 hello-world
    docker tag hello-world $CONTAINER_REGISTRY_NAME.azurecr.io/hello-world:latest
    docker push $CONTAINER_REGISTRY_NAME.azurecr.io/hello-world:latest
fi