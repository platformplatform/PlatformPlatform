targetScope = 'subscription'

param environment string
param location string = deployment().location
param locationPrefix string
param resourceGroupName string
param clusterUniqueName string

var tags = { environment: environment, 'managed-by': 'bicep' }

resource clusterResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module virtualNetwork '../modules/virtual-network.bicep' = {
  name: '${deployment().name}-virtual-network'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: '${locationPrefix}-virtual-network'
    tags: tags
  }
}

module keyVault '../modules/key-vault.bicep' = {
  name: '${deployment().name}-key-vault'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
    virtualNetworkId: virtualNetwork.outputs.subnetId
  }
}

module storageAccount '../modules/storage-account.bicep' = {
  name: '${deployment().name}-diagnostic-storage-account'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: '${clusterUniqueName}diagnostic'
    sku: 'Standard_GRS'
    tags: tags
  }
}
