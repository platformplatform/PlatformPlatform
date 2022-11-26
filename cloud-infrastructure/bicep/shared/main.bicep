param environment string
param location string = resourceGroup().location
param containerRegistryName string

var tags = { environment: environment, 'managed-by': 'bicep' }

module containerRegistry '../modules/container-registry.bicep' = {
  name: '${deployment().name}-Container-Registry'
  params: {
    name: containerRegistryName
    location: location
    tags: tags
  }
}
