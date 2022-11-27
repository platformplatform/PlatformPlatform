targetScope = 'subscription'

param environment string
param location string = deployment().location

var tags = { environment: environment, 'managed-by': 'bicep' }

resource monitorResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${environment}-monitor'
  location: location
  tags: tags
}

module logAnalyticsWorkspace '../modules/log-analytics-workspace.bicep' = {
  name: '${deployment().name}-log-analytics-workspace'
  scope: resourceGroup(monitorResourceGroup.name)
  params: {
    name: '${environment}-log-analytics-workspace'
    location: location
    tags: tags
  }
}
