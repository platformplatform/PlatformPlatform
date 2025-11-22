#!/bin/bash

UNIQUE_PREFIX=$1
ENVIRONMENT=$2
LOCATION_SHARED=$3
PRODUCTION_SERVICE_PRINCIPAL_OBJECT_ID=$4

export UNIQUE_PREFIX
export ENVIRONMENT

GLOBAL_RESOURCE_GROUP_NAME=$UNIQUE_PREFIX-$ENVIRONMENT-global
CONTAINER_REGISTRY_NAME=$UNIQUE_PREFIX$ENVIRONMENT
CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')
DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $LOCATION_SHARED -n $CURRENT_DATE-$UNIQUE_PREFIX-$ENVIRONMENT --output table -f ./main-environment.bicep -p globalResourceGroupName=$GLOBAL_RESOURCE_GROUP_NAME uniquePrefix=$UNIQUE_PREFIX environment=$ENVIRONMENT containerRegistryName=$CONTAINER_REGISTRY_NAME"

# Add production service principal object ID parameter if provided for ACR Pull role assignment to staging Container Registry
if [[ "$PRODUCTION_SERVICE_PRINCIPAL_OBJECT_ID" != "-" ]]; then
  echo "Using production service principal object ID: $PRODUCTION_SERVICE_PRINCIPAL_OBJECT_ID"
  DEPLOYMENT_PARAMETERS="$DEPLOYMENT_PARAMETERS productionServicePrincipalObjectId=$PRODUCTION_SERVICE_PRINCIPAL_OBJECT_ID"
fi

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh

if [[ $output == *"ERROR:"* ]]; then
  echo -e "${RED}$output"
  exit 1
fi
