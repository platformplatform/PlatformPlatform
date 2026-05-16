param name string
param tags object
param dataLocation string
param mailSenderDisplayName string
param keyVaultName string

@description('Custom email domain to provision as a CustomerManaged sender (e.g. "example.com"). The cluster Bicep computes this as the eTLD+1 of the cluster ingress domainName, so the email sender ends up matching the brand of whatever subdomain the user lands on. Apple Mail OTP autofill matches on eTLD+1, so the apex sender autofills on any subdomain of the same apex. Using the apex (rather than the ingress hostname) is required because the ingress is typically a CNAME and DNS forbids TXT/other records at the same name as a CNAME (RFC 1034). The apex of a domain cannot be a CNAME, so SPF (TXT) and DKIM CNAMEs at sub-subdomains coexist freely.')
param emailDomainName string = ''

@description('When true (and `emailDomainName` is set), the CustomerManaged domain is linked to the CommunicationServices resource and becomes the active sender (no-reply@<emailDomainName>). Set automatically by the deploy workflow once it observes the CustomerManaged domain as fully verified - operators do not flip this manually. Defaults to false so the AzureManaged sender keeps mail flowing while DNS records are being added and verified, and so the first apply (which always precedes verification) does not fail with DomainValidationError when Bicep tries to link an unverified domain.')
param useCustomEmailDomain bool = false

var hasCustomDomain = emailDomainName != ''
var customerManagedDomainActive = hasCustomDomain && useCustomEmailDomain

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

resource azureManagedSenderUsername 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-06-01-preview' = {
  name: 'no-reply'
  parent: azureManagedDomainEmailServices
  properties: {
    displayName: mailSenderDisplayName
    username: 'no-reply'
  }
}

// CustomerManaged domain is created in NotStarted state. The deploy workflow then calls
// `az communication email domain initiate-verification` for Domain ownership, SPF, DKIM, and DKIM2
// (idempotent - calling it on an already-InProgress or Verified record is a no-op). Sender usernames
// can be created on the domain regardless of verification state - they only matter when mail is sent.
resource customerManagedDomainEmailServices 'Microsoft.Communication/emailServices/domains@2023-06-01-preview' = if (hasCustomDomain) {
  name: emailDomainName
  location: 'global'
  tags: tags
  parent: emailServices
  properties: {
    domainManagement: 'CustomerManaged'
    userEngagementTracking: 'Disabled'
  }
}

resource customerManagedSenderUsername 'Microsoft.Communication/emailServices/domains/senderUsernames@2023-06-01-preview' = if (hasCustomDomain) {
  name: 'no-reply'
  parent: customerManagedDomainEmailServices
  properties: {
    displayName: mailSenderDisplayName
    username: 'no-reply'
  }
}

// Link the AzureManaged domain (always) and the CustomerManaged domain only once the deploy workflow
// has detected verification is complete and exported USE_CUSTOM_EMAIL_DOMAIN=true (which sets
// useCustomEmailDomain via the bicepparam). Azure rejects linking an unverified CustomerManaged
// domain with "Requested domain is not in a valid state for linking", so the link must wait until
// the workflow's verification check passes. The resource itself is created on the first apply so the
// workflow can surface its verificationRecords for DNS setup; linking happens on the apply *after*
// DNS is in place and Azure has flipped all four states (Domain, SPF, DKIM, DKIM2) to Verified.
var linkedDomains = customerManagedDomainActive
  ? [azureManagedDomainEmailServices.id, customerManagedDomainEmailServices.id]
  : [azureManagedDomainEmailServices.id]

resource communicationServices 'Microsoft.Communication/communicationServices@2023-06-01-preview' = {
  name: name
  location: 'global'
  tags: tags
  properties: {
    dataLocation: dataLocation
    linkedDomains: linkedDomains
  }
}

resource keyVault 'Microsoft.KeyVault/vaults@2025-05-01' existing = {
  name: keyVaultName
}

resource communicationServiceConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2025-05-01' = {
  parent: keyVault
  name: 'communication-services-connection-string'
  properties: {
    value: communicationServices.listKeys().primaryConnectionString
  }
}

// Pick the active sender domain. When the workflow has auto-flipped useCustomEmailDomain=true, mail is
// sent from no-reply@<emailDomainName>; otherwise the AzureManaged sender stays active. The
// fromSenderDomain of a CustomerManaged domain equals the domain name itself, so we use the input
// parameter directly rather than reading the runtime `properties.fromSenderDomain` - that keeps the
// `--plan` (what-if) deterministic since what-if cannot simulate runtime read-only properties of
// conditional resources (it errors with "language expression property 'fromSenderDomain' doesn't exist").
output fromSenderDomain string = customerManagedDomainActive
  ? emailDomainName
  : azureManagedDomainEmailServices.properties.fromSenderDomain
