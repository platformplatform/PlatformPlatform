environment="testing"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="mentumtestweu"
useMssqlElasticPool=true

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
