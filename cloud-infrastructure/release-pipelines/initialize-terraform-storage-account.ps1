[CmdletBinding()]
param ($terraformlocation, $terraformresourcegroup, $terraformstorageaccount)

# This will create the Azure resource group for Terraform if it does not exsist
az group create --location $terraformlocation --name $terraformresourcegroup

# This will create the Terraform storage account if it does not exsist
az storage account create --name $terraformstorageaccount --resource-group $terraformresourcegroup --location $terraformlocation --sku Standard_LRS

# This will create the Terraform storage container if it does not exsist
az storage container create --name terraform --account-name $terraformstorageaccount