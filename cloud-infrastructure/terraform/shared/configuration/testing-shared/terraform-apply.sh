cd ../../
terraform init -backend-config=./configuration/testing-shared/terraform-backend.hcl
terraform apply -var-file=./configuration/testing-shared/terraform.tfvars
rm -rf directoryname .terraform
rm .terraform.lock.hcl