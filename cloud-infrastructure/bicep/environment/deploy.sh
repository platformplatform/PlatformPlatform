cd "$(dirname "${BASH_SOURCE[0]}")"

if [[ "$*" == *"--plan"* ]]
then
    echo "Preparing plan..."
    az deployment sub create -w -l "$location" -n $environment --output table -f ./main.bicep -p environment=$environment
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "Applying changes..."
    az deployment sub create    -l "$location" -n $environment --output table -f ./main.bicep -p environment=$environment
fi

if [[ "$1" == "" ]]
then
    echo "Detecthing changes..."
    az deployment sub create -c -l "$location" -n $environment --output table -f ./main.bicep -p environment=$environment
fi