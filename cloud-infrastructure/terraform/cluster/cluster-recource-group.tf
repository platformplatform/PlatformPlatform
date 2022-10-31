resource "azurerm_resource_group" "cluster-resource-group" {
  name     = var.cluster_resource_group_name
  location = var.cluster_location

  lifecycle {
    prevent_destroy = true
  }
}

resource "azurerm_management_lock" "cluster-resource-group-lock" {
  name       = "cluster-resource-group-lock"
  scope      = azurerm_resource_group.cluster-resource-group.id
  lock_level = "CanNotDelete"
}
