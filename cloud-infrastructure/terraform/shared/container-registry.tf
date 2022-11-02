resource "azurerm_container_registry" "container-registry" {
  name                = var.container_registry
  location            = var.shared_resource_location
  resource_group_name = azurerm_resource_group.shared-resource-group.name
  sku                 = "Basic"
  admin_enabled       = true

  tags = local.tags
}