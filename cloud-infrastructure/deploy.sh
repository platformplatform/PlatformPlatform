activeDirectoryGroupName="Azure-Container-Registry-Pull"

containerRegistryPullAdGroupId=$(az ad group show --group "$activeDirectoryGroupName" --query id --output tsv)

if [ -z "$containerRegistryPullAdGroupId" ]
then
  echo "Active Directory group $activeDirectoryGroupName does not exist. Please create the group before proceeding as described in the README.md."
  exit 1
fi

deploymentParamters+=" --parameters containerRegistryPullAdGroupId=$containerRegistryPullAdGroupId"

set -eo pipefail

if [[ "$*" == *"--plan"* ]]
then
    echo "Preparing plan..."
    $deploymentCommand -w $deploymentParamters
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "Applying changes..."
    $deploymentCommand $deploymentParamters
fi

if [[ "$1" == "" ]]
then
    echo "Detecting changes..."
   $deploymentCommand -c $deploymentParamters
fi
