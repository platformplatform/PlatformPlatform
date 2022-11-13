variable "environment" {
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
  type        = string
}

variable "resource_location" {
  description = "The location of resources."
  type        = string
}
