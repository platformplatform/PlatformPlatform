set -eo pipefail
CYAN='\033[0;36m'
RESET='\033[0m' # Reset formatting

if [[ "$1" == "" ]]
then
    echo "::error::Please specify either the --plan or the--apply parameter."
    exit 1
fi

if [[ "$*" == *"--plan"* ]]
then
    echo "$(date +"%Y-%m-%dT%H:%M:%S") Preparing plan..."
    echo -e "$CYAN$DEPLOYMENT_COMMAND -w $DEPLOYMENT_PARAMETERS$RESET"
    $DEPLOYMENT_COMMAND -w $DEPLOYMENT_PARAMETERS
    if [[ $? -ne 0 ]]; then
        echo "::error::Plan preparation failed."
        exit 1
    fi
fi

if [[ "$*" == *"--apply"* ]]
then
    echo "$(date +"%Y-%m-%dT%H:%M:%S") Applying changes..."
    echo -e "$CYAN$DEPLOYMENT_COMMAND $DEPLOYMENT_PARAMETERS$RESET"
    export output=$($DEPLOYMENT_COMMAND $DEPLOYMENT_PARAMETERS 2>&1)
fi
