resourceGroupName="$environment-$locationPrefix"
deploymentCommand="az deployment sub create"
containerRegistryName=$CONTAINER_REGISTRY_NAME

deploymentParamters="-l $location -n $resourceGroupName --output table -f ./main.bicep -p environment=$environment locationPrefix=$locationPrefix resourceGroupName=$resourceGroupName clusterUniqueName=$clusterUniqueName useMssqlElasticPool=$useMssqlElasticPool containerRegistryName=$containerRegistryName"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh