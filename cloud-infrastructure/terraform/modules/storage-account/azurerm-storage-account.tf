resource "azurerm_storage_account" "storage_account" {
  name                              = var.unique_name
  location                          = var.resource_location
  resource_group_name               = var.resource_group_name
  account_replication_type          = var.account_replication_type
  account_tier                      = "Standard"
  cross_tenant_replication_enabled  = false
  shared_access_key_enabled         = true
  default_to_oauth_authentication   = true
  allow_nested_items_to_be_public   = false
  infrastructure_encryption_enabled = true
  min_tls_version                   = "TLS1_2"

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
