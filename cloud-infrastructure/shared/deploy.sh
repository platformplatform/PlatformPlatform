resourceGroupName="shared"
deploymentCommand="az deployment sub create"
deploymentParamters="-l $location -n $resourceGroupName --output table -f ./main.bicep -p environment=$environment resourceGroupName=$resourceGroupName containerRegistryName=$CONTAINER_REGISTRY_NAME"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh