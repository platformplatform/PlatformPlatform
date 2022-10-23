# Create a resource group
resource "azurerm_resource_group" "cluster" {
  name     = "__resourcegroupname__"
  location = "__location__"
}

resource "azurerm_management_lock" "cloud-app-resource-group-lock" {
  name       = "resource-group-lock"
  scope      = azurerm_resource_group.cluster.id
  lock_level = "CanNotDelete"
}