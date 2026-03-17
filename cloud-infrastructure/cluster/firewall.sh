IP_ADDRESS=$(curl -sf https://api.ipify.org)
if [[ -z "$IP_ADDRESS" ]] || ! [[ "$IP_ADDRESS" =~ ^[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$ ]]; then
    echo "ERROR: Failed to resolve public IP address"
    exit 1
fi
FIREWALL_RULE_NAME="github-action-${DATABASE_NAME}"

if [[ "$1" == "open" ]]
then
    echo "$(date +"%Y-%m-%dT%H:%M:%S") Add the IP $IP_ADDRESS to the PostgreSQL server firewall on server $POSTGRES_SERVER_NAME for database $DATABASE_NAME"
    az postgres flexible-server firewall-rule create --resource-group $CLUSTER_RESOURCE_GROUP_NAME --name $POSTGRES_SERVER_NAME --rule-name "$FIREWALL_RULE_NAME" --start-ip-address $IP_ADDRESS --end-ip-address $IP_ADDRESS
else
    echo "$(date +"%Y-%m-%dT%H:%M:%S") Delete the IP $IP_ADDRESS from the PostgreSQL server firewall on server $POSTGRES_SERVER_NAME for database $DATABASE_NAME"
    az postgres flexible-server firewall-rule delete --resource-group $CLUSTER_RESOURCE_GROUP_NAME --name $POSTGRES_SERVER_NAME --rule-name "$FIREWALL_RULE_NAME" --yes
fi
