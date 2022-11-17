module "monitor_resource_group" {
  source              = "../resource-group"
  tags                = var.tags
  resource_location   = var.resource_location
  resource_group_name = "${var.environment}-monitor"
}

resource "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                       = "${var.environment}-log-analytics-workspace"
  resource_group_name        = module.monitor_resource_group.resource_group_name_out
  location                   = var.resource_location
  sku                        = "PerGB2018"
  retention_in_days          = "30"
  internet_ingestion_enabled = "true"
  internet_query_enabled     = "true"

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}

resource "azurerm_application_insights" "application_insights" {
  name                       = "${var.environment}-application-insights"
  resource_group_name        = module.monitor_resource_group.resource_group_name_out
  location                   = var.resource_location
  application_type           = "web"
  workspace_id               = azurerm_log_analytics_workspace.log_analytics_workspace.id
  internet_ingestion_enabled = true
  retention_in_days          = 90

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
