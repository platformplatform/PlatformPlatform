param name string
param location string
param tags object
param address string

var addressPrefix = split(address, '.')[0]
var privateEndpointSubnet = '${addressPrefix}.0.2.0/24'

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2025-01-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    virtualNetworkPeerings: []
    enableDdosProtection: false
    addressSpace: {
      addressPrefixes: ['${address}/16']
    }
    dhcpOptions: {
      dnsServers: []
    }
    subnets: [
      {
        name: 'container-apps'
        properties: {
          addressPrefix: '${address}/23'
          serviceEndpoints: [
            {
              service: 'Microsoft.KeyVault'
            }
          ]
          delegations: [
            {
              name: 'Microsoft.App.environments'
              properties: {
                serviceName: 'Microsoft.App/environments'
              }
            }
          ]
        }
      }
      {
        name: 'private-endpoints'
        properties: {
          addressPrefix: privateEndpointSubnet
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
}

output virtualNetworkName string = virtualNetwork.name
output virtualNetworkId string = virtualNetwork.id
output containerAppsSubnetId string = virtualNetwork.properties.subnets[0].id
output privateEndpointSubnetId string = virtualNetwork.properties.subnets[1].id
