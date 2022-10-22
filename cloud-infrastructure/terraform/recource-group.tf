# Create a resource group
resource "azurerm_resource_group" "cloud-app" {
  name     = "CloudApp"
  location = "West Europe"
}

resource "azurerm_management_lock" "cloud-app-resource-group-lock" {
  name       = "resource-group-lock"
  scope      = azurerm_resource_group.cloud-app.id
  lock_level = "CanNotDelete"
}