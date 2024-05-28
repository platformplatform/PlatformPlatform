param name string
param location string
param tags object
param subnetId string
param environmentResourceGroupName string


resource existingLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  scope: resourceGroup('${environmentResourceGroupName}')
  name: environmentResourceGroupName
}

var logAnalyticsCustomerId = existingLogAnalyticsWorkspace.properties.customerId
var logAnalyticsSharedKey = existingLogAnalyticsWorkspace.listKeys().primarySharedKey

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-02-preview' = {
  name: name
  location: location
  tags: tags
  properties: {
    vnetConfiguration: {
      internal: false
      infrastructureSubnetId: subnetId
      dockerBridgeCidr: '10.2.0.1/16'
      platformReservedCidr: '10.1.0.0/16'
      platformReservedDnsIP: '10.1.0.2'
    }
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalyticsCustomerId
        sharedKey: logAnalyticsSharedKey
      }
    }
    peerAuthentication: {
      mtls: {
        enabled: false
      }
    }
    zoneRedundant: true
  }
}

output environmentId string = containerAppsEnvironment.id
output name string = containerAppsEnvironment.name
output defaultDomainName string = containerAppsEnvironment.properties.defaultDomain
