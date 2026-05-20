targetScope = 'subscription'

param location string = deployment().location
param clusterResourceGroupName string
param globalResourceGroupName string
param environment string
param containerRegistryName string
param domainName string
param backOfficeDomainName string = ''
param backOfficeEntraClientId string
param backOfficeAdminsGroupId string = ''
param appGatewayVersion string
param accountVersion string
param mainVersion string
param applicationInsightsConnectionString string
param communicationServicesDataLocation string = 'europe'
@minLength(1)
param productName string
@minLength(1)
param mailSenderDisplayName string = productName
param useCustomEmailDomain bool = false
param revisionSuffix string

@description('Object ID of the Entra ID security group for PostgreSQL administration')
param postgresAdminObjectId string = ''

@secure()
param googleOAuthClientId string
@secure()
param googleOAuthClientSecret string

@secure()
param stripePublishableKey string
@secure()
param stripeApiKey string
@secure()
param stripeWebhookSecret string

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

module diagnosticStorageRetention '../modules/storage-account-retention.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-diagnostic-storage-retention'
  params: {
    storageAccountName: diagnosticStorageAccount.outputs.name
    retentionDays: 90
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
    subnetId: virtualNetwork.outputs.containerAppsSubnetId
    globalResourceGroupName: globalResourceGroupName
    logAnalyticsWorkspaceName: resourceNamePrefix
  }
}

module keyVault '../modules/key-vault.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-key-vault'
  params: {
    location: location
    name: clusterResourceGroupName
    tags: tags
    tenantId: subscription().tenantId
    subnetId: virtualNetwork.outputs.containerAppsSubnetId
    storageAccountId: diagnosticStorageAccount.outputs.storageAccountId
    workspaceId: existingLogAnalyticsWorkspace.id
    domainName: domainName
    productName: productName
  }
}

module googleOAuthSecrets '../modules/key-vault-secrets.bicep' = if (!empty(googleOAuthClientId) && !empty(googleOAuthClientSecret)) {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-google-oauth-secrets'
  params: {
    keyVaultName: keyVault.outputs.name
    secrets: {
      'OAuth--Google--ClientId': googleOAuthClientId
      'OAuth--Google--ClientSecret': googleOAuthClientSecret
    }
  }
}

module stripeSecrets '../modules/key-vault-secrets.bicep' = if (!empty(stripeApiKey) && !empty(stripeWebhookSecret) && !empty(stripePublishableKey)) {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-stripe-secrets'
  params: {
    keyVaultName: keyVault.outputs.name
    secrets: {
      'Stripe--ApiKey': stripeApiKey
      'Stripe--WebhookSecret': stripeWebhookSecret
      'Stripe--PublishableKey': stripePublishableKey
    }
  }
}

// Derive the email custom domain as the eTLD+1 (apex) of the cluster's ingress domainName. Cluster
// ingress is typically a CNAME, and DNS rules forbid TXT/other records at the same name as a CNAME
// (RFC 1034). The apex of a domain cannot itself be a CNAME, so SPF (TXT) and DKIM CNAMEs at
// sub-subdomains of the apex can coexist freely with whatever else lives on the apex.
// Apple Mail OTP autofill matches on eTLD+1, so a sender at the apex still autofills on any subdomain
// of the same apex (e.g., sender no-reply@example.com autofills forms on
// staging.example.com or app.example.com).
// The "last two parts" derivation is correct for single-suffix TLDs (.net, .com, .io). It is wrong
// for multi-part public suffixes like .co.uk - replace with an explicit param if that ever applies.
var domainNameParts = split(domainName, '.')
var emailDomainName = empty(domainName)
  ? ''
  : '${domainNameParts[length(domainNameParts) - 2]}.${domainNameParts[length(domainNameParts) - 1]}'

module communicationService '../modules/communication-services.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-communication-services'
  params: {
    name: clusterResourceGroupName
    tags: tags
    dataLocation: communicationServicesDataLocation
    mailSenderDisplayName: mailSenderDisplayName
    keyVaultName: keyVault.outputs.name
    emailDomainName: emailDomainName
    useCustomEmailDomain: useCustomEmailDomain
  }
}

module postgresServer '../modules/postgresql-flexible-server.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-postgresql-server'
  params: {
    location: location
    name: clusterResourceGroupName
    tags: tags
    tenantId: subscription().tenantId
    subnetId: virtualNetwork.outputs.privateEndpointSubnetId
    virtualNetworkId: virtualNetwork.outputs.virtualNetworkId
    isProduction: environment == 'prod'
    diagnosticStorageAccountId: diagnosticStorageAccount.outputs.storageAccountId
    dbAdminObjectId: postgresAdminObjectId
  }
}

var isCustomDomainSet = domainName != ''
var publicUrl = isCustomDomainSet
  ? 'https://${domainName}'
  : 'https://${appGatewayContainerAppName}.${containerAppsEnvironment.outputs.defaultDomainName}'
var cdnUrl = publicUrl

// Back-office is reachable on its custom domain when set, else on the auto-generated ACA FQDN. Both code
// paths must agree on the same hostname for Easy Auth redirect URLs and for the application's host-aware
// routing (HostScopedSinglePageApp, BackOffice__Host, Hostnames__BackOffice).
var backOfficeHost = backOfficeDomainName != ''
  ? backOfficeDomainName
  : 'back-office.${containerAppsEnvironment.outputs.defaultDomainName}'

// Account

var accountIdentityName = '${clusterResourceGroupName}-account'
module accountIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterResourceGroupName}-account-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: accountIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    globalResourceGroupName: globalResourceGroupName
    keyVaultName: keyVault.outputs.name
    grantKeyVaultWritePermissions: true
  }
}

module accountDatabase '../modules/postgresql-flexible-database.bicep' = {
  name: '${clusterResourceGroupName}-account-postgres-database'
  scope: clusterResourceGroup
  params: {
    serverName: postgresServer.outputs.serverName
    databaseName: 'account'
  }
}

var accountStorageAccountName = '${storageAccountUniquePrefix}account'
module accountStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-account-storage-account'
  params: {
    location: location
    name: accountStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
    userAssignedIdentityName: accountIdentity.outputs.name
    containers: [
      {
        name: 'avatars'
        publicAccess: 'None'
      }
      {
        name: 'logos'
        publicAccess: 'None'
      }
      {
        name: 'support-tickets'
        publicAccess: 'None'
      }
      {
        name: 'support-staff'
        publicAccess: 'None'
      }
    ]
  }
}

var accountEnvironmentVariables = [
  {
    name: 'AZURE_CLIENT_ID'
    value: '${accountIdentity.outputs.clientId} ' // Hack, without this trailing space, Bicep --what-if will ignore all changes to Container App
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: applicationInsightsConnectionString
  }
  {
    name: 'DATABASE_CONNECTION_STRING'
    value: '${accountDatabase.outputs.connectionString};Username=${accountIdentityName}'
  }
  {
    name: 'KEYVAULT_URL'
    value: 'https://${keyVault.outputs.name}${az.environment().suffixes.keyvaultDns}'
  }
  {
    name: 'BLOB_STORAGE_URL'
    value: 'https://${accountStorageAccountName}.blob.${az.environment().suffixes.storage}'
  }
  {
    name: 'PUBLIC_URL'
    value: publicUrl
  }
  {
    name: 'CDN_URL'
    value: '${cdnUrl}/account'
  }
  {
    name: 'SENDER_EMAIL_ADDRESS'
    value: 'no-reply@${communicationService.outputs.fromSenderDomain}'
  }
  {
    name: 'Stripe__SubscriptionEnabled'
    value: !empty(stripeApiKey) && !empty(stripeWebhookSecret) && !empty(stripePublishableKey) ? 'true' : 'false'
  }
  {
    name: 'Stripe__AllowMockProvider'
    value: 'false'
  }
  {
    name: 'Hostnames__App'
    value: domainName
  }
  {
    name: 'BackOffice__Host'
    value: backOfficeHost
  }
  {
    name: 'BackOffice__AdminsGroupId'
    value: backOfficeAdminsGroupId
  }
  {
    name: 'PUBLIC_GOOGLE_OAUTH_ENABLED'
    value: !empty(googleOAuthClientId) && !empty(googleOAuthClientSecret) ? 'true' : 'false'
  }
  {
    name: 'PUBLIC_SUBSCRIPTION_ENABLED'
    value: !empty(stripeApiKey) && !empty(stripeWebhookSecret) && !empty(stripePublishableKey) ? 'true' : 'false'
  }
]

module accountWorkers '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-account-workers-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'account-workers'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'account-workers'
    containerImageTag: accountVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 3
    userAssignedIdentityName: accountIdentityName
    ingress: true
    hasProbesEndpoint: false
    revisionSuffix: revisionSuffix
    environmentVariables: accountEnvironmentVariables
  }
}

module accountApi '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-account-api-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'account-api'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'account-api'
    containerImageTag: accountVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 3
    userAssignedIdentityName: accountIdentityName
    ingress: true
    hasProbesEndpoint: true
    external: false
    revisionSuffix: revisionSuffix
    environmentVariables: accountEnvironmentVariables
  }
  dependsOn: [accountWorkers]
}

// Back-office runs the same image as account-api on a separate external container app. Easy Auth is bound here
// only (RedirectToLoginPage), so account-api can stay internal-only and reachable solely through AppGateway.
module backOffice '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-back-office-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'back-office'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'account-api'
    containerImageTag: accountVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 1
    userAssignedIdentityName: accountIdentityName
    ingress: true
    hasProbesEndpoint: true
    additionalDomainName: backOfficeDomainName
    external: true
    revisionSuffix: revisionSuffix
    // The back-office container runs the same image as account-api; this flag tells Program.cs to register the BackOffice SPA fallback instead of the user-facing one.
    environmentVariables: concat(accountEnvironmentVariables, [
      {
        name: 'BackOffice__IsBackOfficeContainer'
        value: 'true'
      }
    ])
  }
  dependsOn: [accountApi]
}

module backOfficeAuthConfig '../modules/container-app-auth-config.bicep' = {
  name: '${clusterResourceGroupName}-back-office-auth-config'
  scope: clusterResourceGroup
  params: {
    containerAppName: 'back-office'
    tenantId: subscription().tenantId
    clientId: backOfficeEntraClientId
    allowedExternalRedirectUrls: [
      'https://${backOfficeHost}/.auth/login/aad/callback'
    ]
  }
  dependsOn: [backOffice]
}

// Main

var mainIdentityName = '${clusterResourceGroupName}-main'
module mainIdentity '../modules/user-assigned-managed-identity.bicep' = {
  name: '${clusterResourceGroupName}-main-managed-identity'
  scope: clusterResourceGroup
  params: {
    name: mainIdentityName
    location: location
    tags: tags
    containerRegistryName: containerRegistryName
    globalResourceGroupName: globalResourceGroupName
    keyVaultName: keyVault.outputs.name
  }
}

module mainDatabase '../modules/postgresql-flexible-database.bicep' = {
  name: '${clusterResourceGroupName}-main-postgres-database'
  scope: clusterResourceGroup
  params: {
    serverName: postgresServer.outputs.serverName
    databaseName: 'main'
  }
}

var mainStorageAccountName = '${storageAccountUniquePrefix}main'
module mainStorageAccount '../modules/storage-account.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-main-storage-account'
  params: {
    location: location
    name: mainStorageAccountName
    tags: tags
    sku: 'Standard_GRS'
    userAssignedIdentityName: mainIdentity.outputs.name
  }
}

var mainEnvironmentVariables = [
  {
    name: 'AZURE_CLIENT_ID'
    value: '${mainIdentity.outputs.clientId} ' // Hack, without this trailing space, Bicep --what-if will ignore all changes to Container App
  }
  {
    name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
    value: applicationInsightsConnectionString
  }
  {
    name: 'DATABASE_CONNECTION_STRING'
    value: '${mainDatabase.outputs.connectionString};Username=${mainIdentityName}'
  }
  {
    name: 'KEYVAULT_URL'
    value: 'https://${keyVault.outputs.name}${az.environment().suffixes.keyvaultDns}'
  }
  {
    name: 'BLOB_STORAGE_URL'
    value: 'https://${mainStorageAccountName}.blob.${az.environment().suffixes.storage}'
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
  {
    name: 'PUBLIC_GOOGLE_OAUTH_ENABLED'
    value: !empty(googleOAuthClientId) && !empty(googleOAuthClientSecret) ? 'true' : 'false'
  }
  {
    name: 'PUBLIC_SUBSCRIPTION_ENABLED'
    value: !empty(stripeApiKey) && !empty(stripeWebhookSecret) && !empty(stripePublishableKey) ? 'true' : 'false'
  }
]

module mainWorkers '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-main-workers-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'main-workers'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'main-workers'
    containerImageTag: mainVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 1
    userAssignedIdentityName: mainIdentityName
    ingress: true
    hasProbesEndpoint: false
    revisionSuffix: revisionSuffix
    environmentVariables: mainEnvironmentVariables
  }
}

module mainApi '../modules/container-app.bicep' = {
  name: '${clusterResourceGroupName}-main-api-container-app'
  scope: clusterResourceGroup
  params: {
    name: 'main-api'
    location: location
    tags: tags
    clusterResourceGroupName: clusterResourceGroupName
    containerAppsEnvironmentId: containerAppsEnvironment.outputs.environmentId
    containerAppsEnvironmentName: containerAppsEnvironment.outputs.name
    containerRegistryName: containerRegistryName
    containerImageName: 'main-api'
    containerImageTag: mainVersion
    cpu: '0.25'
    memory: '0.5Gi'
    minReplicas: 0
    maxReplicas: 1
    userAssignedIdentityName: mainIdentityName
    ingress: true
    hasProbesEndpoint: true
    revisionSuffix: revisionSuffix
    environmentVariables: mainEnvironmentVariables
  }
  dependsOn: [mainWorkers]
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
        name: 'ACCOUNT_STORAGE_URL'
        value: 'https://${accountStorageAccountName}.blob.${az.environment().suffixes.storage}'
      }
      {
        name: 'ACCOUNT_API_URL'
        value: 'https://account-api.internal.${containerAppsEnvironment.outputs.defaultDomainName}'
      }
      {
        name: 'MAIN_API_URL'
        value: 'https://main-api.internal.${containerAppsEnvironment.outputs.defaultDomainName}'
      }
      {
        name: 'Hostnames__App'
        value: domainName
      }
    ]
  }
}

module appGatewayAccountStorageBlobDataReaderRoleAssignment '../modules/role-assignments-storage-blob-data-reader.bicep' = {
  scope: clusterResourceGroup
  name: '${clusterResourceGroupName}-app-gateway-account-blob-reader'
  params: {
    storageAccountName: accountStorageAccount.outputs.name
    userAssignedIdentityName: appGatewayIdentity.outputs.name
  }
}

output accountIdentityClientId string = accountIdentity.outputs.clientId
output mainIdentityClientId string = mainIdentity.outputs.clientId
