targetScope = 'subscription'

param environment string
param location string = deployment().location
param containerRegistryName string

var resourceGroupName = 'shared'
var tags = { environment: environment, 'managed-by': 'bicep' }

resource sharedResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module resourceGroupLock '../modules/resoruce-group-lock.bicep' = {
  name: '${deployment().name}-Resource-Group-Lock'
  scope: resourceGroup(sharedResourceGroup.name)
}

module containerRegistry '../modules/container-registry.bicep' = {
  name: '${deployment().name}-Container-Registry'
  scope: resourceGroup(sharedResourceGroup.name)
  params: {
    name: containerRegistryName
    location: location
    tags: tags
  }
}
