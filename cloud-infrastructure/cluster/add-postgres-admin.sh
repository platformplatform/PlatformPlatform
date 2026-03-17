set -e

UNIQUE_PREFIX=$1
ENVIRONMENT=$2
CLUSTER_LOCATION_ACRONYM=$3
POSTGRES_ADMIN_OBJECT_ID=$4

CLUSTER_RESOURCE_GROUP_NAME=$UNIQUE_PREFIX-$ENVIRONMENT-$CLUSTER_LOCATION_ACRONYM
POSTGRES_SERVER_NAME=$CLUSTER_RESOURCE_GROUP_NAME

echo "$(date +"%Y-%m-%dT%H:%M:%S") Adding Entra ID group $POSTGRES_ADMIN_OBJECT_ID as admin on PostgreSQL server $POSTGRES_SERVER_NAME"

az postgres flexible-server microsoft-entra-admin create \
  --resource-group $CLUSTER_RESOURCE_GROUP_NAME \
  --server-name $POSTGRES_SERVER_NAME \
  --display-name "PostgreSQL Admins" \
  --object-id $POSTGRES_ADMIN_OBJECT_ID \
  --type Group
