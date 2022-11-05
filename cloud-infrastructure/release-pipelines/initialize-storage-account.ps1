[CmdletBinding()]
param ($location, $resourcegroup, $storageaccount, $container)

# This will create an Azure resource group for the storgae account if it does not exsist
az group create --location $location --name $resourcegroup

# This will create a storage account if it does not exsist
az storage account create --name $storageaccount --resource-group $resourcegroup --location $location --sku Standard_LRS --min-tls-version TLS1_2

# This will create a container if it does not exsist
az storage container create --name $container --account-name $storageaccount