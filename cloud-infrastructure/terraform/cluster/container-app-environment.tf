// Terraform doesn't support creating container apps yet https://github.com/hashicorp/terraform-provider-azurerm/issues/14122
// So here we are using the Azure AzAPI Provider to create Azure resources uisng Azure ARM templates
resource "azapi_resource" "container-apps-environment" {
  name      = "container-apps-environment"
  type      = "Microsoft.App/managedEnvironments@2022-03-01"
  location  = var.cluster_location
  parent_id = azurerm_resource_group.cluster-resource-group.id

  body = jsonencode({
    properties = {
      appLogsConfiguration = {
        destination = "log-analytics"
        logAnalyticsConfiguration = {
          customerId = data.azurerm_log_analytics_workspace.log-analytics-workspace.workspace_id
          sharedKey  = data.azurerm_log_analytics_workspace.log-analytics-workspace.primary_shared_key
        }
      },
      vnetConfiguration = {
          internal = false,
          infrastructureSubnetId = azurerm_subnet.subnet.id
      },      
      zoneRedundant = true
    }
  })

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}
