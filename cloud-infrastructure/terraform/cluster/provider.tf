terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=3.29.1"
    }
  }

  backend "azurerm" {
  }
}

provider "azurerm" {
  features {
  }
}

locals {
  tags = {
    managed-by  = "terraform"
    environment = var.environment
  }
}
