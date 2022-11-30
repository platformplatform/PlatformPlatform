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
    echo "Detecthing changes..."
   $deploymentCommand -c $deploymentParamters
fi