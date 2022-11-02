
resource "azurerm_resource_group" "shared_resource_group" {
  name     = "shared"
  location = var.shared_resource_location

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}

resource "azurerm_management_lock" "shared_resource_group_lock" {
  name       = "shared-resource-group-lock"
  scope      = azurerm_resource_group.shared_resource_group.id
  lock_level = "CanNotDelete"
}
