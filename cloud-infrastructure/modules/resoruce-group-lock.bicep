resource resourceGroupLock 'Microsoft.Authorization/locks@2017-04-01' = {
  name: 'resource-group-lock'
  properties: {
    level: 'CanNotDelete'
    notes: 'Lock to prevent resource group and its resources from being deleted'
  }
}
