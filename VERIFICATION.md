# Deployment Verification Checklist

Use this checklist to verify your deployment was successful and all components are working correctly.

## Pre-Deployment Checks

- [ ] Azure CLI installed and logged in (`az login`)
- [ ] Correct subscription selected (`az account show`)
- [ ] Python 3.8+ installed (`python3 --version`)
- [ ] Repository cloned locally
- [ ] In the repository root directory

## Deployment Process

- [ ] Ran deployment script (`./deploy.sh` or `./deploy-with-chat.sh`)
- [ ] No errors during deployment
- [ ] Noted the application URL from deployment output
- [ ] Noted the resource group name: `_________________`
- [ ] Noted the app service name: `_________________`

## Infrastructure Verification

### Resource Group
```bash
az group show --name rg-expensemgmt-dev
```
- [ ] Resource group exists
- [ ] Location is correct (uksouth)

### App Service
```bash
az webapp show --name <app-name> --resource-group <rg-name>
```
- [ ] App Service running (state: Running)
- [ ] Managed Identity assigned
- [ ] HTTPS only enabled

### SQL Database
```bash
az sql db show --name ExpenseManagementDB --server <sql-server> --resource-group <rg-name>
```
- [ ] Database exists
- [ ] Basic SKU configured
- [ ] Azure AD authentication enabled

### Managed Identity
```bash
az identity show --name <managed-identity-name> --resource-group <rg-name>
```
- [ ] Identity exists
- [ ] Has correct role assignments

### GenAI Resources (if deployed with-chat)
```bash
az cognitiveservices account show --name <openai-name> --resource-group <rg-name>
az search service show --name <search-name> --resource-group <rg-name>
```
- [ ] Azure OpenAI service deployed
- [ ] Model deployment exists (gpt-4o)
- [ ] Cognitive Search service deployed
- [ ] Managed Identity has OpenAI user role

## Application Verification

### Basic Functionality

**Home Page** - `https://<app-url>/Index`
- [ ] Page loads without errors
- [ ] Can see list of expenses
- [ ] Filter box works
- [ ] Modern UI with gradient header
- [ ] No database connection error banner (or shows dummy data with error)

**Add Expense** - `https://<app-url>/AddExpense`
- [ ] Page loads
- [ ] Form has Amount, Date, Category, Description fields
- [ ] Categories dropdown populated
- [ ] Can submit form
- [ ] Success message appears

**Approve Expenses** - `https://<app-url>/ApproveExpenses`
- [ ] Page loads
- [ ] Shows pending expenses
- [ ] Can approve/reject expenses
- [ ] Status updates correctly

**API Documentation** - `https://<app-url>/swagger`
- [ ] Swagger UI loads
- [ ] All API endpoints listed:
  - [ ] GET /api/expenses
  - [ ] GET /api/expenses/{id}
  - [ ] POST /api/expenses
  - [ ] PUT /api/expenses/{id}/status
  - [ ] GET /api/expenses/pending
  - [ ] GET /api/categories
- [ ] Can test API endpoints

**AI Chat** - `https://<app-url>/Chat`
- [ ] Page loads
- [ ] Chat interface displayed
- [ ] Can send messages
- [ ] Receives responses (dummy or AI)
- [ ] Shows warning if GenAI not deployed (expected)

### Database Connectivity

If you see "Database Error" banner:
```bash
# Check managed identity roles
az sql server ad-admin show --server <sql-server> --resource-group <rg-name>

# Run SQL setup manually if needed
python3 run-sql.py.tmp  # After creating temp file with correct settings
```

- [ ] No database errors, OR
- [ ] Dummy data displayed with clear error message

### API Testing

Test via Swagger UI or curl:

```bash
# Get all expenses
curl https://<app-url>/api/expenses

# Get categories
curl https://<app-url>/api/categories

# Create expense (should return 201)
curl -X POST https://<app-url>/api/expenses \
  -H "Content-Type: application/json" \
  -d '{"amount":50,"date":"2024-11-21","categoryId":1,"description":"Test"}'
```

- [ ] GET /api/expenses returns data
- [ ] GET /api/categories returns categories
- [ ] POST /api/expenses creates expense (if DB connected)
- [ ] All responses have correct format

### AI Chat Testing (if deployed with-chat)

Try these prompts:
- [ ] "Show me all expenses" - returns expense list
- [ ] "List pending expenses" - returns pending items
- [ ] "What categories are available?" - returns categories
- [ ] "Help" - returns help information

## Performance Checks

### Response Times
- [ ] Home page loads in < 3 seconds
- [ ] API responses < 1 second
- [ ] Chat responses < 5 seconds (for AI)

### Logs
```bash
az webapp log tail --name <app-name> --resource-group <rg-name>
```
- [ ] No critical errors in logs
- [ ] Database connections successful
- [ ] No authentication failures

## Security Verification

### App Service
```bash
az webapp config show --name <app-name> --resource-group <rg-name>
```
- [ ] httpsOnly: true
- [ ] minTlsVersion: "1.2"
- [ ] ftpsState: "Disabled"

### SQL Database
```bash
az sql server show --name <sql-server> --resource-group <rg-name>
```
- [ ] publicNetworkAccess: "Enabled" (but restricted to Azure)
- [ ] Azure AD admin configured
- [ ] Managed Identity has db roles

### Configuration Secrets
```bash
az webapp config appsettings list --name <app-name> --resource-group <rg-name>
```
- [ ] No API keys or passwords in settings
- [ ] Connection string uses Managed Identity
- [ ] OpenAI settings present (if deployed with-chat)

## Cost Monitoring

```bash
# Check current month spending
az consumption usage list --start-date $(date -d "1 day ago" +%Y-%m-%d) --end-date $(date +%Y-%m-%d)
```

- [ ] Resource costs within expected range
- [ ] No unexpected services deployed
- [ ] All resources in correct SKU/tier

## Common Issues & Solutions

### Issue: Database Connection Errors

**Symptoms:** Error banner on pages, dummy data displayed

**Solutions:**
- [ ] Check SQL firewall allows Azure services
- [ ] Verify managed identity has db_datareader and db_datawriter roles
- [ ] Run `python3 run-sql.py` manually
- [ ] Check SQL admin is configured with your user

### Issue: 500 Internal Server Error

**Symptoms:** App loads but shows error pages

**Solutions:**
- [ ] Check app logs: `az webapp log tail`
- [ ] Verify app settings are configured
- [ ] Restart app: `az webapp restart`
- [ ] Redeploy app.zip

### Issue: Chat Not Working

**Symptoms:** Chat shows errors or dummy responses

**Solutions:**
- [ ] Verify deployed with `deploy-with-chat.sh`
- [ ] Check OpenAI__Endpoint in app settings
- [ ] Verify managed identity has OpenAI user role
- [ ] Check Azure OpenAI quota not exceeded

### Issue: Slow Performance

**Symptoms:** Pages load slowly, timeouts

**Solutions:**
- [ ] Check App Service SKU (consider upgrading from B1)
- [ ] Check SQL database DTUs
- [ ] Enable Application Insights for diagnostics
- [ ] Review database query performance

## Clean Up (When Done Testing)

If you want to remove everything:

```bash
# WARNING: This deletes all resources and data!
az group delete --name rg-expensemgmt-dev --yes --no-wait
```

- [ ] Confirmed all important data backed up
- [ ] Resource group deleted
- [ ] Verified deletion in Azure Portal

## Next Steps

Once verified:

- [ ] Configure custom domain (optional)
- [ ] Set up SSL certificate
- [ ] Configure CI/CD pipeline
- [ ] Enable Application Insights
- [ ] Set up monitoring alerts
- [ ] Configure backup policies
- [ ] Review and adjust SKUs for production

## Notes

Add any observations or issues encountered:

```
Date: ___________
Deployed by: ___________

Notes:
_______________________________________
_______________________________________
_______________________________________
```

---

**Status:** 
- [ ] ✅ All checks passed - Deployment successful!
- [ ] ⚠️ Some issues - Needs attention
- [ ] ❌ Major issues - Review deployment

**Overall Result:** ___________
