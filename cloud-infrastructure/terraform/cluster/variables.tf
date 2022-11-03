variable "cluster_resource_group_name" {
  description = "The recourece group name contaning custer resources."
  type        = string
}

variable "cluster_location" {
  description = "The location of the cluster."
  type        = string
}

variable "environment" {
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
  type        = string
}

variable "cluster_unique_name" {
  description = "Generic name or name prefix for all resources that needs a global unique name."
  type        = string
}
