environment="testing"
location="EastUS"
locationPrefix="east-us"
clusterUniqueName="mentumtesteus"
useMssqlElasticPool=false

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
