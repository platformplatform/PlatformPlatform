data "azurerm_log_analytics_workspace" "log-analytics-workspace" {
  name                = "log-analytics-workspace"
  resource_group_name = "monitor"
}
