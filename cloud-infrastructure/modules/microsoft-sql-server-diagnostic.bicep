param diagnosticStorageAccountName string
param microsoftSqlServerName string
param dianosticStorageAccountSubscriptionId string
param dianosticStorageAccountBlobEndpoint string

resource existingMicrosoftSqlServer 'Microsoft.Sql/servers@2023-08-01' existing = {
  name: microsoftSqlServerName
}

var contributorPrincipalId = existingMicrosoftSqlServer.identity.principalId

module diagnosticStorageBlobDataContributorRoleAssignment './role-assignments-storage-blob-data-contributor.bicep' = {
  name: '${microsoftSqlServerName}-microsoft-sql-server-blob-contributer'
  params: {
    storageAccountName: diagnosticStorageAccountName
    principalId: contributorPrincipalId
  }
}

resource microsoftSqlServerOutboundFirewallRules 'Microsoft.Sql/servers/outboundFirewallRules@2023-08-01' = {
  parent: existingMicrosoftSqlServer
  name: replace(replace(dianosticStorageAccountBlobEndpoint, 'https:', ''), '/', '')
  dependsOn: [diagnosticStorageBlobDataContributorRoleAssignment]
}

resource microsoftSqlServerAuditingSettings 'Microsoft.Sql/servers/auditingSettings@2023-08-01' = {
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

resource microsoftSqlServerVulnerabilityAssessment 'Microsoft.Sql/servers/vulnerabilityAssessments@2023-08-01' = {
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
