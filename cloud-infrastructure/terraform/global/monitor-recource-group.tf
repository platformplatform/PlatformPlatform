resource "azurerm_resource_group" "monitor-resource-group" {
  name     = "Monitor"
  location = var.global_resource_location

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}

resource "azurerm_management_lock" "monitor-resource-group-lock" {
  name       = "monitor-resource-group-lock"
  scope      = azurerm_resource_group.monitor-resource-group.id
  lock_level = "CanNotDelete"
  tags = local.tags
}
