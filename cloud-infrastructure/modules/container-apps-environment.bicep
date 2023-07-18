param name string
param location string
param tags object
param subnetId string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-04-01-preview' = {
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
      destination: 'azure-monitor'
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
