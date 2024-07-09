targetScope = 'subscription'

param location string = deployment().location
param resourceGroupName string
param environmentResourceGroupName string
param environment string
param containerRegistryName string
param domainName string
param isDomainConfigured bool
param sqlAdminObjectId string
param appGatewayVersion string
param accountManagementVersion string
param applicationInsightsConnectionString string
param communicatoinServicesDataLocation string = 'europe'
param mailSenderDisplayName string = 'PlatformPlatform'

var storageAccountUniquePrefix = replace(resourceGroupName, '-', '')
var tags = { environment: environment, 'managed-by': 'bicep' }

resource clusterResourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

resource existingLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2022-10-01' existing = {
  scope: resourceGroup('${environmentResourceGroupName}')
  name: environmentResourceGroupName
}

var subnetId = resourceId(
  subscription().subscriptionId,
  resourceGroupName,
  'Microsoft.Network/virtualNetworks/subnets',
  resourceGroupName,
  'subnet'
)

var diagnosticStorageAccountName = '${storageAccountUniquePrefix}diagnostic'
module diagnosticStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-diagnostic-storage-account'
  params: {
    location: location
    name: diagnosticStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
  }
}

module virtualNetwork '../modules/virtual-network.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-virtual-network'
  params: {
    location: location
    name: resourceGroupName
    tags: tags
  }
}

module containerAppsEnvironment '../modules/container-apps-environment.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-container-apps-environment'
  params: {
    location: location
    name: resourceGroupName
    tags: tags
    subnetId: subnetId
    environmentResourceGroupName: environmentResourceGroupName
  }
  dependsOn: [virtualNetwork]
}

module keyVault '../modules/key-vault.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-key-vault'
  params: {
    location: location
    name: resourceGroupName
    tags: tags
    tenantId: subscription().tenantId
    subnetId: subnetId
    storageAccountId: diagnosticStorageAccount.outputs.storageAccountId
    workspaceId: existingLogAnalyticsWorkspace.id
  }
  dependsOn: [virtualNetwork]
}

module communicationService '../modules/communication-services.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-communication-services'
  params: {
    name: resourceGroupName
    tags: tags
    dataLocation: communicatoinServicesDataLocation
    mailSenderDisplayName: mailSenderDisplayName
    keyVaultName: keyVault.outputs.name
  }
}

module microsoftSqlServer '../modules/microsoft-sql-server.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-microsoft-sql-server'
  params: {
    location: location
    name: resourceGroupName
    tags: tags
    subnetId: subnetId
    tenantId: subscription().tenantId
    sqlAdminObjectId: sqlAdminObjectId
  }
  dependsOn: [virtualNetwork]
}

module microsoftSqlDerverDiagnosticConfiguration '../modules/microsoft-sql-server-diagnostic.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-microsoft-sql-server-diagnostic'
  params: {
    diagnosticStorageAccountName: diagnosticStorageAccountName
    microsoftSqlServerName: resourceGroupName
    principalId: microsoftSqlServer.outputs.principalId
    dianosticStorageAccountBlobEndpoint: diagnosticStorageAccount.outputs.blobEndpoint
    dianosticStorageAccountSubscriptionId: subscription().subscriptionId
  }
}

var isCustomDomainSet = domainName != ''
var publicUrl = isCustomDomainSet
  ? 'https://${domainName}'
  : 'https://${appGatewayContainerAppName}.${containerAppsEnvironment.outputs.defaultDomainName}'
var cdnUrl = publicUrl

// Account Management

var accountManagementIdentityName = '${resourceGroupName}-account-management'
module accountManagementIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${resourceGroupName}-account-management-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: accountManagementIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    environmentResourceGroupName: environmentResourceGroupName
    keyVaultName: keyVault.outputs.name
  }
}

module accountManagementDatabase '../modules/microsoft-sql-database.bicep' = {
  name: '${resourceGroupName}-account-management-sql-database'
  scope: clusterResourceGroup
  params: {
    sqlServerName: resourceGroupName
    databaseName: 'account-management'
    location: location
    tags: tags
  }
  dependsOn: [microsoftSqlServer]
}

var accountManagementStorageAccountName = '${storageAccountUniquePrefix}acctmgmt'
module accountManagementStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-account-management-storage-account'
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
  name: '${resourceGroupName}-account-management-workers-container-app'
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
    ingress: true
    hasProbesEndpoint: false
    environmentVariables: accountManagementEnvironmentVariables
  }
  dependsOn: [accountManagementDatabase, accountManagementIdentity, communicationService]
}

module accountManagementApi '../modules/container-app.bicep' = {
  name: '${resourceGroupName}-account-management-api-container-app'
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
    minReplicas: 0
    maxReplicas: 3
    userAssignedIdentityName: accountManagementIdentityName
    ingress: true
    hasProbesEndpoint: true
    environmentVariables: accountManagementEnvironmentVariables
  }
  dependsOn: [accountManagementDatabase, accountManagementIdentity, communicationService, accountManagementWorkers]
}

// App Gateway

var appGatewayIdentityName = '${resourceGroupName}-app-gateway'
module appGatewayIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${resourceGroupName}-app-gateway-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: appGatewayIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    environmentResourceGroupName: environmentResourceGroupName
    keyVaultName: keyVault.outputs.name
  }
}

var appGatewayContainerAppName = 'app-gateway'
module appGateway '../modules/container-app.bicep' = {
  name: '${resourceGroupName}-app-gateway-container-app'
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
    minReplicas: 0
    maxReplicas: 3
    userAssignedIdentityName: appGatewayIdentityName
    ingress: true
    hasProbesEndpoint: false
    domainName: domainName == '' ? '' : domainName
    isDomainConfigured: domainName != '' && isDomainConfigured
    external: true
    environmentVariables: [
      {
        name: 'AZURE_CLIENT_ID'
        value: '${appGatewayIdentity.outputs.clientId} ' // Hack, without this trailing space, Bicep --what-if will ignore all changes to Container App
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
  dependsOn: [appGatewayIdentity]
}

module appGatwayAccountManagementStorageBlobDataReaderRoleAssignment '../modules/role-assignments-storage-blob-data-reader.bicep' = {
  scope: clusterResourceGroup
  name: '${resourceGroupName}-app-gateway-account-management-blob-reader'
  params: {
    storageAccountName: accountManagementStorageAccountName
    userAssignedIdentityName: appGatewayIdentityName
  }
  dependsOn: [appGateway, accountManagementStorageAccount]
}

output accountManagementIdentityClientId string = accountManagementIdentity.outputs.clientId
