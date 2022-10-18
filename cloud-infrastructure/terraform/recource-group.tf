# Create a resource group
resource "azurerm_resource_group" "CloudApp" {
  name     = "CloudApp"
  location = "West Europe"
}