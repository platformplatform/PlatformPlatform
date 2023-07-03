resourceGroupName="$environment-$locationPrefix"
deploymentCommand="az deployment sub create"

if [[ $environment == "testing" ]]
then
    acrSubscriptionId=$(az account show --subscription "Testing" --query "id" --output tsv)
    echo "Found subscription id for Testing environment: $acrSubscriptionId"
else
    acrSubscriptionId=$(az account show --subscription "Shared" --query "id" --output tsv)
    echo "Found subscription id for Shared environment: $acrSubscriptionId"
fi

deploymentParamters="-l $location -n $resourceGroupName --output table -f ./main.bicep -p environment=$environment locationPrefix=$locationPrefix resourceGroupName=$resourceGroupName clusterUniqueName=$clusterUniqueName useMssqlElasticPool=$useMssqlElasticPool acrName=$acrName acrSubscriptionId=$acrSubscriptionId"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh