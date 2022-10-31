variable "global_resource_location" {
  type        = string
  default     = "westeurope"
  description = "The location of global resources."
}

variable "environment" {
  type        = string
  description = "The environment used for billing insighs. E.g. development, staging, production."
}
