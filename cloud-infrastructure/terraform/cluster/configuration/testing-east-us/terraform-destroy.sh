cd ../../
terraform init -backend-config=./configuration/testing-east-us/terraform-backend.hcl
terraform destroy -var-file=./configuration//testing-east-us/terrafrom.tfvars
rm -rf directoryname .terraform
rm .terraform.lock.hcl