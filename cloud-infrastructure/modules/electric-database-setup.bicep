param location string
param tags object
param keyVaultName string
param postgresServerFqdn string
param databaseName string
param scriptIdentityId string

var electricUser = 'electric_${databaseName}'
var passwordSecretName = 'Electric--${databaseName}--DatabasePassword'
var databaseUrlSecretName = 'Electric--${databaseName}--DatabaseUrl'

resource electricSetupScript 'Microsoft.Resources/deploymentScripts@2023-08-01' = {
  name: 'setup-electric-${databaseName}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${scriptIdentityId}': {}
    }
  }
  kind: 'AzureCLI'
  properties: {
    azCliVersion: '2.63.0'
    retentionInterval: 'PT1H'
    cleanupPreference: 'Always'
    environmentVariables: [
      { name: 'KEY_VAULT_NAME', value: keyVaultName }
      { name: 'POSTGRES_HOST', value: postgresServerFqdn }
      { name: 'DATABASE_NAME', value: databaseName }
      { name: 'ELECTRIC_USER', value: electricUser }
      { name: 'PASSWORD_SECRET_NAME', value: passwordSecretName }
      { name: 'DATABASE_URL_SECRET_NAME', value: databaseUrlSecretName }
    ]
    scriptContent: '''
      set -e

      # Check if password already exists in Key Vault
      EXISTING_PASSWORD=$(az keyvault secret show --vault-name "$KEY_VAULT_NAME" --name "$PASSWORD_SECRET_NAME" --query value --output tsv 2>/dev/null || echo "")

      if [ -z "$EXISTING_PASSWORD" ]; then
          echo "Generating new password for $ELECTRIC_USER"
          PASSWORD=$(openssl rand -base64 30 | tr -dc 'A-Za-z0-9' | head -c 40)

          az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "$PASSWORD_SECRET_NAME" --value "$PASSWORD" --output none
      else
          echo "Using existing password for $ELECTRIC_USER from Key Vault"
          PASSWORD="$EXISTING_PASSWORD"
      fi

      # Store full DATABASE_URL in Key Vault (always update in case server FQDN changed)
      DATABASE_URL="postgresql://${ELECTRIC_USER}:${PASSWORD}@${POSTGRES_HOST}:5432/${DATABASE_NAME}?sslmode=require"
      az keyvault secret set --vault-name "$KEY_VAULT_NAME" --name "$DATABASE_URL_SECRET_NAME" --value "$DATABASE_URL" --output none

      # Install PostgreSQL client
      apt-get update -qq && apt-get install -y -qq postgresql-client > /dev/null 2>&1

      # Get Entra ID access token for PostgreSQL (using managed identity)
      ACCESS_TOKEN=$(az account get-access-token --resource-type oss-rdbms --query accessToken --output tsv)

      # Create or update PostgreSQL role
      echo "Creating/updating PostgreSQL role $ELECTRIC_USER on $POSTGRES_HOST/$DATABASE_NAME"
      PGPASSWORD=$ACCESS_TOKEN psql "host=$POSTGRES_HOST dbname=$DATABASE_NAME user=$(az ad signed-in-user show --query userPrincipalName --output tsv 2>/dev/null || echo $ELECTRIC_USER) sslmode=require" << EOSQL
      DO \$\$
      BEGIN
          IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$ELECTRIC_USER') THEN
              CREATE ROLE $ELECTRIC_USER WITH LOGIN PASSWORD '$PASSWORD' REPLICATION;
          ELSE
              ALTER ROLE $ELECTRIC_USER WITH PASSWORD '$PASSWORD';
          END IF;
      END
      \$\$;

      GRANT CONNECT ON DATABASE "$DATABASE_NAME" TO $ELECTRIC_USER;
      GRANT USAGE ON SCHEMA public TO $ELECTRIC_USER;
      GRANT SELECT ON ALL TABLES IN SCHEMA public TO $ELECTRIC_USER;
      ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO $ELECTRIC_USER;
      EOSQL

      # Output the Key Vault secret URI (safe to expose -- not the password)
      SECRET_URI=$(az keyvault secret show --vault-name "$KEY_VAULT_NAME" --name "$DATABASE_URL_SECRET_NAME" --query id --output tsv)

      echo "{\"databaseUrlSecretUri\": \"$SECRET_URI\"}" > $AZ_SCRIPTS_OUTPUT_PATH
    '''
  }
}

output databaseUrlSecretName string = databaseUrlSecretName
