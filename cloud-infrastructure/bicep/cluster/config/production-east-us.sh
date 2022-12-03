environment="production"
location="EastUS"
locationPrefix="east-us"
clusterUniqueName="mentumprodeus"
useMssqlElasticPool=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
