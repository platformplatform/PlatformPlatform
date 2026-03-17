param name string
param location string
param tags object
param tenantId string
param subnetId string
param virtualNetworkId string
param isProduction bool

resource postgresServer 'Microsoft.DBforPostgreSQL/flexibleServers@2025-08-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: isProduction ? 'Standard_D2ds_v5' : 'Standard_B1ms'
    tier: isProduction ? 'GeneralPurpose' : 'Burstable'
  }
  properties: {
    version: '17'
    createMode: 'Default'
    authConfig: {
      activeDirectoryAuth: 'Enabled'
      passwordAuth: 'Disabled'
      tenantId: tenantId
    }
    storage: {
      storageSizeGB: isProduction ? 32 : 32 // Change to 64, 128, etc. if more space is needed
    }
    backup: {
      backupRetentionDays: isProduction ? 35 : 7
      geoRedundantBackup: isProduction ? 'Enabled' : 'Disabled'
    }
    highAvailability: {
      // Zone-redundant HA provides automatic failover (<120s) with zero data loss and 99.99% SLA,
      // but doubles the PostgreSQL cost. Requires General Purpose SKU (already used for production).
      mode: isProduction ? 'Disabled' : 'Disabled'
    }
    network: {
      // Public access is enabled because GitHub-hosted Actions runners cannot reach VNet resources. No permanent
      // firewall rules exist -- runner IPs are added temporarily during CI/CD and removed immediately after.
      // Runtime traffic from Container Apps flows exclusively through the private endpoint.
      publicNetworkAccess: 'Enabled'
    }
  }
}

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2024-06-01' = {
  name: 'privatelink.postgres.database.azure.com'
  location: 'global'
  tags: tags
}

resource privateDnsZoneVnetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2024-06-01' = {
  parent: privateDnsZone
  name: '${name}-vnet-link'
  location: 'global'
  properties: {
    virtualNetwork: {
      id: virtualNetworkId
    }
    registrationEnabled: false
  }
}

resource privateEndpoint 'Microsoft.Network/privateEndpoints@2025-01-01' = {
  name: '${name}-postgres'
  location: location
  tags: tags
  properties: {
    customNetworkInterfaceName: '${name}-postgres'
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: '${name}-postgres-connection'
        properties: {
          privateLinkServiceId: postgresServer.id
          groupIds: ['postgresqlServer']
        }
      }
    ]
  }
}

resource privateDnsZoneGroup 'Microsoft.Network/privateEndpoints/privateDnsZoneGroups@2025-01-01' = {
  parent: privateEndpoint
  name: 'default'
  dependsOn: [privateDnsZoneVnetLink]
  properties: {
    privateDnsZoneConfigs: [
      {
        name: 'postgres'
        properties: {
          privateDnsZoneId: privateDnsZone.id
        }
      }
    ]
  }
}

resource extensionsConfig 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2025-08-01' = {
  parent: postgresServer
  name: 'azure.extensions'
  dependsOn: [privateDnsZoneGroup]
  properties: {
    value: 'pg_stat_statements'
    source: 'user-override'
  }
}

resource walLevelConfig 'Microsoft.DBforPostgreSQL/flexibleServers/configurations@2025-08-01' = {
  parent: postgresServer
  name: 'wal_level'
  dependsOn: [extensionsConfig]
  properties: {
    value: 'logical'
    source: 'user-override'
  }
}

output serverName string = postgresServer.name
output serverFqdn string = postgresServer.properties.fullyQualifiedDomainName
