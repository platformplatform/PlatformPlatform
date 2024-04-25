targetScope = 'subscription'

param environment string
param locationPrefix string
param resourceGroupName string
param clusterUniqueName string
param useMssqlElasticPool bool
param containerRegistryName string
param location string = deployment().location
param sqlAdminObjectId string
param domainName string
param isDomainConfigured bool
param appGatewayVersion string
param accountManagementVersion string
param applicationInsightsConnectionString string
param communicatoinServicesDataLocation string = 'europe'
param mailSenderDisplayName string = 'PlatformPlatform'

var tags = { environment: environment, 'managed-by': 'bicep' }
var diagnosticStorageAccountName = '${clusterUniqueName}diagnostic'

resource clusterResourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

resource existingLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  scope: resourceGroup('${environment}')
  name: '${environment}-log-analytics-workspace'
}

// Manually construct virtual network subnetId to avoid dependent Bicep resources to be ignored. See https://github.com/Azure/arm-template-whatif/issues/157#issuecomment-1336139303
var virtualNetworkName = '${locationPrefix}-virtual-network'
var subnetId = resourceId(
  subscription().subscriptionId,
  resourceGroupName,
  'Microsoft.Network/virtualNetworks/subnets',
  virtualNetworkName,
  'subnet'
)

module diagnosticStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: 'diagnostic-storage-account'
  params: {
    location: location
    name: diagnosticStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
  }
}

module virtualNetwork '../modules/virtual-network.bicep' = {
  scope: clusterResourceGroup
  name: 'virtual-network'
  params: {
    location: location
    name: virtualNetworkName
    tags: tags
  }
}

module containerAppsEnvironment '../modules/container-apps-environment.bicep' = {
  scope: clusterResourceGroup
  name: 'container-apps-environment'
  params: {
    location: location
    name: '${locationPrefix}-container-apps-environment'
    tags: tags
    subnetId: subnetId
  }
  dependsOn: [virtualNetwork]
}

module microsoftSqlServer '../modules/microsoft-sql-server.bicep' = {
  scope: clusterResourceGroup
  name: 'microsoft-sql-server'
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
    subnetId: subnetId
    tenantId: subscription().tenantId
    sqlAdminObjectId: sqlAdminObjectId
  }
  dependsOn: [virtualNetwork]
}

module microsoftSqlDerverDiagnosticConfiguration '../modules/microsoft-sql-server-diagnostic.bicep' = {
  scope: clusterResourceGroup
  name: 'microsoft-sql-server-diagnostic'
  params: {
    diagnosticStorageAccountName: diagnosticStorageAccountName
    microsoftSqlServerName: clusterUniqueName
    principalId: microsoftSqlServer.outputs.principalId
    dianosticStorageAccountBlobEndpoint: diagnosticStorageAccount.outputs.blobEndpoint
    dianosticStorageAccountSubscriptionId: subscription().subscriptionId
  }
}

module microsoftSqlServerElasticPool '../modules/microsoft-sql-server-elastic-pool.bicep' =
  if (useMssqlElasticPool) {
    scope: clusterResourceGroup
    name: 'microsoft-sql-server-elastic-pool'
    params: {
      location: location
      name: '${locationPrefix}-microsoft-sql-server-elastic-pool'
      tags: tags
      sqlServerName: clusterUniqueName
      skuName: 'BasicPool'
      skuTier: 'Basic'
      skuCapacity: 50
      maxDatabaseCapacity: 5
    }
  }

module communicationService '../modules/communication-services.bicep' = {
  scope: clusterResourceGroup
  name: 'communication-services'
  params: {
    name: clusterUniqueName
    tags: tags
    dataLocation: communicatoinServicesDataLocation
    mailSenderDisplayName: mailSenderDisplayName
    keyVaultName: keyVault.outputs.name
  }
}

module keyVault '../modules/key-vault.bicep' = {
  scope: clusterResourceGroup
  name: 'key-vault'
  params: {
    location: location
    name: clusterUniqueName
    tags: tags
    tenantId: subscription().tenantId
    subnetId: subnetId
    storageAccountId: diagnosticStorageAccount.outputs.storageAccountId
    workspaceId: existingLogAnalyticsWorkspace.id
  }
  dependsOn: [virtualNetwork]
}

var accountManagementIdentityName = 'account-management-${resourceGroupName}'
module accountManagementIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: 'account-management-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: accountManagementIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    keyVaultName: keyVault.outputs.name
  }
}

module accountManagementDatabase '../modules/microsoft-sql-database.bicep' = {
  name: 'account-management-database'
  scope: clusterResourceGroup
  params: {
    sqlServerName: clusterUniqueName
    databaseName: 'account-management'
    location: location
    tags: tags
  }
  dependsOn: [microsoftSqlServer]
}

var accountManagementStorageAccountName = '${clusterUniqueName}acctmgmt'
module accountManagementStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: 'account-management-storage-account'
  params: {
    location: location
    name: accountManagementStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
    userAssignedIdentityName: accountManagementIdentityName
    containers: [
      {
        name: 'avatars'
        publicAccess: 'None'
      }
    ]
  }
  dependsOn: [accountManagementIdentity]
}

var isCustomDomainSet = domainName != ''
var publicUrl = isCustomDomainSet
  ? 'https://${domainName}'
  : 'https://${appGatewayContainerAppName}.${containerAppsEnvironment.outputs.defaultDomainName}'
var cdnUrl = publicUrl

var accountManagementEnvironmentVariables = [
  {
    name: 'AZURE_CLIENT_ID'
    value: '${accountManagementIdentity.outputs.clientId} ' // Hack, without this trailing space, Bicep --what-if will ignore all changes to Container App
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: applicationInsightsConnectionString
  }
  {
    name: 'DATABASE_CONNECTION_STRING'
    value: '${accountManagementDatabase.outputs.connectionString};User Id=${accountManagementIdentity.outputs.clientId};'
  }
  {
    name: 'KEYVAULT_URL'
    value: 'https://${keyVault.outputs.name}${az.environment().suffixes.keyvaultDns}'
  }
  {
    name: 'BLOB_STORAGE_URL'
    value: 'https://${accountManagementStorageAccountName}.blob.${az.environment().suffixes.storage}'
  }
  {
    name: 'PUBLIC_URL'
    value: publicUrl
  }
  {
    name: 'CDN_URL'
    value: cdnUrl
  }
  {
    name: 'SENDER_EMAIL_ADDRESS'
    value: 'no-reply@${communicationService.outputs.fromSenderDomain}'
  }
]

module accountManagementWorkers '../modules/container-app.bicep' = {
  name: 'account-management-workers-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'account-management-workers'
    location: location
    tags: tags
    resourceGroupName: resourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'account-management-workers'
    containerImageTag: accountManagementVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 3
    userAssignedIdentityName: accountManagementIdentityName
    ingress: false
    environmentVariables: accountManagementEnvironmentVariables
  }
  dependsOn: [accountManagementDatabase, accountManagementIdentity, communicationService]
}

module accountManagementApi '../modules/container-app.bicep' = {
  name: 'account-management-api-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'account-management-api'
    location: location
    tags: tags
    resourceGroupName: resourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'account-management-api'
    containerImageTag: accountManagementVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 1
    maxReplicas: 3
    userAssignedIdentityName: accountManagementIdentityName
    ingress: true
    environmentVariables: accountManagementEnvironmentVariables
  }
  dependsOn: [accountManagementDatabase, accountManagementIdentity, communicationService, accountManagementWorkers]
}

var appGatewayIdentityName = 'app-gateway-${resourceGroupName}'
module mainAppIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: 'app-gateway-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: appGatewayIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    keyVaultName: keyVault.outputs.name
  }
}

var appGatewayContainerAppName = 'app-gateway'
module appGateway '../modules/container-app.bicep' = {
  name: 'app-gateway-container-app'
  scope: clusterResourceGroup
  params: {
    name: appGatewayContainerAppName
    location: location
    tags: tags
    resourceGroupName: resourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'app-gateway'
    containerImageTag: appGatewayVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 1
    maxReplicas: 3
    userAssignedIdentityName: appGatewayIdentityName
    ingress: true
    domainName: domainName == '' ? '' : domainName
    isDomainConfigured: domainName != '' && isDomainConfigured
    external: true
    environmentVariables: [
      {
        name: 'AZURE_CLIENT_ID'
        value: '${mainAppIdentity.outputs.clientId} ' // Hack, without this trailing space, Bicep --what-if will ignore all changes to Container App
      }
      {
        name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
        value: applicationInsightsConnectionString
      }
      {
        name: 'KEYVAULT_URL'
        value: 'https://${keyVault.outputs.name}${az.environment().suffixes.keyvaultDns}'
      }
      {
        name: 'AVATARS_STORAGE_URL'
        value: 'https://${accountManagementStorageAccountName}.blob.${az.environment().suffixes.storage}'
      }
      {
        name: 'ACCOUNT_MANAGEMENT_API_URL'
        value: 'https://account-management-api.internal.${containerAppsEnvironment.outputs.defaultDomainName}'
      }
    ]
  }
  dependsOn: [mainAppIdentity]
}

module appGatwayAccountManagementStorageBlobDataReaderRoleAssignment '../modules/role-assignments-storage-blob-data-reader.bicep' = {
  scope: clusterResourceGroup
  name: '${appGatewayContainerAppName}-blob-reader-role-assignment'
  params: {
    storageAccountName: accountManagementStorageAccountName
    userAssignedIdentityName: appGatewayIdentityName
  }
  dependsOn: [ appGateway, accountManagementStorageAccount ]
}

output accountManagementIdentityClientId string = accountManagementIdentity.outputs.clientId
