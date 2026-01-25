using './main-cluster.bicep'

// Read all parameters from environment variables
param location = readEnvironmentVariable('LOCATION')
param clusterResourceGroupName = readEnvironmentVariable('CLUSTER_RESOURCE_GROUP_NAME')
param globalResourceGroupName = readEnvironmentVariable('GLOBAL_RESOURCE_GROUP_NAME')
param environment = readEnvironmentVariable('ENVIRONMENT')
param containerRegistryName = readEnvironmentVariable('CONTAINER_REGISTRY_NAME')
param domainName = readEnvironmentVariable('DOMAIN_NAME', '')
param sqlAdminObjectId = readEnvironmentVariable('SQL_ADMIN_OBJECT_ID')
param appGatewayVersion = readEnvironmentVariable('APP_GATEWAY_VERSION')
param accountVersion = readEnvironmentVariable('ACCOUNT_VERSION')
param backOfficeVersion = readEnvironmentVariable('BACK_OFFICE_VERSION')
param applicationInsightsConnectionString = readEnvironmentVariable('APPLICATIONINSIGHTS_CONNECTION_STRING')
param revisionSuffix = readEnvironmentVariable('REVISION_SUFFIX')
param googleOAuthClientId = readEnvironmentVariable('GOOGLE_OAUTH_CLIENT_ID', '')
param googleOAuthClientSecret = readEnvironmentVariable('GOOGLE_OAUTH_CLIENT_SECRET', '')
