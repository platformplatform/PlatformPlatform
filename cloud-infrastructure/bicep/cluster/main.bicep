targetScope = 'subscription'

param environment string
param locationPrefix string
param resourceGroupName string
param clusterUniqueName string
param location string = deployment().location

var tags = { environment: environment, 'managed-by': 'bicep' }
var activeDirectoryAdminObjectId = '33ff85b8-6b6f-4873-8e27-04ffc252c26c'
var diagnosticStorageAccountName = '${clusterUniqueName}diagnostic'

// Manually construct virtual network subnetId to avoid dependent Bicep resources to be ignored. See https://github.com/Azure/arm-template-whatif/issues/157#issuecomment-1336139303
var subnetId = '/subscriptions/${subscription().subscriptionId}/resourcegroups/${resourceGroupName}/providers/microsoft.network/virtualnetworks/${locationPrefix}-virtual-network/subnets/subnet'

// resource existingLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
//   scope: resourceGroup('${environment}-monitor')
//   name: '${environment}-log-analytics-workspace'
// }

resource clusterResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module diagnosticStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-diagnostic-storage-account'
  params: {
    location: location
    name: diagnosticStorageAccountName
    sku: 'Standard_GRS'
    tags: tags
  }
}

module networkWatcher '../modules/network-watcher.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-network-watcher'
  params: {
    location: location
    name: '${locationPrefix}-network-watcher'
    tags: tags
  }
}

module virtualNetwork '../modules/virtual-network.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-virtual-network'
  params: {
    location: location
    name: '${locationPrefix}-virtual-network'
    tags: tags
  }
  dependsOn: [ networkWatcher ]
}

module keyVault '../modules/key-vault.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-key-vault'
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
    tenantId: subscription().tenantId
    subnetId: subnetId
  }
  dependsOn: [ virtualNetwork ]
}

module serviceBus '../modules/service-bus.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-service-bus'
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
  }
}

module contaionerAppsEnvironment '../modules/container-apps-environment.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-container-apps-environment'
  params: {
    location: location
    name: '${locationPrefix}-container-apps-environment'
    tags: tags
    subnetId: subnetId
  }
  dependsOn: [ virtualNetwork ]
}

module microsoftSqlServer '../modules/microsoft-sql-server.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-microsoft-sql-server'
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
    subnetId: subnetId
    tenantId: subscription().tenantId
    sqlAdminObjectId: activeDirectoryAdminObjectId
  }
  dependsOn: [ virtualNetwork ]
}

module microsoftSqlDerverDiagnosticConfiguration '../modules/microsoft-sql-server-diagnostic.bicep' = {
  scope: clusterResourceGroup
  name: '${deployment().name}-microsoft-sql-server-diagnostic'
  params: {
    diagnosticStorageAccountName: diagnosticStorageAccountName
    microsoftSqlServerName: clusterUniqueName
    principalId: microsoftSqlServer.outputs.principalId
    dianosticStorageAccountBlobEndpoint: diagnosticStorageAccount.outputs.blobEndpoint
    dianosticStorageAccountSubscriptionId: subscription().subscriptionId
  }
}
