param name string
param location string
param tags object
param customLocationName string
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
