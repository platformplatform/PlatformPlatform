variable "shared_resource_location" {
  type        = string
  description = "The location of shared resources."
}

variable "environment" {
  type        = string
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
}

variable "container_registry" {
  type        = string
  description = "The shared Azure container registry used across environments."
}
