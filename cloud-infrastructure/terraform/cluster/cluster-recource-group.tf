# Create a resource group
resource "azurerm_resource_group" "cluster-resource-group" {
  name     = "__resourcegroupname__"
  location = "__location__"
}

resource "azurerm_management_lock" "cluster-resource-group-lock" {
  name       = "cluster-resource-group-lock"
  scope      = azurerm_resource_group.cluster-resource-group.id
  lock_level = "CanNotDelete"
}
