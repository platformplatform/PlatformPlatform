output "log_analytics_workspace_name_out" {
  value = azurerm_log_analytics_workspace.log_analytics_workspace.name
}

output "resource_group_name_out" {
  value = module.monitor_resource_group.resource_group_name_out
}
