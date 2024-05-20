#!/bin/bash

ENVIRONMENT="prod"
LOCATION="WestEurope"
LOCATION_ACRONYM="weu"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy-cluster.sh
