    RESOURCE_GROUP_NAME="$ENVIRONMENT-$LOCATION_PREFIX"
    DEPLOYMENT_COMMAND="az deployment sub create"

    DEPLOYMENT_PARAMETERS="-l $LOCATION -n $RESOURCE_GROUP_NAME --output table -f ./main-cluster.bicep -p environment=$ENVIRONMENT locationPrefix=$LOCATION_PREFIX resourceGroupName=$RESOURCE_GROUP_NAME clusterUniqueName=$CLUSTER_UNIQUE_NAME useMssqlElasticPool=$USE_MSSQL_ELASTIC_POOL containerRegistryName=$CONTAINER_REGISTRY_NAME"

    cd "$(dirname "${BASH_SOURCE[0]}")"
    . ../deploy.sh