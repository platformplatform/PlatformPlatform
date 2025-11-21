param name string
param location string
param tags object
param subnetId string
param tenantId string
param sqlAdminObjectId string

resource microsoftSqlServer 'Microsoft.Sql/servers@2023-08-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Group'
      login: 'Azure SQL Server Admins'
      sid: sqlAdminObjectId
      tenantId: tenantId
      azureADOnlyAuthentication: true
    }
    restrictOutboundNetworkAccess: 'Enabled'
  }
}

resource sqlServerVirtualNetworkRule 'Microsoft.Sql/servers/virtualNetworkRules@2023-08-01' = {
  name: 'sql-server-virtual-network-rule'
  parent: microsoftSqlServer
  properties: {
    ignoreMissingVnetServiceEndpoint: true
    virtualNetworkSubnetId: subnetId
  }
}

resource microsoftSqlServerSecurityAlertPolicies 'Microsoft.Sql/servers/securityAlertPolicies@2023-08-01' = {
  parent: microsoftSqlServer
  name: 'Default'
  properties: {
    state: 'Enabled'
    disabledAlerts: ['']
    emailAddresses: ['']
    emailAccountAdmins: false
    retentionDays: 0
  }
}

output sqlServerId string = microsoftSqlServer.id
output principalId string = microsoftSqlServer.identity.principalId
