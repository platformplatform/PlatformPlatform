resource "azurerm_virtual_network" "virtual_network" {
  name                = "virtual-network"
  location            = var.cluster_location
  resource_group_name = azurerm_resource_group.cluster_resource_group.name
  address_space       = ["10.0.0.0/16"]
  depends_on          = [azurerm_network_watcher.network_watcher]

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}

resource "azurerm_subnet" "subnet" {
  name                 = "subnet"
  resource_group_name  = azurerm_resource_group.cluster_resource_group.name
  virtual_network_name = azurerm_virtual_network.virtual_network.name
  address_prefixes     = ["10.0.0.0/23"]
  service_endpoints    = ["Microsoft.KeyVault"]
}

resource "azurerm_network_watcher" "network_watcher" {
  name                = "network-watcher"
  location            = var.cluster_location
  resource_group_name = azurerm_resource_group.cluster_resource_group.name

  lifecycle {
    prevent_destroy = false
  }

  tags = local.tags
}
