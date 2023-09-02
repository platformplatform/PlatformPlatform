param name string
param location string
param tags object
param subnetId string
param sqlServerId string

resource privatelink 'Microsoft.Network/privateEndpoints@2023-04-01' = {
  name: name
  location: location
  tags: tags

  properties: {
    subnet: {
      id: subnetId
    }
    privateLinkServiceConnections: [
      {
        name: 'sql-server'
        properties: {
          privateLinkServiceId: sqlServerId
          groupIds: ['SqlServer']
        }
      }
    ]
  }
}
