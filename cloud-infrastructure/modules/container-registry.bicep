param name string
param location string
param tags object
param adGroupId string

resource containerRegistry 'Microsoft.ContainerRegistry/registries@2021-09-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Basic'
  }
  identity: {
    type: 'None'
  }
  properties: {
    adminUserEnabled: true
    dataEndpointEnabled: false
    publicNetworkAccess: 'Enabled'
    networkRuleBypassOptions: 'AzureServices'
    zoneRedundancy: 'Disabled'
    encryption: {
      status: 'disabled'
    }
  }
}

resource existingContainerRegistry 'Microsoft.ContainerRegistry/registries@2021-09-01' existing = {
    name: name
  }
  
  resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
    name: guid(existingContainerRegistry.id, adGroupId, '7f951dda-4ed3-4680-a7ca-43fe172d538d')
    scope: existingContainerRegistry
    properties: {
      roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '7f951dda-4ed3-4680-a7ca-43fe172d538d')
      principalId: adGroupId
      principalType: 'Group'
    }
  }
