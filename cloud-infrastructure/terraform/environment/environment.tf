module "azure_monitor" {
  source            = "../modules/azure-monitor"
  tags              = local.tags
  environment       = var.environment
  resource_location = var.resource_location
}
