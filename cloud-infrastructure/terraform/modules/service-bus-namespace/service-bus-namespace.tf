data "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "${var.environment}-log-analytics-workspace"
  resource_group_name = "${var.environment}-monitor"
}

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

resource "azurerm_monitor_diagnostic_setting" "service_bus_audit_diagnostic_setting" {
  name               = "service-bus-audits"
  target_resource_id = azurerm_servicebus_namespace.service_bus.id
  storage_account_id = var.dianostic_storage_account_id

  log {
    category_group = "audit"
    enabled        = true

    retention_policy {
      days    = 1
      enabled = true
    }
  }

  depends_on = [
    azurerm_servicebus_namespace.service_bus
  ]

  lifecycle {
    # A bug in Terraform triggers a update everytime. https://github.com/hashicorp/terraform-provider-azurerm/issues/10388
    ignore_changes = [log, metric]
  }
}

resource "azurerm_monitor_diagnostic_setting" "service_bus_metric_diagnostic_setting" {
  name                       = "service-bus-metrics"
  target_resource_id         = azurerm_servicebus_namespace.service_bus.id
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
    azurerm_servicebus_namespace.service_bus
  ]
  
  lifecycle {
    # A bug in Terraform triggers a update everytime. https://github.com/hashicorp/terraform-provider-azurerm/issues/10388
    ignore_changes = [log, metric]
  }
}
