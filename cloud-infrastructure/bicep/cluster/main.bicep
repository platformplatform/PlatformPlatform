targetScope = 'subscription'

param environment string
param location string = deployment().location
param locationPrefix string
param resourceGroupName string
param clusterUniqueName string

var tags = { environment: environment, 'managed-by': 'bicep' }

resource existingLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  scope: resourceGroup('${environment}-log-analytics-workspace')
  name: '${environment}-log-analytics-workspace'
}

resource clusterResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module networkWatcher '../modules/network-watcher.bicep' = {
  name: '${deployment().name}-network-watcher'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: '${locationPrefix}-network-watcher'
    tags: tags
  }
}

module virtualNetwork '../modules/virtual-network.bicep' = {
  name: '${deployment().name}-virtual-network'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: '${locationPrefix}-virtual-network'
    tags: tags
  }
  dependsOn: [ networkWatcher ]
}

module keyVault '../modules/key-vault.bicep' = {
  name: '${deployment().name}-key-vault'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
    tenantId: subscription().tenantId
    subnetId: virtualNetwork.outputs.subnetId
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

module serviceBus '../modules/service-bus.bicep' = {
  name: '${deployment().name}-service-bus'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
  }
}

module contaionerAppsEnvironment '../modules/container-apps-environment.bicep' = {
  name: '${deployment().name}-container-apps-environment'
  scope: resourceGroup(clusterResourceGroup.name)
  params: {
    location: location
    name: '${locationPrefix}-container-apps-environment'
    tags: tags
    subnetId: virtualNetwork.outputs.subnetId
    customerId: existingLogAnalyticsWorkspace.properties.customerId
  }
}
