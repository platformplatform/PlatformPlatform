resource "azurerm_container_registry" "container_registry" {
  name                = var.container_registry
  location            = var.shared_resource_location
  resource_group_name = azurerm_resource_group.shared_resource_group.name
  sku                 = "Basic"
  admin_enabled       = true

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}
