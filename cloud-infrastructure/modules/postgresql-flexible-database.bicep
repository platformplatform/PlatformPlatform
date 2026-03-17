param serverName string
param databaseName string

resource postgresDatabase 'Microsoft.DBforPostgreSQL/flexibleServers/databases@2025-08-01' = {
  name: '${serverName}/${databaseName}'
  properties: {
    charset: 'UTF8'
    collation: 'en_US.utf8'
  }
}

output connectionString string = 'Host=${serverName}.postgres.database.azure.com;Database=${databaseName};Ssl Mode=VerifyFull'
