param containerRegistryName string
param principalId string

resource containerRegistryResource 'Microsoft.ContainerRegistry/registries@2023-08-01-preview' existing = {
  name: containerRegistryName
}

var containerRegistryDataImporterDefinitionId = '577a9874-89fd-4f24-9dbd-b5034d0ad23a'
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(principalId)
  properties: {
    principalId: principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', containerRegistryDataImporterDefinitionId)
  }
  scope: containerRegistryResource
}
