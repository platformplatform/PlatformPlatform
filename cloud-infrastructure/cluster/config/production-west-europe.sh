environment="production"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="p14mprodweu"
useMssqlElasticPool=false
acrName="platformplatform"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
