variable "environment" {
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
  type        = string
}

variable "resource_location" {
  description = "The location of resources."
  type        = string
}

variable "container_registry_name" {
  description = "The global unique Azure Container Registry name."
  type        = string
}
