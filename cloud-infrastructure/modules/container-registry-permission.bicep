param containerRegistryName string
param identityPrincipalId string

resource containerRegistryResource 'Microsoft.ContainerRegistry/registries@2023-08-01-preview' existing = {
  name: containerRegistryName
}

var containerRegistryPullDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(identityPrincipalId)
  properties: {
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', containerRegistryPullDefinitionId)
  }
  scope: containerRegistryResource
}

output loginServer string = containerRegistryResource.properties.loginServer
