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

variable "cluster_unique_name" {
  description = "Generic name or name prefix for all resources that needs a global unique name."
  type        = string
}

variable "use_mssql_elasticpool" {
  description = "Indicates if a SQL elastic pool should be used for SQL databases."
  type        = bool
}
