environment="testing"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="p14mtestweu"
useMssqlElasticPool=false
acrName="platformplatformtest"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
