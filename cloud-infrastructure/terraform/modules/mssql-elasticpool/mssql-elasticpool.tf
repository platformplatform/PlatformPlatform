resource "azurerm_mssql_elasticpool" "mssql_elasticpool" {
  name                = "${replace(lower(var.resource_location), " ", "-")}-sql-elastic-pool"
  resource_group_name = var.resource_group_name
  location            = var.resource_location
  server_name         = var.sql_server_name
  license_type        = "BasePrice"
  max_size_gb         = "4.8828125"

  sku {
    name     = "BasicPool"
    tier     = "Basic"
    capacity = 50
  }

  per_database_settings {
    min_capacity = 0
    max_capacity = 5
  }

  lifecycle {
    prevent_destroy = false
  }

  tags = var.tags
}
