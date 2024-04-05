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

var storageBlobDataReaderRoleDefinitionId = '2a2b9908-6ea1-4ae2-8e65-a410df84e7d1'
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(existingStorageAccount.id, userAssignedIdentityName, storageBlobDataReaderRoleDefinitionId)
  properties: {
    principalId: userAssignedIdentityName == '' ? principalId : userAssignedIdentity.properties.principalId
    principalType: 'ServicePrincipal'
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', storageBlobDataReaderRoleDefinitionId)
  }
  scope: existingStorageAccount
}
