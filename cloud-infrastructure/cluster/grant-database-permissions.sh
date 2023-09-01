RESOURCE_GROUP_NAME="$ENVIRONMENT-$LOCATION_PREFIX"
MANAGED_IDENTITY="$1-$RESOURCE_GROUP_NAME"
SQL_DATABASE=$1
SQL_SERVER="$SQL_SERVER_NAME.database.windows.net"

# Convert the ClientId of the Managed Identity to the binary version. The following bash script is equivalent to this PowerShell:
#   $SID = "0x" + [System.BitConverter]::ToString(([guid]$SID).ToByteArray()).Replace("-", "")
SID=$(echo $2 | tr 'a-f' 'A-F' | tr -d '-') # Convert to uppercase and remove hyphens
SID=$(awk -v id="$SID" 'BEGIN {
  printf "0x%s%s%s%s\n",
    substr(id,7,2) substr(id,5,2) substr(id,3,2) substr(id,1,2),
    substr(id,11,2) substr(id,9,2),
    substr(id,15,2) substr(id,13,2),
    substr(id,17)
}') # Reverse the byte order for the first three sections of the GUID and concatenate

echo "Granting $MANAGED_IDENTITY (ID: $SID) in Recource group $RESOURCE_GROUP_NAME permissions on $SQL_SERVER/$SQL_DATABASE database"

# Execute the SQL script using mssql-scripter. Pass the script as a heredoc to sqlcmd to allow for complex SQL.
sqlcmd -S $SQL_SERVER -d $SQL_DATABASE --authentication-method=ActiveDirectoryDefault --exit-on-error << EOF
IF NOT EXISTS (SELECT [name] FROM [sys].[database_principals] WHERE [name] = '$MANAGED_IDENTITY' AND [type] = 'E')
BEGIN
    CREATE USER [$MANAGED_IDENTITY] WITH SID = $SID, TYPE = E;
    ALTER ROLE db_datareader ADD MEMBER [$MANAGED_IDENTITY];
    ALTER ROLE db_datawriter ADD MEMBER [$MANAGED_IDENTITY];
    ALTER ROLE db_ddladmin ADD MEMBER [$MANAGED_IDENTITY];
END
GO
EOF
