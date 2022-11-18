resource "azurerm_mssql_server" "mssql_server" {
  name                                 = var.unique_name
  location                             = var.resource_location
  resource_group_name                  = var.resource_group_name
  version                              = "12.0"
  minimum_tls_version                  = "1.2"
  outbound_network_restriction_enabled = true

  azuread_administrator {
    login_username              = var.sql_admin_name
    object_id                   = var.sql_admin_object_id
    azuread_authentication_only = true
  }

  identity {
    type = "SystemAssigned"
  }

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
