data "azurerm_client_config" "current" {}

data "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "${var.environment}-log-analytics-workspace"
  resource_group_name = "${var.environment}-monitor"
}

resource "azurerm_key_vault" "key_vault" {
  name                          = var.unique_name
  location                      = var.resource_location
  resource_group_name           = var.resource_group_name
  sku_name                      = "standard"
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  public_network_access_enabled = true
  soft_delete_retention_days    = 7
  purge_protection_enabled      = false

  network_acls {
    bypass                     = "AzureServices"
    default_action             = "Deny"
    virtual_network_subnet_ids = [var.subnet_id]
  }

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}

resource "azurerm_monitor_diagnostic_setting" "key_vault_audit_diagnostic_setting" {
  name               = "key-vault-audits"
  target_resource_id = azurerm_key_vault.key_vault.id
  storage_account_id = var.dianostic_storage_account_id

  log {
    category_group = "audit"
    enabled        = true

    retention_policy {
      days    = 90
      enabled = true
    }
  }

  depends_on = [
    azurerm_key_vault.key_vault
  ]

  lifecycle {
    # A bug in Terraform triggers a update everytime. https://github.com/hashicorp/terraform-provider-azurerm/issues/10388
    ignore_changes = [log, metric]
  }
}

resource "azurerm_monitor_diagnostic_setting" "key_vault_metric_diagnostic_setting" {
  name                       = "key-vault-metrics"
  target_resource_id         = azurerm_key_vault.key_vault.id
  log_analytics_workspace_id = data.azurerm_log_analytics_workspace.log_analytics_workspace.id

  metric {
    category = "AllMetrics"
    enabled  = true

    retention_policy {
      days    = 90
      enabled = true
    }
  }

  depends_on = [
    azurerm_key_vault.key_vault
  ]

  lifecycle {
    # A bug in Terraform triggers a update everytime. https://github.com/hashicorp/terraform-provider-azurerm/issues/10388
    ignore_changes = [log, metric]
  }
}
