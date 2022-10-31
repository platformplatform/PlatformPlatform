terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.29.1"
    }

    azapi = {
      source  = "azure/azapi"
      version = "=1.0.0"
    }
  }
}

provider "azurerm" {
  features {}
}

provider "azapi" {
}

