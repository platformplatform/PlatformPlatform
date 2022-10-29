# Create a resource group
resource "azurerm_resource_group" "monitor-resource-group" {
  name     = "Monitor"
  location = "westeurope"
}

resource "azurerm_management_lock" "monitor-resource-group-lock" {
  name       = "monitor-resource-group-lock"
  scope      = azurerm_resource_group.monitor-resource-group.id
  lock_level = "CanNotDelete"
}