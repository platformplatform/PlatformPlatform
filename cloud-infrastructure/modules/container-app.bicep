param name string
param location string
param tags object
param acrSubscriptionId string
param acrResourceGroupName string
param acrName string
param containerImageName string
param containerImageTag string
param identityName string
param environmentId string

resource userAssignedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2022-01-31-preview' = {
  name: identityName
  location: location
  tags: tags
}

module containerRegistryPermission './container-registry-permission.bicep' = {
  name: 'acrModule'
  scope: resourceGroup(acrSubscriptionId, acrResourceGroupName)
  params: {
    acrName: acrName
    identityPrincipalId: userAssignedIdentity.properties.principalId
  }
}

var acrServerUrl = '${acrName}.azurecr.io'
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
          image: '${acrServerUrl}/${containerImageName}:${containerImageTag}'
          resources: {
            cpu: '0.5'
            memory: '1Gi'
          }
        }
      ]
    }
    configuration: {
      registries: [
        {
          server: acrServerUrl
          identity: userAssignedIdentity.id
        }
      ]
    }
  }
  dependsOn: [
    containerRegistryPermission
  ]
}
