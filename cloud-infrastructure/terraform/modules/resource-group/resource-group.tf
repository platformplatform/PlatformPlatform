resource "azurerm_resource_group" "resource_group" {
  name     = var.resource_group_name
  location = var.resource_location

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}

resource "azurerm_management_lock" "resource_group_lock" {
  name       = "resource-group-lock"
  scope      = azurerm_resource_group.resource_group.id
  lock_level = "CanNotDelete"

  depends_on = [
    azurerm_resource_group.resource_group
  ]
}
