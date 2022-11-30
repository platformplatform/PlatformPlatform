resourceGroupName="$environment-monitor"
deploymentCommand="az deployment sub create"
deploymentParamters="-l $location -n $environment --output table -f ./main.bicep -p environment=$environment resourceGroupName=$resourceGroupName"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh