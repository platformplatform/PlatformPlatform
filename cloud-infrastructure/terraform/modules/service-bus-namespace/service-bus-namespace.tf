resource "azurerm_servicebus_namespace" "service_bus" {
  name                = var.unique_name
  location            = var.resource_location
  resource_group_name = var.resource_group_name
  sku                 = "Basic"
  minimum_tls_version = "1.2"
  local_auth_enabled  = false

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
