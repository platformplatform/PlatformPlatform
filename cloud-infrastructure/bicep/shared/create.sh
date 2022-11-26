cd "$(dirname "${BASH_SOURCE[0]}")"
az group create -n $resourceGroupName -l "$location" --output table --tags environment=$environment
az deployment group create --what-if -g $resourceGroupName --mode complete --output table -f ./main.bicep -p containerRegistryName=$containerRegistryName environment=$environment
az deployment group create           -g $resourceGroupName --mode complete --output table -f ./main.bicep -p containerRegistryName=$containerRegistryName environment=$environment