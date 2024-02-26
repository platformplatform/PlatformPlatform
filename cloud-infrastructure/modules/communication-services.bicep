param name string
param tags object
param dataLocation string
param mailSenderDisplayName string
param keyVaultName string

resource emailServices 'Microsoft.Communication/emailServices@2023-06-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
  }
}

resource azureManagedDomainEmailServices 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' = {
  name: 'AzureManagedDomain'
  location: 'global'
  tags: tags
  parent: emailServices
  properties: {
    domainManagement: 'AzureManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource senderUsername 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-06-01-preview' = {
  name: 'no-reply'
  parent: azureManagedDomainEmailServices
  properties: {
    displayName: mailSenderDisplayName
    username: 'no-reply'
  }
}

resource communicationServices 'Microsoft.Communication/communicationServices@2023-06-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
    linkedDomains: [azureManagedDomainEmailServices.id]
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2021-10-01' existing = {
  name: keyVaultName
}

resource communicationServiceConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2021-10-01' = {
  parent: keyVault
  name: 'communication-services-connection-string'
  properties: {
    value: communicationServices.listKeys().primaryConnectionString
  }
}

