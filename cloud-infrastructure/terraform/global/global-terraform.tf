terraform {
  # Setting the required_ Azure Provider and version
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.29.1"
    }
  }

  # Terrafrom storrage account (must be created before running Terrafrom)
  backend "azurerm" {
    storage_account_name = "__terraformstorageaccount__"
    container_name       = "__terraformcontainer__"
    key                  = "__terraformstatefile__"
  }
}

provider "azurerm" {
  features {}
}
