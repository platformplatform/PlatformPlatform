#!/bin/bash

ENVIRONMENT="development"
LOCATION="WestEurope"
LOCATION_PREFIX="west-europe"
CLUSTER_UNIQUE_NAME="${UNIQUE_CLUSTER_PREFIX}devweu"
USE_MSSQL_ELASTIC_POOL=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy-cluster.sh
