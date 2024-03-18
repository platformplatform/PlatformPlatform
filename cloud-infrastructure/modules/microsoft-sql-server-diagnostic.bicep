param diagnosticStorageAccountName string
param microsoftSqlServerName string
param principalId string
param dianosticStorageAccountSubscriptionId string
param dianosticStorageAccountBlobEndpoint string

module diagnosticStorageBlobDataContributorRoleAssignment 'role-assignments-storage-blob-data-contributor.bicep' = if (principalId != '') {
  name: '${microsoftSqlServerName}-sql-server-blob-contributer-role-assignment'
  params: {
    storageAccountName: diagnosticStorageAccountName
    principalId: principalId
  }
}

resource existingMicrosoftSqlServer 'Microsoft.Sql/servers@2023-05-01-preview' existing = {
  name: microsoftSqlServerName
}

resource microsoftSqlServerOutboundFirewallRules 'Microsoft.Sql/servers/outboundFirewallRules@2023-05-01-preview' = {
  parent: existingMicrosoftSqlServer
  name: replace(replace(dianosticStorageAccountBlobEndpoint, 'https:', ''), '/', '')
  dependsOn: [diagnosticStorageBlobDataContributorRoleAssignment]
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
