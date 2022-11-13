resource "azurerm_virtual_network" "virtual_network" {
  name                = "virtual_network"
  location            = var.resource_location
  resource_group_name = var.resource_group_name
  address_space       = ["10.0.0.0/16"]

  depends_on = [
    azurerm_network_watcher.network_watcher
  ]

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}

resource "azurerm_subnet" "subnet" {
  name                 = "subnet"
  resource_group_name  = var.resource_group_name
  virtual_network_name = azurerm_virtual_network.virtual_network.name
  address_prefixes     = ["10.0.0.0/23"]
  service_endpoints    = ["Microsoft.KeyVault"]
}

resource "azurerm_network_watcher" "network_watcher" {
  name                = "network-watcher"
  location            = var.resource_location
  resource_group_name = var.resource_group_name

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
