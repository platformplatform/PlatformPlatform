cd "$(dirname "${BASH_SOURCE[0]}")"

az deployment sub create --what-if -l "$location" --output table -f ./main.bicep -p containerRegistryName=$containerRegistryName environment=$environment

if [[ "$1" == "apply" ]]
then
    az deployment sub create       -l "$location" --output table -f ./main.bicep -p containerRegistryName=$containerRegistryName environment=$environment
fi