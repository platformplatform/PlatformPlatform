param name string
param location string
param tags object
param containerAppsEnvironmentName string
param domainName string

resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' existing = {
  name: containerAppsEnvironmentName
}

resource managedCertificate 'Microsoft.App/managedEnvironments/managedCertificates@2024-03-01' = {
  name: name
  parent: containerAppsEnvironment
  location: location
  tags: tags
  properties: {
    subjectName: domainName
    domainControlValidation: 'CNAME'
  }
}

output certificateId string = managedCertificate.id
