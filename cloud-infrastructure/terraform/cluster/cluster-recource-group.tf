resource "azurerm_resource_group" "cluster_resource_group" {
  name     = var.cluster_resource_group_name
  location = var.cluster_location

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}

resource "azurerm_management_lock" "cluster_resource_group_lock" {
  name       = "cluster_resource_group_lock"
  scope      = azurerm_resource_group.cluster_resource_group.id
  lock_level = "CanNotDelete"
}
