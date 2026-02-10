param keyVaultName string

@secure()
param secrets object

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource keyVaultSecrets 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = [
  for secret in items(secrets): {
    parent: keyVault
    name: secret.key
    properties: {
      value: secret.value
    }
  }
]
