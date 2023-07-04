resourceGroupName="$environment-$locationPrefix"
deploymentCommand="az deployment sub create"

if [[ $environment == "testing" ]]
then
    containerRegistryName="platformplatformtest"
    # containerRegistrySubscriptionId=$(az account show --subscription "Testing" --query "id" --output tsv)
    containerRegistrySubscriptionId="44778978-99ea-4d41-91ab-8b00e79ddac4"
    echo "Found subscription id for Testing environment: $containerRegistrySubscriptionId"
else
    containerRegistryName="platformplatform"
    containerRegistrySubscriptionId=$(az account show --subscription "Shared" --query "id" --output tsv)
    echo "Found subscription id for Shared environment: $containerRegistrySubscriptionId"
fi

deploymentParamters="-l $location -n $resourceGroupName --output table -f ./main.bicep -p environment=$environment locationPrefix=$locationPrefix resourceGroupName=$resourceGroupName clusterUniqueName=$clusterUniqueName useMssqlElasticPool=$useMssqlElasticPool containerRegistryName=$containerRegistryName containerRegistrySubscriptionId=$containerRegistrySubscriptionId"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh