cd ../../
terraform init -backend-config=./configuration/testing-west-europe/terraform-backend.hcl
terraform destroy -var-file=./configuration//testing-west-europe/terrafrom.tfvars
rm -rf directoryname .terraform
rm .terraform.lock.hcl