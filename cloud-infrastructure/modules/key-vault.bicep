param name string
param location string
param tags object
param tenantId string
param subnetId string
param storageAccountId string
param workspaceId string

resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: name
  location: location
  tags: tags
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: tenantId
    networkAcls: {
      bypass: 'AzureServices'
      defaultAction: 'Deny'
      ipRules: []
      virtualNetworkRules: [
        {
          id: subnetId
        }
      ]
    }
    enabledForDeployment: false
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: false
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: true
    publicNetworkAccess: 'Enabled'
  }
}

resource keyVaultAuditDiagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: keyVault
  name: 'key-vault-audit'
  properties: {
    storageAccountId: storageAccountId
    logs: [
      {
        categoryGroup: 'audit'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        categoryGroup: 'allLogs'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

resource keyVaultMetricDiagnosticSetting 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = {
  scope: keyVault
  name: 'key-vault-metrics'
  properties: {
    workspaceId: workspaceId
    logs: [
      {
        category: 'AuditEvent'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
      {
        category: 'AzurePolicyEvaluationDetails'
        enabled: false
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
        retentionPolicy: {
          enabled: false
          days: 0
        }
      }
    ]
  }
}

// These keys and secrets are used by all self-contained systems to generate and validate JWT authentication tokens
// Note: Changing these values will invalidate all existing tokens and log out all users
resource authenticationTokenSigningKey 'Microsoft.KeyVault/vaults/keys@2023-07-01' = {
  parent: keyVault
  name: 'authentication-token-signing-key'
  properties: {
    keySize: 2048
    kty: 'RSA'
  }
}

resource authenticationTokenIssuer 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'authentication-token-issuer'
  properties: {
    value: 'PlatformPlatform' // Consider using the domain name (https://app.your-company.net) or company name (Your Company) as the issuer
  }
}

resource authenticationTokenAudience 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'authentication-token-audience'
  properties: {
    value: 'PlatformPlatform' // Consider using the domain name (https://product.your-company.net) or product name (product-name) as the audience
  }
}

output name string = keyVault.name
output keyVaultId string = keyVault.id
