targetScope = 'subscription'

param location string = deployment().location
param clusterResourceGroupName string
param globalResourceGroupName string
param environment string
param containerRegistryName string
param domainName string
param sqlAdminObjectId string
param appGatewayVersion string
param accountManagementVersion string
param backOfficeVersion string
param applicationInsightsConnectionString string
param communicationServicesDataLocation string = 'europe'
param mailSenderDisplayName string = 'PlatformPlatform'
param revisionSuffix string

var storageAccountUniquePrefix = replace(clusterResourceGroupName, '-', '')
var tags = { environment: environment, 'managed-by': 'bicep' }

resource clusterResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: clusterResourceGroupName
  location: location
  tags: tags
}

// Derive the resource name prefix by removing location suffix from cluster resource group name
// e.g., "pxp-stage-eu" -> "pxp-stage"
var resourceNamePrefix = substring(clusterResourceGroupName, 0, lastIndexOf(clusterResourceGroupName, '-'))

resource existingLogAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' existing = {
  scope: resourceGroup('${globalResourceGroupName}')
  name: resourceNamePrefix
}

var subnetId = resourceId(
  subscription().subscriptionId,
  clusterResourceGroupName,
  'Microsoft.Network/virtualNetworks/subnets',
  clusterResourceGroupName,
  'subnet'
)

var diagnosticStorageAccountName = '${storageAccountUniquePrefix}diagnostic'
module diagnosticStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-diagnostic-storage-account'
  params: {
    location: location
    name: diagnosticStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
  }
}

module virtualNetwork '../modules/virtual-network.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-virtual-network'
  params: {
    location: location
    name: clusterResourceGroupName
    tags: tags
    address: '10.0.0.0'
  }
}

module containerAppsEnvironment '../modules/container-apps-environment.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-container-apps-environment'
  params: {
    location: location
    name: clusterResourceGroupName
    tags: tags
    subnetId: subnetId
    globalResourceGroupName: globalResourceGroupName
    logAnalyticsWorkspaceName: resourceNamePrefix
  }
  dependsOn: [virtualNetwork]
}

module keyVault '../modules/key-vault.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-key-vault'
  params: {
    location: location
    name: clusterResourceGroupName
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
  name: '${clusterResourceGroupName}-communication-services'
  params: {
    name: clusterResourceGroupName
    tags: tags
    dataLocation: communicationServicesDataLocation
    mailSenderDisplayName: mailSenderDisplayName
    keyVaultName: keyVault.outputs.name
  }
}

module microsoftSqlServer '../modules/microsoft-sql-server.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-microsoft-sql-server'
  params: {
    location: location
    name: clusterResourceGroupName
    tags: tags
    subnetId: subnetId
    tenantId: subscription().tenantId
    sqlAdminObjectId: sqlAdminObjectId
  }
  dependsOn: [virtualNetwork]
}

module microsoftSqlDerverDiagnosticConfiguration '../modules/microsoft-sql-server-diagnostic.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-microsoft-sql-server-diagnostic'
  params: {
    diagnosticStorageAccountName: diagnosticStorageAccountName
    microsoftSqlServerName: clusterResourceGroupName
    dianosticStorageAccountBlobEndpoint: diagnosticStorageAccount.outputs.blobEndpoint
    dianosticStorageAccountSubscriptionId: subscription().subscriptionId
  }
  dependsOn: [microsoftSqlServer]
}

var isCustomDomainSet = domainName != ''
var publicUrl = isCustomDomainSet
  ? 'https://${domainName}'
  : 'https://${appGatewayContainerAppName}.${containerAppsEnvironment.outputs.defaultDomainName}'
var cdnUrl = publicUrl

// Account Management

var accountManagementIdentityName = '${clusterResourceGroupName}-account-management'
module accountManagementIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterResourceGroupName}-account-management-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: accountManagementIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    globalResourceGroupName: globalResourceGroupName
    keyVaultName: keyVault.outputs.name
    grantKeyVaultWritePermissions: true
  }
}

module accountManagementDatabase '../modules/microsoft-sql-database.bicep' = {
  name: '${clusterResourceGroupName}-account-management-sql-database'
  scope: clusterResourceGroup
  params: {
    sqlServerName: clusterResourceGroupName
    databaseName: 'account-management'
    location: location
    tags: tags
  }
  dependsOn: [microsoftSqlServer]
}

var accountManagementStorageAccountName = '${storageAccountUniquePrefix}acctmgmt'
module accountManagementStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-account-management-storage-account'
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
      {
        name: 'logos'
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
    value: '${cdnUrl}/account-management'
  }
  {
    name: 'SENDER_EMAIL_ADDRESS'
    value: 'no-reply@${communicationService.outputs.fromSenderDomain}'
  }
]

module accountManagementWorkers '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-account-management-workers-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'account-management-workers'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
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
    revisionSuffix: revisionSuffix
    environmentVariables: accountManagementEnvironmentVariables
  }
}

module accountManagementApi '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-account-management-api-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'account-management-api'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
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
    revisionSuffix: revisionSuffix
    environmentVariables: accountManagementEnvironmentVariables
  }
  dependsOn: [accountManagementWorkers]
}

// Back Office

var backOfficeIdentityName = '${clusterResourceGroupName}-back-office'
module backOfficeIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterResourceGroupName}-back-office-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: backOfficeIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    globalResourceGroupName: globalResourceGroupName
    keyVaultName: keyVault.outputs.name
  }
}

module backOfficeDatabase '../modules/microsoft-sql-database.bicep' = {
  name: '${clusterResourceGroupName}-back-office-sql-database'
  scope: clusterResourceGroup
  params: {
    sqlServerName: clusterResourceGroupName
    databaseName: 'back-office'
    location: location
    tags: tags
  }
  dependsOn: [microsoftSqlServer]
}

var backOfficeStorageAccountName = '${storageAccountUniquePrefix}backoffice'
module backOfficeStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-back-office-storage-account'
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
  name: '${clusterResourceGroupName}-back-office-workers-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'back-office-workers'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
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
    ingress: true
    hasProbesEndpoint: false
    revisionSuffix: revisionSuffix
    environmentVariables: backOfficeEnvironmentVariables
  }
}

module backOfficeApi '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-back-office-api-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'back-office-api'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
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
    hasProbesEndpoint: true
    revisionSuffix: revisionSuffix
    environmentVariables: backOfficeEnvironmentVariables
  }
  dependsOn: [backOfficeWorkers]
}

// App Gateway

var appGatewayIdentityName = '${clusterResourceGroupName}-app-gateway'
module appGatewayIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterResourceGroupName}-app-gateway-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: appGatewayIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    globalResourceGroupName: globalResourceGroupName
    keyVaultName: keyVault.outputs.name
  }
}

var appGatewayContainerAppName = 'app-gateway'
module appGateway '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-app-gateway-container-app'
  scope: clusterResourceGroup
  params: {
    name: appGatewayContainerAppName
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
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
    external: true
    revisionSuffix: revisionSuffix
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
        name: 'ACCOUNT_MANAGEMENT_STORAGE_URL'
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
}

module appGatewayAccountManagementStorageBlobDataReaderRoleAssignment '../modules/role-assignments-storage-blob-data-reader.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-app-gateway-account-management-blob-reader'
  params: {
    storageAccountName: accountManagementStorageAccountName
    userAssignedIdentityName: appGatewayIdentityName
  }
  dependsOn: [appGateway, accountManagementStorageAccount]
}

output accountManagementIdentityClientId string = accountManagementIdentity.outputs.clientId
output backOfficeIdentityClientId string = backOfficeIdentity.outputs.clientId
