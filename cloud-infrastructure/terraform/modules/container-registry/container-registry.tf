resource "azurerm_container_registry" "container_registry" {
  name                = var.container_registry_name
  location            = var.resource_location
  resource_group_name = var.resource_group_name
  sku                 = "Basic"
  admin_enabled       = true

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
