targetScope = 'subscription'

param location string = deployment().location
param globalResourceGroupName string
param uniquePrefix string
param environment string
param containerRegistryName string
param productionServicePrincipalObjectId string = ''

var tags = { environment: environment, 'managed-by': 'bicep' }
var resourceNamePrefix = '${uniquePrefix}-${environment}'

resource globalResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: globalResourceGroupName
  location: location
  tags: tags
}

module containerRegistry '../modules/container-registry.bicep' = {
  name: '${globalResourceGroupName}-container-registry'
  scope: resourceGroup(globalResourceGroup.name)
  params: {
    name: containerRegistryName
    location: location
    tags: tags
  }
}

// Grant production service principal Container Registry Data Importer access to registry if specified
module productionServicePrincipalDataImporter '../modules/role-assignments-container-registry-data-importer.bicep' = if (!empty(productionServicePrincipalObjectId)) {
  name: '${globalResourceGroupName}-production-sp-data-importer'
  scope: resourceGroup(globalResourceGroup.name)
  params: {
    containerRegistryName: containerRegistryName
    principalId: productionServicePrincipalObjectId
  }
  dependsOn: [containerRegistry]
}

module logAnalyticsWorkspace '../modules/log-analytics-workspace.bicep' = {
  name: '${globalResourceGroupName}-log-analytics-workspace'
  scope: resourceGroup(globalResourceGroup.name)
  params: {
    name: resourceNamePrefix
    location: location
    tags: tags
  }
}

module applicationInsights '../modules/application-insights.bicep' = {
  name: '${globalResourceGroupName}-application-insights'
  scope: resourceGroup(globalResourceGroup.name)
  params: {
    name: resourceNamePrefix
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.outputs.workspaceId
  }
}
