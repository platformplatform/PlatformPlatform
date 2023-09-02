param name string
param location string
param tags object
param sqlServerName string
param skuName string
param skuTier string
param skuCapacity int
param maxDatabaseCapacity int

resource microsoftSqlServerElasticPool 'Microsoft.Sql/servers/elasticPools@2022-11-01-preview' = {
  name: '${sqlServerName}/${name}'
  tags: tags
  location: location
  sku: {
    name: skuName
    tier: skuTier
    capacity: skuCapacity
  }
  properties: {
    perDatabaseSettings: {
      minCapacity: 0
      maxCapacity: maxDatabaseCapacity
    }
    zoneRedundant: false
  }
}
