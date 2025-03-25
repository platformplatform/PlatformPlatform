UNIQUE_PREFIX=$1
ENVIRONMENT=$2
CLUSTER_LOCATION_ACRONYM=$3
SQL_DATABASE_NAME=$4
MANAGEMENT_IDENTITY_CLIENT_ID=$5

RESOURCE_GROUP_NAME=$UNIQUE_PREFIX-$ENVIRONMENT-$CLUSTER_LOCATION_ACRONYM
MANAGED_IDENTITY_NAME=$RESOURCE_GROUP_NAME-$4
SQL_SERVER_NAME=$RESOURCE_GROUP_NAME
SQL_SERVER=$SQL_SERVER_NAME.database.windows.net

cd "$(dirname "${BASH_SOURCE[0]}")"
# Export SQL_DATABASE_NAME for firewall.sh to use
export SQL_DATABASE_NAME=$SQL_DATABASE_NAME
trap '. ./firewall.sh close' EXIT # Ensure that the firewall is closed no matter if other commands fail
. ./firewall.sh open

# Convert the ClientId of the Managed Identity to the binary version. The following bash script is equivalent to this PowerShell:
#   $SID = "0x" + [System.BitConverter]::ToString(([guid]$SID).ToByteArray()).Replace("-", "")
SID=$(echo $MANAGEMENT_IDENTITY_CLIENT_ID | tr 'a-f' 'A-F' | tr -d '-') # Convert to uppercase and remove hyphens
SID=$(awk -v id="$SID" 'BEGIN {
  printf "0x%s%s%s%s\n",
    substr(id,7,2) substr(id,5,2) substr(id,3,2) substr(id,1,2),
    substr(id,11,2) substr(id,9,2),
    substr(id,15,2) substr(id,13,2),
    substr(id,17)
}') # Reverse the byte order for the first three sections of the GUID and concatenate

echo "$(date +"%Y-%m-%dT%H:%M:%S") Granting $MANAGED_IDENTITY_NAME (ID: $SID) in Recource group $RESOURCE_GROUP_NAME permissions on $SQL_SERVER/$SQL_DATABASE_NAME database"

# Execute the SQL script using mssql-scripter. Pass the script as a heredoc to sqlcmd to allow for complex SQL.
sqlcmd -S $SQL_SERVER -d $SQL_DATABASE_NAME --authentication-method=ActiveDirectoryDefault --exit-on-error << EOF
IF NOT EXISTS (SELECT [name] FROM [sys].[database_principals] WHERE [name] = '$MANAGED_IDENTITY_NAME' AND [type] = 'E')
BEGIN
    CREATE USER [$MANAGED_IDENTITY_NAME] WITH SID = $SID, TYPE = E;
    ALTER ROLE db_datareader ADD MEMBER [$MANAGED_IDENTITY_NAME];
    ALTER ROLE db_datawriter ADD MEMBER [$MANAGED_IDENTITY_NAME];
    ALTER ROLE db_ddladmin ADD MEMBER [$MANAGED_IDENTITY_NAME];
END
GO
EOF
