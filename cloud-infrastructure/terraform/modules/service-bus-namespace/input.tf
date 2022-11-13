variable "tags" {
  description = "Azure tags put on all resource to en able better understanding owner and billing."
  type = object({
    environment = string
    managed-by  = string
  })
}

variable "environment" {
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
  type        = string
}

variable "resource_location" {
  description = "The location of resources."
  type        = string
}

variable "resource_group_name" {
  description = "The name of the recource group."
  type        = string
}

variable "unique_name" {
  description = "The global unique name for the Azure Service Bus Namespace."
  type        = string
}
