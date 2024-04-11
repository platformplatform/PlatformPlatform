param name string
param location string
param tags object
param repositoryUrl string
param appInsightsConnectionString string
param branch string = 'main'
param sku string = 'Free'
param tier string = 'Free'

resource hybridStaticWebsite 'Microsoft.Web/staticSites@2022-09-01' = {
  name: name
  location: location
  sku: {
    name: sku
    tier: tier
  }
  properties: {
    repositoryUrl: repositoryUrl
    branch: branch
    stagingEnvironmentPolicy: 'Disabled'
    allowConfigFileUpdates: true
    provider: 'GitHub'
    enterpriseGradeCdnStatus: 'Disabled'
    buildProperties: {
      skipGithubActionWorkflowGeneration: true
    }
  }
  tags: tags
}

resource applicationSettings 'Microsoft.Web/staticSites/config@2022-09-01' = {
  name: 'appsettings'
  parent: hybridStaticWebsite
  properties: {
    APPINSIGHTS_CONNECTION_STRING: appInsightsConnectionString
  }
}

output id string = hybridStaticWebsite.id
output defaultHostname string = hybridStaticWebsite.properties.defaultHostname
