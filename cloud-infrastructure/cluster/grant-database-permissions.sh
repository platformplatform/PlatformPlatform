MANAGED_IDENTITY="$1-$RESOURCE_GROUP_NAME"
SQL_DATABASE=$1
SQL_SERVER="$CLUSTER_UNIQUE_NAME.database.windows.net"

echo Granting database permissions to $MANAGED_IDENTITY on $SQL_SERVER/$SQL_DATABASE

# Execute the SQL script using mssql-scripter. Pass the script as a heredoc to sqlcmd to allow for complex SQL.
sqlcmd -S $SQL_SERVER -d $SQL_DATABASE --authentication-method=ActiveDirectoryDefault << EOF
IF NOT EXISTS (SELECT [name] FROM [sys].[database_principals] WHERE [name] = '$MANAGED_IDENTITY' AND [type] = 'E')
BEGIN
    CREATE USER [$MANAGED_IDENTITY] FROM EXTERNAL PROVIDER;
    ALTER ROLE db_datareader ADD MEMBER [$MANAGED_IDENTITY];
    ALTER ROLE db_datawriter ADD MEMBER [$MANAGED_IDENTITY];
    ALTER ROLE db_ddladmin ADD MEMBER [$MANAGED_IDENTITY];
END
GO
EOF
