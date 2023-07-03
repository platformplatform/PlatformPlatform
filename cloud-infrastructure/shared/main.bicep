targetScope = 'subscription'

param environment string
param resourceGroupName string
param acrName string
param location string = deployment().location

var tags = { environment: environment, 'managed-by': 'bicep' }

resource sharedResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module containerRegistry '../modules/container-registry.bicep' = {
  name: '${deployment().name}-container-registry'
  scope: resourceGroup(sharedResourceGroup.name)
  params: {
    name: acrName
    location: location
    tags: tags
  }
}
