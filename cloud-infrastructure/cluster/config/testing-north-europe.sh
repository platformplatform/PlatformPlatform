environment="testing"
location="NorthEurope"
locationPrefix="north-europe"
clusterUniqueName="p14mtestneu"
useMssqlElasticPool=false
containerRegistryName="platformplatformtest"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
