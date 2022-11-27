
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
      subnet
    ]
  }
}

resource subnet 'Microsoft.Network/virtualNetworks/subnets@2022-05-01' = {
  name: 'west-europe-virtual-network/subnet'
  properties: {
    addressPrefix: '10.0.0.0/23'
    serviceEndpoints: [
      {
        service: 'Microsoft.KeyVault'
        locations: [
          '*'
        ]
      }
    ]
    delegations: []
    privateEndpointNetworkPolicies: 'Enabled'
    privateLinkServiceNetworkPolicies: 'Enabled'
  }
}

output virtualNetworkId string = virtualNetwork.id
output subnetId string = subnet.id
