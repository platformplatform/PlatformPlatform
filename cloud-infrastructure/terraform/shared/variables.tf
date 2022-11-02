variable "shared_resource_location" {
  type        = string
  description = "The location of shared resources."
}

variable "environment" {
  type        = string
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
}
