cd "$(dirname "${BASH_SOURCE[0]}")"
az group create -n $resourceGroupName -l "$location" --output table --tags environment=$environment

az deployment group create --what-if -g $resourceGroupName --mode complete -f ./main.bicep -p containerRegistryName=$containerRegistryName environment=$environment

if [[ "$1" == "apply" ]]
then
    az deployment group create --output table -g $resourceGroupName --mode complete -f ./main.bicep -p containerRegistryName=$containerRegistryName environment=$environment
fi