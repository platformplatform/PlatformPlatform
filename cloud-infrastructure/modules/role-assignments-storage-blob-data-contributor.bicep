param storageAccountName string
param userAssignedIdentityName string = ''
param principalId string = ''

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = if(userAssignedIdentityName != '') {
  scope: resourceGroup()
  name: userAssignedIdentityName ?? principalId
}

resource existingStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  scope: resourceGroup()
  name: storageAccountName
}

var storageBlobDataContributorRoleDefinitionId = 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(existingStorageAccount.id, userAssignedIdentityName, storageBlobDataContributorRoleDefinitionId)
  properties: {
    principalId: userAssignedIdentityName == '' ? principalId : userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataContributorRoleDefinitionId)
  }
  scope: existingStorageAccount
}
