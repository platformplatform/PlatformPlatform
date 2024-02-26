param name string
param location string
param tags object
param resourceGroupName string
param containerAppsEnvironmentId string
param containerAppsEnvironmentName string
param containerRegistryName string
param containerImageName string
param containerImageTag string
param cpu string = '0.25'
param memory string = '0.5Gi'
param minReplicas int = 1
param maxReplicas int = 3
param sqlServerName string
param sqlDatabaseName string
param userAssignedIdentityName string
param domainName string
param domainConfigured bool
param applicationInsightsConnectionString string
param keyVaultName string

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' existing = {
  scope: resourceGroup(resourceGroupName)
  name: userAssignedIdentityName
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' existing = {
  name: keyVaultName
}

var containerRegistryResourceGroupName = 'shared'
module containerRegistryPermission './container-registry-permission.bicep' = {
  name: 'container-registry-permission'
  scope: resourceGroup(subscription().subscriptionId, containerRegistryResourceGroupName)
  params: {
    containerRegistryName: containerRegistryName
    identityPrincipalId: userAssignedIdentity.properties.principalId
  }
}

var certificateName = '${domainName}-certificate'
var isCustomDomainSet = domainName != ''

module newManagedCertificate './managed-certificate.bicep' =
  if (isCustomDomainSet) {
    name: certificateName
    scope: resourceGroup(resourceGroupName)
    dependsOn: [containerApp]
    params: {
      name: certificateName
      location: location
      tags: tags
      containerAppsEnvironmentName: containerAppsEnvironmentName
      domainName: domainName
    }
  }

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2023-05-02-preview' existing =
  if (isCustomDomainSet) {
    name: containerAppsEnvironmentName
  }

resource existingManagedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2023-05-02-preview' existing =
  if (isCustomDomainSet) {
    name: certificateName
    parent: containerAppsEnvironment
  }

var customDomainConfiguration = isCustomDomainSet
  ? [
      {
        bindingType: domainConfigured ? 'SniEnabled' : 'Disabled'
        name: domainName
        certificateId: domainConfigured ? existingManagedCertificate.id : null
      }
    ]
  : []

var publicUrl = isCustomDomainSet
  ? 'https://${domainName}'
  : 'https://${name}.${containerAppsEnvironment.properties.defaultDomain}'
var cdnUrl = publicUrl

var imageTag = containerImageTag != '' ? containerImageTag : 'latest'

var containerRegistryServerUrl = '${containerRegistryName}.azurecr.io'
resource containerApp 'Microsoft.App/containerApps@2023-05-02-preview' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${userAssignedIdentity.id}': {}
    }
  }
  properties: {
    environmentId: containerAppsEnvironmentId
    template: {
      containers: [
        {
          name: name
          image: '${containerRegistryServerUrl}/${containerImageName}:${imageTag}'
          resources: {
            cpu: json(cpu)
            memory: memory
          }
          env: [
            {
              name: 'ConnectionStrings__${sqlDatabaseName}'
              value: 'Server=tcp:${sqlServerName}${environment().suffixes.sqlServerHostname},1433;Initial Catalog=${sqlDatabaseName};User Id=${userAssignedIdentity.properties.clientId};Authentication=Active Directory Default;TrustServerCertificate=True;'
            }
            {
              name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
              value: applicationInsightsConnectionString
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
              name: 'KEYVAULT_URL'
              value: keyVault.properties.vaultUri
            }
          ]
        }
      ]
      revisionSuffix: containerImageTag == '' ? 'initial' : null
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
    configuration: {
      registries: [
        {
          server: containerRegistryServerUrl
          identity: userAssignedIdentity.id
        }
      ]
      ingress: {
        external: true
        targetPort: 8443
        exposedPort: 0
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
        customDomains: customDomainConfiguration
        stickySessions: null
      }
    }
  }
  dependsOn: [containerRegistryPermission]
}

resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2021-10-01' = {
  parent: keyVault
  name: 'add'
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: userAssignedIdentity.properties.principalId
        permissions: {
          secrets: [
            'get'
          ]
        }
      }
    ]
  }
}
