#!/bin/bash

echo "Logging in to Azure"
az login

echo "Prompting for Azure Subscription and GitHub Repository"
echo "Enter your Azure Subscription ID: "
read subscriptionId

# Prompt for GitHub repository URL and validate its format
echo "Enter your GitHub repository URL (e.g., https://github.com/<Organization>/<Repository>): "
read gitHubRepositoryUrl

echo "Validating and extract the GitHub Organization and Repository"
if [[ $gitHubRepositoryUrl =~ ^https://github\.com/([a-zA-Z0-9_-]+)/([a-zA-Z0-9_-]+)$ ]]; then
    if [ -n "$BASH_VERSION" ]; then
        gitHubOrganization="${BASH_REMATCH[1]}"
        gitHubRepositoryName="${BASH_REMATCH[2]}"
    elif [ -n "$ZSH_VERSION" ]; then
        gitHubOrganization="${match[1]}"
        gitHubRepositoryName="${match[2]}"
    else
        echo "Unsupported shell."
        return 1
    fi
    gitHubRepositoryPath="$gitHubOrganization/$gitHubRepositoryName"
    echo "Successfully extracted GitHub Organization and Repository: $gitHubRepositoryPath"
    sleep 2
else
  echo "Invalid GitHub URL. Please follow the format: https://github.com/<Organization>/<Repository>"
  return 1
fi

az account set --subscription $subscriptionId
az account show --query 'id' -o tsv | grep -q $subscriptionId
if [ $? -eq 0 ]; then
    echo "Successfully set subscription to $subscriptionId"
    sleep 2
else
    echo "Failed to set subscription to $subscriptionId"
    return 1
fi

echo "Checking if custom role already exists"
roleExists=$(az role definition list --name "PlatformPlatform - Azure Infrastructure Reader" --query "[].roleName" -o tsv)
if [ "$roleExists" = "PlatformPlatform - Azure Infrastructure Reader" ]; then
    echo "Custom role already exists"
else
    echo "Creating custom role"
    # Export the variable so it can be used by envsubst
    export subscriptionId
    # Substitute the subscription ID into the custom role definition JSON template and create the custom role
    envsubst < azure-infrastructure-reader-custom-role-definition.json | az role definition create --role-definition @-
fi
sleep 2

echo "Checking if Service Principals already exist"
readerExists=$(az ad sp list --display-name "GitHub Workflows - Reader" --query "[].appId" -o tsv)
writerExists=$(az ad sp list --display-name "GitHub Workflows - Writer" --query "[].appId" -o tsv)

if [ -n "$readerExists" ]; then
    echo "GitHub Workflows - Reader already exists with App ID: $readerExists"
    return 1
fi

if [ -n "$writerExists" ]; then
    echo "GitHub Workflows - Writer already exists with App ID: $writerExists"
    return 1
fi
sleep 2

echo "Creating Azure AD Service Principals"
readerAppId=$(az ad app create --display-name "GitHub Workflows - Reader" --query 'appId' -o tsv)
az ad sp create --id $readerAppId
writerAppId=$(az ad app create --display-name "GitHub Workflows - Writer" --query 'appId' -o tsv)
az ad sp create --id $writerAppId
sleep 2

echo "Creating Federated Credentials for Main Branch"
mainCredential=$(echo -n "{
  \"name\": \"MainBranch\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$gitHubRepositoryPath:ref:refs/heads/main\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
echo $mainCredential | az ad app federated-credential create --id $readerAppId --parameters @-
echo $mainCredential | az ad app federated-credential create --id $writerAppId --parameters @-
sleep 2

echo " Creating Federated Credentials for Pull Requests"
pullRequestCredential=$(echo -n "{
  \"name\": \"PullRequests\",
  \"issuer\": \"https://token.actions.githubusercontent.com\",
  \"subject\": \"repo:$gitHubRepositoryPath:pull_request\",
  \"audiences\": [\"api://AzureADTokenExchange\"]
}")
echo $pullRequestCredential | az ad app federated-credential create --id $readerAppId --parameters @-
sleep 2

echo "Assigning Custom ('PlatformPlatform - Azure Infrastructure Reader') subscription roles for 'GitHub Workflows - Reader'"
az role assignment create --assignee $readerAppId --role "PlatformPlatform - Azure Infrastructure Reader" --scope "/subscriptions/$subscriptionId" --description "Used by GitHub workflows to detect changes to Infrastrcture (read-only)"

echo "Assigning ContContributor subscription roles for 'GitHub Workflows - Writer'"
az role assignment create --assignee $writerAppId --role Contributor --scope "/subscriptions/$subscriptionId" --description "Used by GitHub workflows to update Azure Infrastrcture"
sleep 2

echo "Outputting Instructions for GitHub Secrets"
tenantId=$(az account show --query 'tenantId' -o tsv)
echo "Create the following GitHub repository secrets:"
echo "- AZURE_TENANT_ID: $tenantId"
echo "- AZURE_SUBSCRIPTION_ID: $subscriptionId"
echo "- AZURE_READER_CLIENT_ID: $readerAppId"
echo "- AZURE_WRITER_CLIENT_ID: $writerAppId"