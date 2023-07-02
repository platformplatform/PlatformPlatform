param acrName string
param identityPrincipalId string

resource acrResource 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' existing = {
  name: acrName
}

var acrPullDefinitionId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: acrPullDefinitionId
  properties: {
    principalId: identityPrincipalId  
    principalType: 'ServicePrincipal'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', acrPullDefinitionId)
  }
  scope: acrResource
}

output loginServer string = acrResource.properties.loginServer
