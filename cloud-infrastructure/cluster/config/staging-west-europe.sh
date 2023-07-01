environment="staging"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="p14mstageweu"
useMssqlElasticPool=false
containerRegistryName="platformplatform"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
