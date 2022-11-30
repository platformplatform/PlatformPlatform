param name string
param location string
param tags object
param subnetId string
param tenantId string
param sqlAdminObjectId string

resource microsoftSqlServer 'Microsoft.Sql/servers@2022-05-01-preview' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    version: '12.0'
    minimalTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    administrators: {
      administratorType: 'ActiveDirectory'
      principalType: 'Group'
      login: 'Active Directory Admin'
      sid: sqlAdminObjectId
      tenantId: tenantId
      azureADOnlyAuthentication: true
    }
    restrictOutboundNetworkAccess: 'Enabled'
  }
}

resource virtualNetworkRule 'Microsoft.Sql/servers/virtualNetworkRules@2022-05-01-preview' = {
  name: 'sql-virtual-network-rule'
  parent: microsoftSqlServer
  properties: {
    ignoreMissingVnetServiceEndpoint: true
    virtualNetworkSubnetId: subnetId
  }
}

resource microsoftSqlServerSecurityAlertPolicies 'Microsoft.Sql/servers/securityAlertPolicies@2022-05-01-preview' = {
  name: '${microsoftSqlServer.name}/Default'
  properties: {
    state: 'Enabled'
    disabledAlerts: []
    emailAddresses: []
    emailAccountAdmins: false
    retentionDays: 0
  }

}

output principalId string = microsoftSqlServer.identity.principalId
