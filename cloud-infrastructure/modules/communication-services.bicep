param name string
param tags object
param dataLocation string

resource communicationServices 'Microsoft.Communication/communicationServices@2023-06-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
