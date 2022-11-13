module "shared_resource_group" {
  source              = "../modules/resource-group"
  tags                = local.tags
  environment         = var.environment
  resource_location   = var.resource_location
  resource_group_name = "shared"
}

module "shared_container_registry" {
  source                  = "../modules/container-registry"
  tags                    = local.tags
  environment             = var.environment
  resource_location       = var.resource_location
  resource_group_name     = module.shared_resource_group.resource_group_name_out
  container_registry_name = var.container_registry_name
}
