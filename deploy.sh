#!/bin/bash
set -e

# =============================================================================
# deploy.sh - Deploy Expense Management App to Azure (without GenAI)
# =============================================================================
# Prerequisites: az cli, jq, python3, .NET 8 SDK, ODBC Driver 18 for SQL Server
#
# Usage:
#   1. Update ADMIN_OBJECT_ID and ADMIN_LOGIN below
#   2. Ensure you are logged in: az login
#   3. Set your resource group: az group create --name rg-expensemgmt-demo --location uksouth
#   4. Run: bash deploy.sh
# =============================================================================

# Configuration - UPDATE THESE
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
ADMIN_OBJECT_ID="YOUR_OBJECT_ID"  # az ad signed-in-user show --query id -o tsv
ADMIN_LOGIN="YOUR_EMAIL"           # your Azure AD email

echo "=============================================="
echo "=== Deploying Infrastructure (No GenAI)   ==="
echo "=============================================="

DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infra/main.bicep \
  --parameters location=$LOCATION adminObjectId=$ADMIN_OBJECT_ID adminLogin=$ADMIN_LOGIN deployGenAI=false \
  --query properties.outputs -o json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFqdn.value')
SQL_DATABASE=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlDatabaseName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_PRINCIPAL_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityPrincipalId.value')
MANAGED_IDENTITY_NAME="mid-AppModAssist-02-03-38"

echo ""
echo "Infrastructure deployed:"
echo "  App Service:      $APP_SERVICE_NAME"
echo "  SQL Server FQDN:  $SQL_SERVER_FQDN"
echo "  Database:         $SQL_DATABASE"
echo "  MI Client ID:     $MANAGED_IDENTITY_CLIENT_ID"

echo ""
echo "=== Configuring App Service Settings ==="
az webapp config appsettings set \
  --name $APP_SERVICE_NAME \
  --resource-group $RESOURCE_GROUP \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_FQDN},1433;Database=${SQL_DATABASE};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};" \
    "ManagedIdentityClientId=${MANAGED_IDENTITY_CLIENT_ID}" \
    "AZURE_CLIENT_ID=${MANAGED_IDENTITY_CLIENT_ID}" \
  --output none

echo "  ✓ App settings configured"

echo ""
echo "=== Waiting 30 seconds for SQL Server to be ready ==="
sleep 30

# Add current IP to SQL firewall
echo "Adding current IP to SQL firewall..."
MY_IP=$(curl -s https://api.ipify.org)
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)

# Allow Azure services access
echo "Allowing Azure services access to SQL Server..."
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowAllAzureIPs" \
    --start-ip-address 0.0.0.0 \
    --end-ip-address 0.0.0.0 \
    --output none

# Add deployment machine IP
az sql server firewall-rule create \
    --resource-group $RESOURCE_GROUP \
    --server $SQL_SERVER_NAME \
    --name "AllowDeploymentIP" \
    --start-ip-address $MY_IP \
    --end-ip-address $MY_IP \
    --output none

echo "  ✓ Firewall rules configured (IP: $MY_IP)"

echo ""
echo "=== Waiting additional 15 seconds for firewall rules to propagate ==="
sleep 15

echo ""
echo "=== Installing Python dependencies ==="
pip3 install --quiet pyodbc azure-identity

echo ""
echo "=== Importing database schema ==="
export SQL_SERVER=$SQL_SERVER_FQDN
export SQL_DATABASE=$SQL_DATABASE
python3 run-sql.py

echo ""
echo "=== Configuring managed identity database roles ==="
# Replace managed identity name placeholder in script.sql (cross-platform)
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak
python3 run-sql-dbrole.py

echo ""
echo "=== Deploying stored procedures ==="
python3 run-sql-stored-procs.py

echo ""
echo "=== Building and packaging application ==="
cd app
dotnet publish -c Release -o ../publish --nologo
cd ../publish
zip -r ../app.zip . -q
cd ..
echo "  ✓ Application packaged"

echo ""
echo "=== Deploying application to App Service ==="
az webapp deploy \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --src-path ./app.zip \
  --type zip \
  --output none

echo "  ✓ Application deployed"

echo ""
echo "=============================================="
echo "=== Deployment Complete!                   ==="
echo "=============================================="
echo ""
echo "  App URL: https://${APP_SERVICE_NAME}.azurewebsites.net/Index"
echo "  API Docs: https://${APP_SERVICE_NAME}.azurewebsites.net/swagger"
echo ""
echo "  NOTE: Navigate to /Index to view the app (not the root URL)"
echo "  NOTE: First startup may take 1-2 minutes"
echo ""
