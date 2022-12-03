param name string
param location string
param tags object
param storageAccountId string

resource serviceBus 'Microsoft.ServiceBus/namespaces@2022-01-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    minimumTlsVersion: '1.2'
    publicNetworkAccess: 'Enabled'
    disableLocalAuth: true
    zoneRedundant: false
  }
}

resource serviceBusAuditDiagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: serviceBus
  name: 'service-bus-audit'
  properties: {
    storageAccountId: storageAccountId
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
        retentionPolicy: {
          enabled: true
          days: 90
        }
      }
      {
        categoryGroup: 'allLogs'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}
