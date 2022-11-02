
resource "azurerm_resource_group" "shared-resource-group" {
  name     = "shared"
  location = var.shared_resource_location

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}

resource "azurerm_management_lock" "shared-resource-group-lock" {
  name       = "shared-resource-group-lock"
  scope      = azurerm_resource_group.shared-resource-group.id
  lock_level = "CanNotDelete"
}
