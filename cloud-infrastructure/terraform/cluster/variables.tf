variable "cluster_resource_group_name" {
  type        = string
  description = "The recourece group name contaning custer resources."
}

variable "cluster_location" {
  type        = string
  description = "The location of the cluster."
}

variable "environment" {
  type        = string
  description = "The environment used for billing insighs. E.g. development, staging, production, shared."
}
