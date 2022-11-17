module "cluster_resource_group" {
  source              = "../modules/resource-group"
  tags                = local.tags
  resource_location   = var.resource_location
  resource_group_name = var.resource_group_name
}

module "virtual_network" {
  source              = "../modules/virtual-network"
  tags                = local.tags
  resource_location   = var.resource_location
  resource_group_name = var.resource_group_name

  depends_on = [
    module.cluster_resource_group
  ]
}

module "key_vault" {
  source              = "../modules/key-vault"
  tags                = local.tags
  resource_location   = var.resource_location
  resource_group_name = var.resource_group_name
  unique_name         = var.cluster_unique_name
  subnet_id           = module.virtual_network.subnet_id_out

  depends_on = [
    module.virtual_network
  ]
}

module "service_bus_namespace" {
  source              = "../modules/service-bus-namespace"
  tags                = local.tags
  resource_location   = var.resource_location
  resource_group_name = var.resource_group_name
  unique_name         = var.cluster_unique_name

  depends_on = [
    module.cluster_resource_group
  ]
}

module "container_apps_environment" {
  source            = "../modules/container-apps-environment"
  tags              = local.tags
  environment       = var.environment
  resource_location = var.resource_location
  resource_group_id = module.cluster_resource_group.resource_group_id_out
  subnet_id         = module.virtual_network.subnet_id_out

  depends_on = [
    module.virtual_network
  ]
}
