resource "azurerm_application_insights" "application-insights" {
  name                       = "application-insights"
  resource_group_name        = azurerm_resource_group.monitor-resource-group.name
  location                   = var.monitor_resource_location
  application_type           = "web"
  workspace_id               = azurerm_log_analytics_workspace.log-analytics-workspace.id
  internet_ingestion_enabled = true
  retention_in_days          = 90

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}
