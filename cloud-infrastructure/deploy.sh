set -eo pipefail

if [[ "$1" == "" ]]
then
    echo "::error::Please specify either the --plan or the--apply parameter."
    exit 1
fi

if [[ "$*" == *"--plan"* ]]
then
    echo "Preparing plan..."
    $DEPLOYMENT_COMMAND -w $DEPLOYMENT_PARAMETERS
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "Applying changes..."
    export output=$($DEPLOYMENT_COMMAND $DEPLOYMENT_PARAMETERS)
fi
