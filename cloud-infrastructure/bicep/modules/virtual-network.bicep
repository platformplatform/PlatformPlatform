
param name string
param location string
param tags object

resource virtualNetwork 'Microsoft.Network/virtualNetworks@2022-05-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    virtualNetworkPeerings: []
    enableDdosProtection: false
    addressSpace: {
      addressPrefixes: [
        '10.0.0.0/16'
      ]
    }
    dhcpOptions: {
      dnsServers: []
    }
    subnets: [
      {
        name: 'subnet'
        properties: {
          addressPrefix: '10.0.0.0/23'
          serviceEndpoints: []
          delegations: []
          privateEndpointNetworkPolicies: 'Enabled'
          privateLinkServiceNetworkPolicies: 'Enabled'
        }
        type: 'Microsoft.Network/virtualNetworks/subnets'
      }
    ]
  }
}
