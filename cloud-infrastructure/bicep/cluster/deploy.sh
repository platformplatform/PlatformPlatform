cd "$(dirname "${BASH_SOURCE[0]}")"

if [[ "$*" == *"--plan"* ]]
then
    echo "Preparing plan..."
    az deployment sub create -w -l "$location" --output table -f ./main.bicep -p environment=$environment resourceGroupName=$resourceGroupName
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "Applying changes..."
    az deployment sub create    -l "$location" --output table -f ./main.bicep -p environment=$environment resourceGroupName=$resourceGroupName
fi

if [[ "$1" == "" ]]
then
    echo "Detecthing changes..."
    az deployment sub create -c -l "$location" --output table -f ./main.bicep -p environment=$environment resourceGroupName=$resourceGroupName
fi