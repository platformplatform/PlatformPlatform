environment="production"
location="WestEurope"
locationPrefix="west-europe"
clusterUniqueName="${UNIQUE_CLUSTER_PREFIX}prodweu"
useMssqlElasticPool=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
