param name string
param tags object
param dataLocation string
param mailSenderDisplayName string

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
