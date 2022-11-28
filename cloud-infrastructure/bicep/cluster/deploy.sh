cd "$(dirname "${BASH_SOURCE[0]}")"

if [[ "$*" == *"--plan"* ]]
then
    echo "Preparing plan..."
    az deployment sub create -w -l "$location" -n $resourceGroupName --output table -f ./main.bicep -p environment=$environment locationPrefix=$locationPrefix resourceGroupName=$resourceGroupName clusterUniqueName=$clusterUniqueName
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "Applying changes..."
    az deployment sub create    -l "$location" -n $resourceGroupName --output table -f ./main.bicep -p environment=$environment locationPrefix=$locationPrefix resourceGroupName=$resourceGroupName clusterUniqueName=$clusterUniqueName
fi

if [[ "$1" == "" ]]
then
    echo "Detecthing changes..."
    az deployment sub create -c -l "$location" -n $resourceGroupName --output table -f ./main.bicep -p environment=$environment locationPrefix=$locationPrefix resourceGroupName=$resourceGroupName clusterUniqueName=$clusterUniqueName
fi