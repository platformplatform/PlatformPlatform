environment="staging"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="mentumstageweu"
useMssqlElasticPool=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
