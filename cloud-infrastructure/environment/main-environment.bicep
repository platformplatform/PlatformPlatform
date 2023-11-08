targetScope = 'subscription'

param environment string
param resourceGroupName string
param location string = deployment().location

var tags = { environment: environment, 'managed-by': 'bicep' }

resource monitorResourceGroup 'Microsoft.Resources/resourceGroups@2023-07-01' = {
  name: resourceGroupName
  location: location
  tags: tags
}

module logAnalyticsWorkspace '../modules/log-analytics-workspace.bicep' = {
  name: 'log-analytics-workspace'
  scope: resourceGroup(monitorResourceGroup.name)
  params: {
    name: '${environment}-log-analytics-workspace'
    location: location
    tags: tags
  }
}

module applicationInsights '../modules/application-insights.bicep' = {
  name: 'application-insights'
  scope: resourceGroup(monitorResourceGroup.name)
  params: {
    name: '${environment}-application-insights'
    location: location
    tags: tags
    logAnalyticsWorkspaceId: logAnalyticsWorkspace.outputs.workspaceId
  }
}
