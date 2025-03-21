param name string
param location string
param tags object
param containerRegistryName string
param environmentResourceGroupName string
param keyVaultName string
param grantKeyVaultWritePermissions bool = false

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

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

var keyVaultSecretsUserRoleDefinitionId = '4633458b-17de-408a-b874-0445c86b69e6' // Key Vault Secrets User role
resource readKeyVaultSecretsRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, name, keyVaultSecretsUserRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultSecretsUserRoleDefinitionId
    )
    principalType: 'ServicePrincipal'
    principalId: userAssignedIdentity.properties.principalId
  }
}

var keyVaultCryptoServiceEncryptionUserRoleDefinitionId = 'e147488a-f6f5-4113-8e2d-b22465e65bf6' // Key Vault Crypto Service Encryption User 
resource readKeyVaultKeysRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVaultName, name, keyVaultCryptoServiceEncryptionUserRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultCryptoServiceEncryptionUserRoleDefinitionId
    )
    principalType: 'ServicePrincipal'
    principalId: userAssignedIdentity.properties.principalId
  }
}

var keyVaultCryptoOfficerRoleDefinitionId = '14b46e9e-c2b7-41b4-b07b-48a6ebf60603' // Key Vault Crypto Officer
resource signKeyVaultKeysRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = if (grantKeyVaultWritePermissions) {
  name: guid(keyVaultName, name, keyVaultCryptoOfficerRoleDefinitionId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId(
      'Microsoft.Authorization/roleDefinitions',
      keyVaultCryptoOfficerRoleDefinitionId
    )
    principalType: 'ServicePrincipal'
    principalId: userAssignedIdentity.properties.principalId
  }
}

output id string = userAssignedIdentity.id
output clientId string = userAssignedIdentity.properties.clientId
output principalId string = userAssignedIdentity.properties.principalId
