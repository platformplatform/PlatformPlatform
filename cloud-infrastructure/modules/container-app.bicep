param name string
param location string
param tags object
param environmentId string
param containerRegistryName string
param containerImageName string
param containerImageTag string
param cpu string = '0.25'
param memory string = '0.5Gi'

var identityName = '${name}-${resourceGroup().name}'
resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: identityName
  location: location
  tags: tags
}

var containerRegistryResourceGroupName = 'shared'
module containerRegistryPermission './container-registry-permission.bicep' = {
  name: 'containerRegistryPermission'
  scope: resourceGroup(subscription().subscriptionId, containerRegistryResourceGroupName)
  params: {
    containerRegistryName: containerRegistryName
    identityPrincipalId: userAssignedIdentity.properties.principalId
  }
}

var containerRegistryServerUrl = '${containerRegistryName}.azurecr.io'
resource containerApp 'Microsoft.App/containerApps@2022-11-01-preview' = {
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
    environmentId: environmentId
    template: {
      containers: [
        {
          name: 'app'
          image: '${containerRegistryServerUrl}/${containerImageName}:${containerImageTag}'
          resources: {
            cpu: cpu
            memory: memory
          }
        }
      ]
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
        targetPort: 80
        exposedPort: 0
        allowInsecure: false
        traffic: [
          {
            latestRevision: true
            weight: 100
          }
        ]
        stickySessions: null
      }
    }
  }
  dependsOn: [
    containerRegistryPermission
  ]
}
