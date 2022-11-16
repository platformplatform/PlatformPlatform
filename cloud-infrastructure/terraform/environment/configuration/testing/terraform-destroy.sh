cd ../../
terraform init -backend-config=./configuration/testing/terraform-backend.hcl
terraform destroy -var-file=./configuration/testing/terrafrom.tfvars
rm -rf directoryname .terraform
rm .terraform.lock.hcl