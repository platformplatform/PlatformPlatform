param name string
param location string
param tags object
param containerRegistryName string
param environmentResourceGroupName string
param keyVaultName string

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: name
  location: location
  tags: tags
}

module containerRegistryPermission './role-assignments-container-registry-acr-pull.bicep' = {
  name: '${name}-permission'
  scope: resourceGroup(subscription().subscriptionId, environmentResourceGroupName)
  params: {
    containerRegistryName: containerRegistryName
    principalId: userAssignedIdentity.properties.principalId
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' existing = {
  name: keyVaultName
}

var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User role
resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, name, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', keyVaultSecretsUserRoleDefinitionId)
    principalType: 'ServicePrincipal'
    principalId: userAssignedIdentity.properties.principalId
  }
}

output id string = userAssignedIdentity.id
output clientId string = userAssignedIdentity.properties.clientId
output principalId string = userAssignedIdentity.properties.principalId
