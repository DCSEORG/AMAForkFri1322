# Deployment Guide

This guide provides step-by-step instructions for deploying the Expense Management System to Azure.

## Prerequisites

Before you begin, ensure you have:

- ✅ Active Azure subscription
- ✅ Azure CLI installed (version 2.50.0 or later)
- ✅ Contributor or Owner access to the subscription
- ✅ Bash shell (Linux, macOS, WSL, or Git Bash on Windows)
- ✅ Python 3.8+ (for database setup)

## Quick Start

### Step 1: Login to Azure

```bash
az login
```

### Step 2: Set Your Subscription

```bash
# List available subscriptions
az account list --output table

# Set the subscription you want to use
az account set --subscription "your-subscription-id-or-name"

# Verify the correct subscription is selected
az account show
```

### Step 3: Choose Your Deployment

#### Option A: Standard Deployment (Recommended for testing)

Deploy without AI capabilities:

```bash
./deploy.sh
```

**What gets deployed:**
- Azure App Service (Basic B1)
- Azure SQL Database (Basic tier)
- User-assigned Managed Identity
- Expense Management Application

**Time:** ~5-10 minutes  
**Cost:** ~£44/month

#### Option B: Full Deployment with AI

Deploy with AI chat capabilities:

```bash
./deploy-with-chat.sh
```

**What gets deployed:**
- Everything from Option A, PLUS:
- Azure OpenAI Service (GPT-4o)
- Azure Cognitive Search (Basic tier)

**Time:** ~10-15 minutes  
**Cost:** ~£104/month + OpenAI usage

## Post-Deployment

### Accessing Your Application

After deployment completes, you'll see output similar to:

```
✅ Deployment Complete!
🌐 Application URL: https://app-expensemgmt-211342.azurewebsites.net/Index
```

**Important:** Navigate to `/Index` path, not just the root URL!

### Testing the Application

1. **View Expenses**
   - Navigate to: `https://your-app-url/Index`
   - You should see a list of sample expenses

2. **Add New Expense**
   - Click "Add Expense" button
   - Fill in the form and submit

3. **Approve Expenses**
   - Navigate to "Approve" in the menu
   - See pending expenses and approve/reject them

4. **Test APIs**
   - Navigate to: `https://your-app-url/swagger`
   - Try out the API endpoints interactively

5. **Try AI Chat** (if deployed with-chat)
   - Navigate to "AI Chat Assistant" in the menu
   - Ask: "Show me all expenses"
   - Ask: "List pending expenses"
   - Ask: "What categories are available?"

## Configuration

### Resource Group

The default resource group name is `rg-expensemgmt-dev`. To change it:

```bash
# Edit deploy.sh or deploy-with-chat.sh
RESOURCE_GROUP="your-custom-name"
```

### Location

The default location is `uksouth`. To change it:

```bash
# Edit deploy.sh or deploy-with-chat.sh
LOCATION="your-preferred-region"
```

### SKUs and Tiers

To adjust costs, you can modify the Bicep files:

**App Service:** `bicep/app-service.bicep`
```bicep
sku: {
  name: 'B1'  // Change to F1 (Free) or P1V2 (Premium)
}
```

**SQL Database:** `bicep/sql.bicep`
```bicep
sku: {
  name: 'Basic'  // Change to S0 (Standard) or higher
}
```

## Troubleshooting

### Deployment Fails

**Issue:** "Subscription not registered for resource provider"
```bash
# Register required providers
az provider register --namespace Microsoft.Web
az provider register --namespace Microsoft.Sql
az provider register --namespace Microsoft.ManagedIdentity
az provider register --namespace Microsoft.CognitiveServices  # For AI deployment
az provider register --namespace Microsoft.Search  # For AI deployment
```

**Issue:** "Location not available"
```bash
# Check available locations for a resource
az account list-locations --query "[].name" -o table

# Verify resource availability
az provider show --namespace Microsoft.CognitiveServices --query "resourceTypes[?resourceType=='accounts'].locations"
```

### Application Errors

**Issue:** "Database connection error" banner appears

**Solution:**
1. Check if SQL Server firewall allows Azure services
2. Verify Managed Identity has correct roles:
```bash
# Check role assignments
az role assignment list --assignee <managed-identity-client-id> --all
```

3. Run the SQL setup script manually:
```bash
# Get connection details from deployment output
SQL_SERVER_NAME="your-server-name"
SQL_DATABASE_NAME="ExpenseManagementDB"
MANAGED_IDENTITY_NAME="your-identity-name"

# Update and run
sed "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql > script-tmp.sql
python3 run-sql.py  # After updating SERVER and DATABASE in the file
```

**Issue:** AI Chat not working

**Solution:**
1. Verify you deployed with `deploy-with-chat.sh`
2. Check App Service settings:
```bash
az webapp config appsettings list \
  --name <app-name> \
  --resource-group <resource-group> \
  --query "[?name=='OpenAI__Endpoint']"
```

3. If missing, redeploy with `deploy-with-chat.sh`

### Permission Issues

**Issue:** "Insufficient permissions to complete operation"

**Solution:**
1. Verify your role:
```bash
az role assignment list --assignee $(az ad signed-in-user show --query id -o tsv) --all
```

2. Request Contributor or Owner role from subscription admin

3. Alternatively, deploy to a resource group where you have permissions

## Manual Deployment Steps

If you prefer to deploy manually or understand what the scripts do:

### 1. Create Resource Group
```bash
az group create --name rg-expensemgmt-dev --location uksouth
```

### 2. Deploy Infrastructure
```bash
az deployment group create \
  --resource-group rg-expensemgmt-dev \
  --template-file bicep/main.bicep \
  --parameters environmentSuffix=$(date +%d%H%M) deployGenAI=false \
               sqlAdminObjectId=$(az ad signed-in-user show --query id -o tsv) \
               sqlAdminLogin=$(az ad signed-in-user show --query userPrincipalName -o tsv)
```

### 3. Configure App Settings
```bash
APP_NAME="app-expensemgmt-211342"  # Replace with your app name
SQL_SERVER="sql-expensemgmt-211342"  # Replace with your SQL server name

az webapp config appsettings set \
  --resource-group rg-expensemgmt-dev \
  --name $APP_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:${SQL_SERVER}.database.windows.net,1433;Database=ExpenseManagementDB;Authentication=Active Directory Managed Identity;"
```

### 4. Deploy Application
```bash
az webapp deploy \
  --resource-group rg-expensemgmt-dev \
  --name $APP_NAME \
  --src-path app.zip \
  --type zip
```

### 5. Setup Database
```bash
pip3 install pyodbc azure-identity
python3 run-sql.py  # After configuring with your server details
```

## Clean Up

To remove all deployed resources:

```bash
# Delete the resource group and all its resources
az group delete --name rg-expensemgmt-dev --yes --no-wait
```

**Warning:** This action is irreversible and will delete all data!

## Cost Optimization

### For Development/Testing

- Use Free/Shared tier App Service (F1)
- Use Basic SQL Database tier
- Skip GenAI deployment initially
- Delete resources when not in use

### For Production

- Use Standard or Premium App Service
- Enable Auto-scaling
- Use Standard SQL Database with performance insights
- Consider Azure Cost Management alerts

## Monitoring

### View Application Logs
```bash
az webapp log tail \
  --resource-group rg-expensemgmt-dev \
  --name <app-name>
```

### View Metrics
```bash
az monitor metrics list \
  --resource <app-resource-id> \
  --metric "Http2xx" "Http5xx" "ResponseTime"
```

### Enable Application Insights
```bash
# Create Application Insights
az monitor app-insights component create \
  --app expensemgmt-insights \
  --location uksouth \
  --resource-group rg-expensemgmt-dev

# Link to App Service
az webapp config appsettings set \
  --name <app-name> \
  --resource-group rg-expensemgmt-dev \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="<connection-string>"
```

## Next Steps

After successful deployment:

1. ✅ Test all functionality
2. ✅ Review security settings
3. ✅ Set up monitoring and alerts
4. ✅ Configure custom domain (optional)
5. ✅ Enable SSL/TLS certificates
6. ✅ Configure backup policies
7. ✅ Set up CI/CD pipeline

## Support

For issues or questions:
- Check [ARCHITECTURE.md](./ARCHITECTURE.md) for system design
- Review [GenAISettings.md](./GenAISettings.md) for AI configuration
- Consult [README.md](./README.md) for feature documentation

## Additional Resources

- [Azure App Service Documentation](https://learn.microsoft.com/azure/app-service/)
- [Azure SQL Database Documentation](https://learn.microsoft.com/azure/azure-sql/)
- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Managed Identity Documentation](https://learn.microsoft.com/azure/active-directory/managed-identities-azure-resources/)
