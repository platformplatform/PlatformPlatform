param diagnosticStorageAccountName string
param microsoftSqlServerName string
param principalId string
param dianosticStorageAccountSubscriptionId string
param dianosticStorageAccountBlobEndpoint string

resource existingStorageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' existing = {
  scope: resourceGroup()
  name: diagnosticStorageAccountName
}

resource existingMicrosoftSqlServer 'Microsoft.Sql/servers@2023-05-01-preview' existing = {
  name: microsoftSqlServerName
}

@description(
  'This is the built-in Storage Blob Data Contributor role. See https://docs.microsoft.com/azure/role-based-access-control/built-in-roles#contributor'
)
resource existingStorageBlobDataContributorRoleDefinition 'Microsoft.Authorization/roleDefinitions@2022-05-01-preview' existing = {
  scope: subscription()
  name: 'ba92f5b4-2d11-453d-a403-e96b0029c9fe'
}

resource roleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  scope: existingStorageAccount
  name: guid(existingStorageAccount.id, principalId, existingStorageBlobDataContributorRoleDefinition.id)
  properties: {
    roleDefinitionId: existingStorageBlobDataContributorRoleDefinition.id
    principalId: principalId
    principalType: 'ServicePrincipal'
  }
}

resource microsoftSqlServerOutboundFirewallRules 'Microsoft.Sql/servers/outboundFirewallRules@2023-05-01-preview' = {
  parent: existingMicrosoftSqlServer
  name: replace(replace(dianosticStorageAccountBlobEndpoint, 'https:', ''), '/', '')
  dependsOn: [roleAssignment]
}

resource microsoftSqlServerAuditingSettings 'Microsoft.Sql/servers/auditingSettings@2023-05-01-preview' = {
  parent: existingMicrosoftSqlServer
  name: 'default'
  properties: {
    retentionDays: 90
    auditActionsAndGroups: [
      'SUCCESSFUL_DATABASE_AUTHENTICATION_GROUP'
      'FAILED_DATABASE_AUTHENTICATION_GROUP'
      'BATCH_COMPLETED_GROUP'
    ]
    isAzureMonitorTargetEnabled: true
    isManagedIdentityInUse: true
    state: 'Enabled'
    storageEndpoint: dianosticStorageAccountBlobEndpoint
    storageAccountSubscriptionId: dianosticStorageAccountSubscriptionId
  }
  dependsOn: [microsoftSqlServerOutboundFirewallRules]
}

resource microsoftSqlServerVulnerabilityAssessment 'Microsoft.Sql/servers/vulnerabilityAssessments@2023-05-01-preview' = {
  name: 'default'
  parent: existingMicrosoftSqlServer
  properties: {
    recurringScans: {
      emails: ['']
      emailSubscriptionAdmins: true
      isEnabled: true
    }
    storageContainerPath: '${dianosticStorageAccountBlobEndpoint}sql-vulnerability-scans/'
  }
  dependsOn: [microsoftSqlServerOutboundFirewallRules]
}
