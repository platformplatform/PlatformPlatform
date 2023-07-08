environment="staging"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="${UNIQUE_CLUSTER_PREFIX}stageweu"
useMssqlElasticPool=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
