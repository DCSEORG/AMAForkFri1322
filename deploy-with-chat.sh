#!/bin/bash
set -e

echo "======================================"
echo "Expense Management System Deployment"
echo "WITH GENAI CAPABILITIES"
echo "======================================"
echo ""

# Configuration
RESOURCE_GROUP="rg-expensemgmt-dev"
LOCATION="uksouth"
TIMESTAMP=$(date +%d%H%M)

echo "📋 Configuration:"
echo "  Resource Group: $RESOURCE_GROUP"
echo "  Location: $LOCATION"
echo "  Timestamp: $TIMESTAMP"
echo ""

# Create resource group if it doesn't exist
echo "🔨 Creating resource group..."
az group create \
  --name $RESOURCE_GROUP \
  --location $LOCATION \
  --output none

echo "✅ Resource group created/verified"
echo ""

# Deploy infrastructure (WITH GenAI)
echo "🚀 Deploying infrastructure with GenAI resources..."
echo "   (This may take several minutes)"
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file bicep/main.bicep \
  --parameters environmentSuffix=$TIMESTAMP deployGenAI=true \
  --query 'properties.outputs' \
  --output json)

echo "✅ Infrastructure deployed"
echo ""

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
SQL_SERVER_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerName.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlDatabaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')

echo "📊 Deployed Resources:"
echo "  App Service: $APP_SERVICE_NAME"
echo "  SQL Server: $SQL_SERVER_NAME"
echo "  SQL Database: $SQL_DATABASE_NAME"
echo "  Managed Identity: $MANAGED_IDENTITY_NAME"
echo "  Azure OpenAI: $OPENAI_ENDPOINT"
echo "  Cognitive Search: $SEARCH_ENDPOINT"
echo ""

# Configure App Service settings (including GenAI)
echo "⚙️  Configuring App Service settings..."
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $APP_SERVICE_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER_NAME}.database.windows.net,1433;Database=${SQL_DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};" \
    "ManagedIdentity__ClientId=$MANAGED_IDENTITY_CLIENT_ID" \
    "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
    "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
    "Search__Endpoint=$SEARCH_ENDPOINT" \
  --output none

echo "✅ App Service settings configured"
echo ""

# Import database schema
echo "📦 Importing database schema..."
echo "  Note: Schema import requires Azure AD authentication"
echo "  Running SQL setup script..."

# Update script.sql with managed identity name
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql && rm -f script.sql.bak

# Install required Python packages if not already installed
pip3 install --quiet pyodbc azure-identity

# Update run-sql.py with actual server and database names
sed -i.bak "s/sql-expense-mgmt-xyz.database.windows.net/${SQL_SERVER_NAME}.database.windows.net/g" run-sql.py && rm -f run-sql.py.bak
sed -i.bak "s/ExpenseManagementDB/${SQL_DATABASE_NAME}/g" run-sql.py && rm -f run-sql.py.bak

# Run the Python script
python3 run-sql.py

echo "✅ Database schema imported"
echo ""

# Deploy application code
if [ -f "app.zip" ]; then
  echo "📦 Deploying application code..."
  az webapp deploy \
    --resource-group $RESOURCE_GROUP \
    --name $APP_SERVICE_NAME \
    --src-path ./app.zip \
    --type zip \
    --async true
  
  echo "✅ Application deployment initiated (running asynchronously)"
  echo ""
else
  echo "⚠️  app.zip not found. Skipping application deployment."
  echo "   Build the application and create app.zip, then deploy manually with:"
  echo "   az webapp deploy --resource-group $RESOURCE_GROUP --name $APP_SERVICE_NAME --src-path ./app.zip"
  echo ""
fi

# Summary
echo "======================================"
echo "✅ Deployment Complete!"
echo "======================================"
echo ""
echo "🌐 Application URL: ${APP_SERVICE_URL}/Index"
echo "💬 Chat UI URL: ${APP_SERVICE_URL}/Chat"
echo ""
echo "📝 Note: Navigate to /Index to view the application"
echo ""
echo "🔐 Database Connection:"
echo "   Server: ${SQL_SERVER_NAME}.database.windows.net"
echo "   Database: $SQL_DATABASE_NAME"
echo "   Authentication: Azure AD with Managed Identity"
echo ""
echo "🤖 GenAI Resources:"
echo "   Azure OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "   Model Deployment: $OPENAI_MODEL_NAME"
echo "   Search Endpoint: $SEARCH_ENDPOINT"
echo ""
