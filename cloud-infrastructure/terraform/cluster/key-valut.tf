resource "azurerm_key_vault" "key_vault" {
  name                          = var.cluster_unique_name
  location                      = var.cluster_location
  resource_group_name           = azurerm_resource_group.cluster_resource_group.name
  sku_name                      = "standard"
  tenant_id                     = data.azurerm_client_config.current.tenant_id
  public_network_access_enabled = true
  soft_delete_retention_days    = 7
  purge_protection_enabled      = false

  network_acls {
    bypass                     = "AzureServices"
    default_action             = "Deny"
    virtual_network_subnet_ids = [azurerm_subnet.subnet.id]
  }

  lifecycle {
    prevent_destroy = true
  }

  tags = local.tags
}
