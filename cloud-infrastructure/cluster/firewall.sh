IP_ADDRESS=$(curl -s https://api.ipify.org)
FIREWALL_RULE_NAME="GitHub Action Workflows - On active when deploying"

if [[ "$1" == "open" ]]
then
    echo "Add the IP $IP_ADDRESS to the SQL Server firewall on server $SQL_SERVER_NAME"
    az sql server firewall-rule create --resource-group $RESOURCE_GROUP_NAME --server $SQL_SERVER_NAME --name "$FIREWALL_RULE_NAME" --start-ip-address $IP_ADDRESS --end-ip-address $IP_ADDRESS
else
    echo "Delete the IP $IP_ADDRESS from the SQL Server firewall on server $SQL_SERVER_NAME"
    az sql server firewall-rule delete --resource-group $RESOURCE_GROUP_NAME --server $SQL_SERVER_NAME --name "$FIREWALL_RULE_NAME"
fi
