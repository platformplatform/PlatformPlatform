data "azurerm_subscription" "current" {
}

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

resource "azurerm_mssql_virtual_network_rule" "sql_virtual_network_rule" {
  name      = "sql-virtual-network-rule"
  server_id = azurerm_mssql_server.mssql_server.id
  subnet_id = var.subnet_id

  depends_on = [
    azurerm_mssql_server.mssql_server
  ]  
}

resource "azurerm_mssql_outbound_firewall_rule" "firewall_rule" {
  name      = replace(replace(var.dianostic_storage_account_blob_endpoint, "https:", ""), "/", "")
  server_id = azurerm_mssql_server.mssql_server.id

  depends_on = [
    azurerm_mssql_server.mssql_server
  ]
}

resource "azurerm_role_assignment" "sql_server_role_assignment_to_blob_storage" {
  scope                = var.dianostic_storage_account_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_mssql_server.mssql_server.identity.0.principal_id

  depends_on = [
    azurerm_mssql_server.mssql_server
  ]
}

resource "azurerm_mssql_server_extended_auditing_policy" "mssql_server_extended_auditing_policy" {
  server_id                       = azurerm_mssql_server.mssql_server.id
  log_monitoring_enabled          = true
  storage_account_subscription_id = data.azurerm_subscription.current.subscription_id
  storage_endpoint                = var.dianostic_storage_account_blob_endpoint
  retention_in_days               = 90

  depends_on = [
    azurerm_mssql_server.mssql_server,
    azurerm_mssql_outbound_firewall_rule.firewall_rule,
    azurerm_role_assignment.sql_server_role_assignment_to_blob_storage
  ]
}
