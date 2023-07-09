ENVIRONMENT="testing"
LOCATION="WestEurope"
LOCATION_PREFIX="west-europe"
CLUSTER_UNIQUE_NAME="${UNIQUE_CLUSTER_PREFIX}testweu"
USE_MSSQL_ELASTIC_POOL=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy-cluster.sh
