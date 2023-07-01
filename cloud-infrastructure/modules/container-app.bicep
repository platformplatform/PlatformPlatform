param name string
param location string
param tags object
param customLocationName string
param adGroupId string
param resourceGroupName string
param subscriptionId string
param identityName string
param acrServerUrl string
param containerImageName string
param containerImageTag string

resource containerApp 'Microsoft.App/containerApps@2022-11-01-preview' = {
  name: name
  location: location
  tags: tags
  extendedLocation: {
    name: customLocationName
    type: 'CustomLocation'
  }
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    template: {
      containers: [
        {
          name: 'app'
          image: '${acrServerUrl}/${containerImageName}:${containerImageTag}'
        }
      ]
    }
  }
}

resource addIdentityToAdGroup 'Microsoft.Resources/deploymentScripts@2020-10-01' = {
    name: '${containerApp.name}-AddIdentityToAdGroup'
    location: location
    kind: 'AzureCLI'
    identity: {
      type: 'UserAssigned'
      userAssignedIdentities: {
        '/subscriptions/${subscriptionId}/resourceGroups/${resourceGroupName}/providers/Microsoft.ManagedIdentity/userAssignedIdentities/${identityName}': {}
      }
    }
    properties: {
      forceUpdateTag: guid('${containerApp.name}-AddIdentityToAdGroup')
      cleanupPreference: 'OnSuccess'
      retentionInterval: 'P1D'
      azCliVersion: '2.30.0'
      timeout: 'PT30M'
      scriptContent: 'az ad group member add --group ${adGroupId} --member-id ${containerApp.identity.principalId}'
    }
  }
