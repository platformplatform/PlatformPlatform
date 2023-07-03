environment="production"
location="EastUS"
locationPrefix="east-us"
clusterUniqueName="p14mprodeus"
useMssqlElasticPool=false
acrName="platformplatform"

cd "$(dirname "${BASH_SOURCE[0]}")"
. ../deploy.sh
