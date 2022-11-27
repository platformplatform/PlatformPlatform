param name string
param location string
param tags object
param virtualNetworkId string

resource mentumtesteuw 'Microsoft.KeyVault/vaults@2022-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: '8ddc3e17-1f68-4773-98b6-ea1d90659983'
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: [
        {
          id: virtualNetworkId
        }
      ]
    }
    accessPolicies: []
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: false
    provisioningState: 'Succeeded'
    publicNetworkAccess: 'Enabled'
  }
}
