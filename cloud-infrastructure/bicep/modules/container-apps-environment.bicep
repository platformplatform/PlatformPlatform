param name string
param location string
param tags object
param subnetId string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2022-06-01-preview' = {
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
      outboundSettings: {
        outBoundType: 'LoadBalancer'
      }
    }
    appLogsConfiguration: {
      destination: 'azure-monitor'
    }
    zoneRedundant: true

    #disable-next-line BCP037 // useLegionServerlessCompute is not in the API, but without Bicep detects changes
    useLegionServerlessCompute: false
  }
  sku: {
    name: 'Consumption'
  }
}
