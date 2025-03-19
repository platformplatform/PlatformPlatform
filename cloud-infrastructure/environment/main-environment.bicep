targetScope = 'subscription'

param location string = deployment().location
param resourceGroupName string
param environment string
param containerRegistryName string
param productionServicePrincipalObjectId string = ''

var tags = { environment: environment, 'managed-by': 'bicep' }

resource environmentResourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module containerRegistry '../modules/container-registry.bicep' = {
  name: '${resourceGroupName}-container-registry'
  scope: resourceGroup(environmentResourceGroup.name)
  params: {
    name: containerRegistryName
    location: location
    tags: tags
  }
}

// Grant production service principal ACR Pull access to registry if specified
module productionServicePrincipalAcrPull '../modules/role-assignments-container-registry-acr-pull.bicep' = if (!empty(productionServicePrincipalObjectId)) {
  name: '${resourceGroupName}-production-sp-acr-pull'
  scope: resourceGroup(environmentResourceGroup.name)
  params: {
    containerRegistryName: containerRegistryName
    principalId: productionServicePrincipalObjectId
  }
}

module logAnalyticsWorkspace '../modules/log-analytics-workspace.bicep' = {
  name: '${resourceGroupName}-log-analytics-workspace'
  scope: resourceGroup(environmentResourceGroup.name)
  params: {
    name: resourceGroupName
    location: location
    tags: tags
  }
}

module applicationInsights '../modules/application-insights.bicep' = {
  name: '${resourceGroupName}-application-insights'
  scope: resourceGroup(environmentResourceGroup.name)
  params: {
    name: resourceGroupName
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.outputs.workspaceId
  }
}
