environment="production"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="mentumprodweu"
useMssqlElasticPool=true

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
