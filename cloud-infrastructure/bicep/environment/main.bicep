targetScope = 'subscription'

param environment string
param location string = deployment().location

var tags = { environment: environment, 'managed-by': 'bicep' }

resource monitorResourceGroup 'Microsoft.Resources/resourceGroups@2021-04-01' = {
  name: '${environment}-monitor'
  location: location
  tags: tags
}
