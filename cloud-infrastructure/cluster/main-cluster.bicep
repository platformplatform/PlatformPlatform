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
param backOfficeVersion string
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
  name: '${clusterUniqueName}-diagnostic-storage-account'
  params: {
    location: location
    name: diagnosticStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
  }
}

module virtualNetwork '../modules/virtual-network.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterUniqueName}-virtual-network'
  params: {
    location: location
    name: virtualNetworkName
    tags: tags
  }
}

module containerAppsEnvironment '../modules/container-apps-environment.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterUniqueName}-container-apps-environment'
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
  name: '${clusterUniqueName}-microsoft-sql-server'
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
  name: '${clusterUniqueName}-microsoft-sql-server-diagnostic'
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
    name: '${clusterUniqueName}-microsoft-sql-server-elastic-pool'
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
  name: '${clusterUniqueName}-communication-services'
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
  name: '${clusterUniqueName}-key-vault'
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

var isCustomDomainSet = domainName != ''
var publicUrl = isCustomDomainSet
  ? 'https://${domainName}'
  : 'https://${appGatewayContainerAppName}.${containerAppsEnvironment.outputs.defaultDomainName}'
var cdnUrl = publicUrl

// Account Management

var accountManagementIdentityName = 'account-management-${resourceGroupName}'
module accountManagementIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterUniqueName}-account-management-managed-identity'
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
  name: '${clusterUniqueName}-account-management-database'
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
  name: '${clusterUniqueName}-account-management-storage-account'
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
  name: '${clusterUniqueName}-account-management-workers-container-app'
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
  name: '${clusterUniqueName}-account-management-api-container-app'
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


// Back Office

var backOfficeIdentityName = 'back-office-${resourceGroupName}'
module backOfficeIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterUniqueName}-back-office-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: backOfficeIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    keyVaultName: keyVault.outputs.name
  }
}

module backOfficeDatabase '../modules/microsoft-sql-database.bicep' = {
  name: '${clusterUniqueName}-back-office-database'
  scope: clusterResourceGroup
  params: {
    sqlServerName: clusterUniqueName
    databaseName: 'back-office'
    location: location
    tags: tags
  }
  dependsOn: [microsoftSqlServer]
}

var backOfficeStorageAccountName = '${clusterUniqueName}backoffice'
module backOfficeStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterUniqueName}-back-office-storage-account'
  params: {
    location: location
    name: backOfficeStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
    userAssignedIdentityName: backOfficeIdentityName
  }
  dependsOn: [backOfficeIdentity]
}

var backOfficeEnvironmentVariables = [
  {
    name: 'AZURE_CLIENT_ID'
    value: '${backOfficeIdentity.outputs.clientId} ' // Hack, without this trailing space, Bicep --what-if will ignore all changes to Container App
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: applicationInsightsConnectionString
  }
  {
    name: 'DATABASE_CONNECTION_STRING'
    value: '${backOfficeDatabase.outputs.connectionString};User Id=${backOfficeIdentity.outputs.clientId};'
  }
  {
    name: 'KEYVAULT_URL'
    value: 'https://${keyVault.outputs.name}${az.environment().suffixes.keyvaultDns}'
  }
  {
    name: 'BLOB_STORAGE_URL'
    value: 'https://${backOfficeStorageAccountName}.blob.${az.environment().suffixes.storage}'
  }
  {
    name: 'PUBLIC_URL'
    value: publicUrl
  }
  {
    name: 'CDN_URL'
    value: '${cdnUrl}/back-office'
  }
  {
    name: 'SENDER_EMAIL_ADDRESS'
    value: 'no-reply@${communicationService.outputs.fromSenderDomain}'
  }
]

module backOfficeWorkers '../modules/container-app.bicep' = {
  name: '${clusterUniqueName}-back-office-workers-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'back-office-workers'
    location: location
    tags: tags
    resourceGroupName: resourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'back-office-workers'
    containerImageTag: backOfficeVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 1
    userAssignedIdentityName: backOfficeIdentityName
    ingress: false
    environmentVariables: backOfficeEnvironmentVariables
  }
  dependsOn: [backOfficeDatabase, backOfficeIdentity, communicationService]
}

module backOfficeApi '../modules/container-app.bicep' = {
  name: '${clusterUniqueName}-back-office-api-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'back-office-api'
    location: location
    tags: tags
    resourceGroupName: resourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'back-office-api'
    containerImageTag: backOfficeVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 1
    userAssignedIdentityName: backOfficeIdentityName
    ingress: true
    environmentVariables: backOfficeEnvironmentVariables
  }
  dependsOn: [backOfficeDatabase, backOfficeIdentity, communicationService, backOfficeWorkers]
}

// App Gateway

var appGatewayIdentityName = 'app-gateway-${resourceGroupName}'
module mainAppIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterUniqueName}-app-gateway-managed-identity'
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
  name: '${clusterUniqueName}-app-gateway-container-app'
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
      {
        name: 'BACK_OFFICE_API_URL'
        value: 'https://back-office-api.internal.${containerAppsEnvironment.outputs.defaultDomainName}'
      }
    ]
  }
  dependsOn: [mainAppIdentity]
}

module appGatwayAccountManagementStorageBlobDataReaderRoleAssignment '../modules/role-assignments-storage-blob-data-reader.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterUniqueName}-${appGatewayContainerAppName}-blob-reader'
  params: {
    storageAccountName: accountManagementStorageAccountName
    userAssignedIdentityName: appGatewayIdentityName
  }
  dependsOn: [ appGateway, accountManagementStorageAccount ]
}

output accountManagementIdentityClientId string = accountManagementIdentity.outputs.clientId
output backOfficeIdentityClientId string = backOfficeIdentity.outputs.clientId
