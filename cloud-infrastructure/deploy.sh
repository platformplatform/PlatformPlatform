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
