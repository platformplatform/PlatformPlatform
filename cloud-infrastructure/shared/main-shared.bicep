targetScope = 'subscription'

param environment string
param resourceGroupName string
param containerRegistryName string
param location string = deployment().location

var tags = { environment: environment, 'managed-by': 'bicep' }

resource sharedResourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module containerRegistry '../modules/container-registry.bicep' = {
  name: 'container-registry'
  scope: resourceGroup(sharedResourceGroup.name)
  params: {
    name: containerRegistryName
    location: location
    tags: tags
  }
}
