data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "key_vault" {
  name                          = var.unique_name
  location                      = var.resource_location
  resource_group_name           = var.resource_group_name
  sku_name                      = "standard"
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  public_network_access_enabled = true
  soft_delete_retention_days    = 7
  purge_protection_enabled      = false

  network_acls {
    bypass                     = "AzureServices"
    default_action             = "Deny"
    virtual_network_subnet_ids = [var.subnet_id]
  }

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
