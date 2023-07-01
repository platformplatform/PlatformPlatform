environment="testing"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="p14mtestweu"
useMssqlElasticPool=false
containerRegistryName="platformplatformtest"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
