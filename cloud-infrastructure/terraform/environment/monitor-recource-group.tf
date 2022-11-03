resource "azurerm_resource_group" "monitor_resource_group" {
  name     = "monitor"
  location = var.global_resource_location

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}

resource "azurerm_management_lock" "monitor_resource_group_lock" {
  name       = "monitor-resource-group-lock"
  scope      = azurerm_resource_group.monitor_resource_group.id
  lock_level = "CanNotDelete"
}
