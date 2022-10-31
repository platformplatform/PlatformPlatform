resource "azurerm_virtual_network" "virtual-network" {
  name                = "virtual-network"
  location            = var.cluster_location
  resource_group_name = azurerm_resource_group.cluster-resource-group.name
  address_space       = ["10.0.0.0/16"]

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}

resource "azurerm_subnet" "subnet" {
  name                 = "subnet"
  resource_group_name  = azurerm_resource_group.cluster-resource-group.name
  virtual_network_name = azurerm_virtual_network.virtual-network.name
  address_prefixes     = ["10.0.0.0/23"]
}

resource "azurerm_network_watcher" "network_watcher" {
  name                = "network-watcher"
  location            = var.cluster_location
  resource_group_name = azurerm_resource_group.cluster-resource-group.name

  lifecycle {
    prevent_destroy = false
  }

  tags = local.tags
}
