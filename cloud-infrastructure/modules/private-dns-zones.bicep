param privateDnsZones_privatelink_database_windows_net_name string = 'privatelink.database.windows.net'
param virtualNetworks_west_europe_virtual_network_externalid string = '/subscriptions/bed03e92-258a-4243-a9c1-92c80aab5417/resourceGroups/development-west-europe/providers/Microsoft.Network/virtualNetworks/west-europe-virtual-network'

resource privateDnsZone 'Microsoft.Network/privateDnsZones@2020-06-01' = {
  name: privateDnsZones_privatelink_database_windows_net_name
  location: 'global'
  properties: {
    maxNumberOfRecordSets: 25000
    maxNumberOfVirtualNetworkLinks: 1000
    maxNumberOfVirtualNetworkLinksWithRegistration: 100
    numberOfRecordSets: 2
    numberOfVirtualNetworkLinks: 1
    numberOfVirtualNetworkLinksWithRegistration: 0
    provisioningState: 'Succeeded'
  }
}

resource privateDnsZoneA 'Microsoft.Network/privateDnsZones/A@2018-09-01' = {
  name: '${privateDnsZones_privatelink_database_windows_net_name}/p14mdevweu'
  dependsOn: [privateDnsZone]
  properties: {
    ttl: 3600
    aRecords: [
      {
        ipv4Address: '10.0.0.91'
      }
    ]
  }
}

resource privateDnsZoneSOA 'Microsoft.Network/privateDnsZones/SOA@2018-09-01' = {
  name: '${privateDnsZones_privatelink_database_windows_net_name}/@'
  dependsOn: [privateDnsZone]
  properties: {
    ttl: 3600
    soaRecord: {
      email: 'azureprivatedns-host.microsoft.com'
      expireTime: 2419200
      host: 'azureprivatedns.net'
      minimumTtl: 10
      refreshTime: 3600
      retryTime: 300
      serialNumber: 1
    }
  }
}

resource privateDnsZoneVNetLink 'Microsoft.Network/privateDnsZones/virtualNetworkLinks@2018-09-01' = {
  name: '${privateDnsZones_privatelink_database_windows_net_name}/nl53a3t4satyu'
  location: 'global'
  dependsOn: [privateDnsZone]
  properties: {
    registrationEnabled: false
    virtualNetwork: {
      id: virtualNetworks_west_europe_virtual_network_externalid
    }
  }
}
