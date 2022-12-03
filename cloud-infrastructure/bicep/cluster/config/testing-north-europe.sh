environment="testing"
location="NorthEurope"
locationPrefix="north-europe"
clusterUniqueName="mentumtestneu"
useMssqlElasticPool=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
