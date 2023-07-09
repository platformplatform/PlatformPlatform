set -eo pipefail

if [[ "$*" == *"--plan"* ]]
then
    echo "Preparing plan..."
    $DEPLOYMENT_COMMAND -w $DEPLOYMENT_PARAMETERS
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "Applying changes..."
    $DEPLOYMENT_COMMAND $DEPLOYMENT_PARAMETERS
fi

if [[ "$1" == "" ]]
then
    echo "Detecting changes..."
   $DEPLOYMENT_COMMAND -c $DEPLOYMENT_PARAMETERS
fi
