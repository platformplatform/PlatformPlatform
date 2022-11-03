data "azurerm_client_config" "current" {}

data "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "log-analytics-workspace"
  resource_group_name = "monitor"
}
