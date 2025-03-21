param name string
param location string
param tags object
param address string

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2024-05-01' = {
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
        name: 'subnet'
        properties: {
          addressPrefix: '${address}/23'
          serviceEndpoints: [
            {
              service: 'Microsoft.KeyVault'
            }
            {
              service: 'Microsoft.Sql'
            }
          ]
          delegations: []
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
      }
    ]
  }
}

output virtualNetworkName string = virtualNetwork.name
output virtualNetworkId string = virtualNetwork.id
output subnetId string = virtualNetwork.properties.subnets[0].id
