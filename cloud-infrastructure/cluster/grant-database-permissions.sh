UNIQUE_PREFIX=$1
ENVIRONMENT=$2
CLUSTER_LOCATION_ACRONYM=$3
DATABASE_NAME=$4
MANAGED_IDENTITY_CLIENT_ID=$5

CLUSTER_RESOURCE_GROUP_NAME=$UNIQUE_PREFIX-$ENVIRONMENT-$CLUSTER_LOCATION_ACRONYM
MANAGED_IDENTITY_NAME=$CLUSTER_RESOURCE_GROUP_NAME-$4
POSTGRES_SERVER_NAME=$CLUSTER_RESOURCE_GROUP_NAME
POSTGRES_HOST=$POSTGRES_SERVER_NAME.postgres.database.azure.com

cd "$(dirname "${BASH_SOURCE[0]}")"

# Get an access token for PostgreSQL using the current Azure CLI identity
ACCESS_TOKEN=$(az account get-access-token --resource-type oss-rdbms --query accessToken --output tsv)

echo "$(date +"%Y-%m-%dT%H:%M:%S") Granting $MANAGED_IDENTITY_NAME (Client ID: $MANAGED_IDENTITY_CLIENT_ID) permissions on $POSTGRES_HOST/$DATABASE_NAME database"

# PostgreSQL uses roles instead of SQL Server users. Create a role for the managed identity
# and grant it the necessary permissions. The pgaadauth_create_principal function handles
# Entra ID principal creation in Azure Database for PostgreSQL Flexible Server.
PGPASSWORD=$ACCESS_TOKEN psql "host=$POSTGRES_HOST dbname=$DATABASE_NAME user=$(az account show --query user.name --output tsv) sslmode=require" << EOF
DO \$\$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$MANAGED_IDENTITY_NAME') THEN
        PERFORM pgaadauth_create_principal('$MANAGED_IDENTITY_NAME', false, false);
    END IF;
END
\$\$;

GRANT CONNECT ON DATABASE "$DATABASE_NAME" TO "$MANAGED_IDENTITY_NAME";
GRANT USAGE ON SCHEMA public TO "$MANAGED_IDENTITY_NAME";
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO "$MANAGED_IDENTITY_NAME";
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO "$MANAGED_IDENTITY_NAME";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO "$MANAGED_IDENTITY_NAME";
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT USAGE, SELECT ON SEQUENCES TO "$MANAGED_IDENTITY_NAME";
EOF
