variable "environment" {
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
  type        = string
}

variable "global_resource_location" {
  description = "The location of monitor resources."
  type        = string
}
