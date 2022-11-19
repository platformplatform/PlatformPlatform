variable "tags" {
  description = "Azure tags put on all resource to en able better understanding owner and billing."
  type = object({
    environment = string
    managed-by  = string
  })
}

variable "resource_location" {
  description = "The location of resources."
  type        = string
}

variable "resource_group_name" {
  description = "The name of the recource group."
  type        = string
}

variable "sql_server_name" {
  description = "The global unique name for the Azure Key Vault."
  type        = string
}

variable "subnet_id" {
  description = "Id of the subnet that have access to the SQL Server."
  type        = string
}

variable "sql_admin_name" {
  description = "The name of the Azure AD Admin in the Active Directory."
  default     = "SQL Admin"
}

variable "sql_admin_object_id" {
  description = "The ObjectId of the Azure AD Admin in the Active Directory"
  default     = "33ff85b8-6b6f-4873-8e27-04ffc252c26c"
}

variable "dianostic_storage_account_id" {
  description = "Id of the diagnostic Azure Storage account."
  type        = string
}

variable "dianostic_storage_account_blob_endpoint" {
  description = "The storage blob endpoint of the diagnostic Azure Storage account."
  type        = string
}
