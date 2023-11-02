set -eo pipefail

if [[ "$1" == "" ]]
then
    echo "::error::Please specify either the --plan or the--apply parameter."
    exit 1
fi

if [[ "$*" == *"--plan"* ]]
then
    echo "$(date +"%Y-%m-%dT%H:%M:%S") Preparing plan..."
    $DEPLOYMENT_COMMAND -w $DEPLOYMENT_PARAMETERS
    if [[ $? -ne 0 ]]; then
        echo "::error::Plan preparation failed."
        exit 1
    fi
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "$(date +"%Y-%m-%dT%H:%M:%S") Applying changes..."
    export output=$($DEPLOYMENT_COMMAND $DEPLOYMENT_PARAMETERS | tee /dev/tty)
fi
