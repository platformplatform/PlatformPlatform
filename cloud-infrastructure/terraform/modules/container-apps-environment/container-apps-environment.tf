terraform {
  required_providers {
    azapi = {
      source  = "azure/azapi"
      version = "=1.0.0"
    }
  }
}

data "azurerm_log_analytics_workspace" "log_analytics_workspace" {
  name                = "${var.environment}-log-analytics-workspace"
  resource_group_name = "${var.environment}-monitor"
}

// Terraform doesn't support creating container apps yet https://github.com/hashicorp/terraform-provider-azurerm/issues/14122
// So here we are using the Azure AzAPI Provider to create Azure resources uisng Azure ARM templates
resource "azapi_resource" "container_apps_environment" {
  name      = "${replace(lower(var.resource_location), " ", "-")}-container-apps-environment"
  type      = "Microsoft.App/managedEnvironments@2022-03-01"
  location  = var.resource_location
  parent_id = var.resource_group_id

  body = jsonencode({
    properties = {
      appLogsConfiguration = {
        destination = "log-analytics"
        logAnalyticsConfiguration = {
          customerId = data.azurerm_log_analytics_workspace.log_analytics_workspace.workspace_id
          sharedKey  = data.azurerm_log_analytics_workspace.log_analytics_workspace.primary_shared_key
        }
      },
      vnetConfiguration = {
        internal               = false,
        infrastructureSubnetId = var.subnet_id
      },
      zoneRedundant = true
    }
  })

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
