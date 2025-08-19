using './main-cluster.bicep'

// All parameters read from environment variables - nothing hardcoded
param location = readEnvironmentVariable('LOCATION')
param resourceGroupName = readEnvironmentVariable('RESOURCE_GROUP_NAME')
param environmentResourceGroupName = readEnvironmentVariable('ENVIRONMENT_RESOURCE_GROUP_NAME')
param environment = readEnvironmentVariable('ENVIRONMENT')
param containerRegistryName = readEnvironmentVariable('CONTAINER_REGISTRY_NAME')
param domainName = readEnvironmentVariable('DOMAIN_NAME', '')
param isDomainConfigured = bool(readEnvironmentVariable('IS_DOMAIN_CONFIGURED', 'false'))
param sqlAdminObjectId = readEnvironmentVariable('SQL_ADMIN_OBJECT_ID')
param appGatewayVersion = readEnvironmentVariable('APP_GATEWAY_VERSION')
param accountManagementVersion = readEnvironmentVariable('ACCOUNT_MANAGEMENT_VERSION')
param backOfficeVersion = readEnvironmentVariable('BACK_OFFICE_VERSION')
param applicationInsightsConnectionString = readEnvironmentVariable('APPLICATIONINSIGHTS_CONNECTION_STRING')
