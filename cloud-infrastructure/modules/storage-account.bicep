param name string
param location string
param tags object
param sku string
param userAssignedIdentityName string = ''
@description('Array of containers to be created')
param containers array = [
  {
    name: 'default'
    publicAccess: 'None'
  }
]

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: name
  location: location
  tags: tags
  kind: 'StorageV2'
  sku: {
    name: sku
  }
  identity: {
    type: 'None'
  }
  properties: {
    defaultToOAuthAuthentication: true
    publicNetworkAccess: 'Enabled'
    allowCrossTenantReplication: false
    isNfsV3Enabled: false
    minimumTlsVersion: 'TLS1_2'
    allowBlobPublicAccess: false
    allowSharedKeyAccess: true
    isHnsEnabled: false
    networkAcls: {
      bypass: 'AzureServices'
      virtualNetworkRules: []
      ipRules: []
      defaultAction: 'Allow'
    }
    supportsHttpsTrafficOnly: true
    encryption: {
      requireInfrastructureEncryption: true
      keySource: 'Microsoft.Storage'
    }
    accessTier: 'Hot'
  }
}

resource blobContainers 'Microsoft.Storage/storageAccounts/blobServices/containers@2021-04-01' = [for container in containers: {
  name: '${name}/default/${container.name}'
  properties: {
    publicAccess: container.publicAccess
  }
  dependsOn: [storageAccount]
}]

module storageBlobDataContributorRoleAssignment 'role-assignments-storage-blob-data-contributor.bicep' = if (userAssignedIdentityName != '') {
  name: '${name}-blob-contributer-role-assignment'
  params: {
    storageAccountName: name
    userAssignedIdentityName: userAssignedIdentityName
  }
  dependsOn: [ storageAccount ]
}

output blobEndpoint string = storageAccount.properties.primaryEndpoints.blob
output storageAccountId string = storageAccount.id
