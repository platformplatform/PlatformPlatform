#!/bin/bash

UNIQUE_PREFIX=$1
ENVIRONMENT=$2
LOCATION_SHARED=$3

RESOURCE_GROUP_NAME=$UNIQUE_PREFIX-$ENVIRONMENT
CONTAINER_REGISTRY_NAME=$UNIQUE_PREFIX$ENVIRONMENT
CURRENT_DATE=$(date +'%Y-%m-%dT%H-%M')
DEPLOYMENT_COMMAND="az deployment sub create"
DEPLOYMENT_PARAMETERS="-l $LOCATION_SHARED -n $CURRENT_DATE-$UNIQUE_PREFIX-$ENVIRONMENT --output table -f ./main-environment.bicep -p resourceGroupName=$RESOURCE_GROUP_NAME environment=$ENVIRONMENT containerRegistryName=$CONTAINER_REGISTRY_NAME"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh

cleaned_output=$(echo "$output" | sed '/^WARNING/d')
if [[ $cleaned_output == *"ERROR:"* ]]; then
  echo -e "${RED}$output"
  exit 1
fi
