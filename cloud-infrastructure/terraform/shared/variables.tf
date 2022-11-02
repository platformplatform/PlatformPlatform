variable "shared_resource_location" {
  description = "The location of shared resources."
  type        = string
}

variable "environment" {
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
  type        = string
}

variable "container_registry" {
  description = "The shared Azure container registry used across environments."
  type        = string
}
