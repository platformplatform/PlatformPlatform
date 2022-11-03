resource "azurerm_servicebus_namespace" "service-bus" {
  name                = var.cluster_unique_name
  location            = var.cluster_location
  resource_group_name = azurerm_resource_group.cluster_resource_group.name
  sku                 = "Basic"
  minimum_tls_version = "1.2"
  local_auth_enabled  = false

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}
