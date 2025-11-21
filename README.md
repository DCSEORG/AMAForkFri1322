![Header image](https://github.com/DougChisholm/App-Mod-Assist/blob/main/repo-header.png)

# Expense Management System

A modern, cloud-native expense management application built on Azure. This project demonstrates how to modernize legacy applications using Azure App Service, Azure SQL Database with managed identity authentication, and optional AI capabilities with Azure OpenAI.

## Features

✅ **Modern Web UI** - Clean, responsive interface built with ASP.NET Core Razor Pages and Bootstrap 5  
✅ **REST APIs** - Full RESTful API with Swagger documentation  
✅ **Azure SQL Database** - Secure database with Azure AD-only authentication  
✅ **Managed Identity** - Passwordless authentication for maximum security  
✅ **AI Chat Assistant** - Optional natural language interface powered by Azure OpenAI (with function calling)  
✅ **Infrastructure as Code** - Complete Bicep templates for Azure resources  
✅ **One-Command Deployment** - Deploy everything with a single script  

## Screenshots

See the `Legacy-Screenshots` folder for the original application that this modernizes.

## Quick Start

### Prerequisites

- Azure subscription
- Azure CLI installed and logged in (`az login`)
- Appropriate permissions to create resources in your subscription

### Option 1: Standard Deployment (Without AI)

Deploy the core expense management system:

```bash
# 1. Set your subscription
az account set --subscription "your-subscription-id"

# 2. Run the deployment script
./deploy.sh
```

This deploys:
- Azure App Service (Basic B1 SKU)
- Azure SQL Database (Basic tier) with Azure AD-only auth
- User-assigned Managed Identity
- Application code with REST APIs and Swagger

**Access the application:**
- Main App: `https://app-expensemgmt-[timestamp].azurewebsites.net/Index`
- API Docs: `https://app-expensemgmt-[timestamp].azurewebsites.net/swagger`
- Chat UI: Available but with dummy responses

### Option 2: Full Deployment (With AI Chat)

Deploy everything including Azure OpenAI for AI-powered chat:

```bash
# 1. Set your subscription
az account set --subscription "your-subscription-id"

# 2. Run the deployment script with AI services
./deploy-with-chat.sh
```

This deploys everything from Option 1 PLUS:
- Azure OpenAI Service (GPT-4o model in Sweden Central)
- Azure Cognitive Search (for RAG capabilities)
- Fully functional AI chat with natural language expense management

**Note:** The URL to view the app is `<app-url>/Index` - don't just navigate to the root URL.

## Architecture

See [ARCHITECTURE.md](./ARCHITECTURE.md) for a detailed architecture diagram and component descriptions.

```
┌─────────────────┐
│ User-Assigned   │
│ Managed Identity├──────────┐
└─────────────────┘          │
                              │
        ┌─────────────────────▼───────────┐
        │   Azure App Service             │
        │   (ASP.NET Core 8.0)            │
        │                                 │
        │   • Expense Management UI       │
        │   • REST APIs + Swagger         │
        │   • AI Chat Interface           │
        └────────┬───────────┬────────────┘
                 │           │
                 │           │
     ┌───────────▼──┐   ┌────▼──────────┐
     │ Azure SQL DB │   │ Azure OpenAI  │
     │ (AAD Auth)   │   │ (GPT-4o)      │
     └──────────────┘   └───────────────┘
```

## Application Features

### Expense Management
- **Add Expenses**: Create new expense entries with amount, date, category, and description
- **View Expenses**: List all expenses with filtering capabilities
- **Approve/Reject**: Manager workflow for expense approval
- **Status Tracking**: Draft, Submitted, Approved, Rejected states

### REST API
- `GET /api/expenses` - Get all expenses (with optional filters)
- `POST /api/expenses` - Create a new expense
- `PUT /api/expenses/{id}/status` - Update expense status
- `GET /api/expenses/pending` - Get pending expenses for approval
- `GET /api/categories` - Get expense categories

Access Swagger UI at `/swagger` for interactive API documentation.

### AI Chat Assistant (Optional)
When deployed with AI services, the chat assistant can:
- Answer questions about expenses in natural language
- Retrieve expense information
- Create new expenses through conversation
- Filter and search expenses
- Provide summaries and insights

## Database Schema

The application uses the expense management schema from `Database-Schema/database_schema.sql`:

- **Users**: Employee and Manager roles
- **Expenses**: Expense records with amounts in minor units (pence)
- **ExpenseCategories**: Travel, Meals, Supplies, Accommodation, Other
- **ExpenseStatus**: Draft, Submitted, Approved, Rejected
- **Roles**: Employee, Manager

## Security Features

- ✅ **Azure AD-only authentication** for SQL Database (no SQL passwords)
- ✅ **Managed Identity** for passwordless service authentication
- ✅ **HTTPS only** enforcement
- ✅ **Role-based access** to Azure resources
- ✅ **Firewall rules** for network security
- ✅ **TLS 1.2+** minimum encryption

## Configuration

Application settings are configured automatically by the deployment scripts:

```json
{
  "ConnectionStrings__DefaultConnection": "Server=...; Authentication=Active Directory Managed Identity; ...",
  "ManagedIdentity__ClientId": "...",
  "OpenAI__Endpoint": "...",  // Only with deploy-with-chat.sh
  "OpenAI__DeploymentName": "gpt-4o"  // Only with deploy-with-chat.sh
}
```

## Development

### Local Development

1. Clone the repository
2. Navigate to `ExpenseApp/ExpenseManagement`
3. Update `appsettings.Development.json` with your connection string
4. Run: `dotnet run`

### Building the Application

```bash
cd ExpenseApp/ExpenseManagement
dotnet build
dotnet publish -c Release -o ../../publish
```

### Creating Deployment Package

```bash
cd publish
zip -r ../app.zip .
```

## Cost Estimation (Monthly)

### Standard Deployment (No AI)
- App Service Basic B1: ~£40
- Azure SQL Basic: ~£4
- Total: ~£44/month

### Full Deployment (With AI)
- App Service Basic B1: ~£40
- Azure SQL Basic: ~£4
- Azure OpenAI S0: Pay-per-token (varies with usage)
- Cognitive Search Basic: ~£60
- Total: ~£104/month + OpenAI usage

*Prices are estimates in GBP for UK South region. Actual costs may vary.*

## Troubleshooting

### Database Connection Errors
If you see database connection errors, the app will automatically fall back to dummy data and display an error banner. Check:
1. Managed Identity is correctly assigned
2. SQL Server firewall allows Azure services
3. Managed Identity has `db_datareader` and `db_datawriter` roles

### AI Chat Not Working
The chat will show a warning if GenAI services aren't deployed. To enable:
1. Run `./deploy-with-chat.sh` instead of `./deploy.sh`
2. Ensure Azure OpenAI quota is available in your subscription
3. Check the deployed model name matches configuration

## File Structure

```
├── bicep/                  # Infrastructure as Code
│   ├── main.bicep         # Main orchestration
│   ├── app-service.bicep  # App Service + Managed Identity
│   ├── sql.bicep          # Azure SQL Database
│   └── genai.bicep        # Azure OpenAI + Cognitive Search
├── ExpenseApp/            # Application code
│   └── ExpenseManagement/ # ASP.NET Core project
│       ├── Pages/         # Razor Pages UI
│       ├── Controllers/   # API Controllers
│       ├── Models/        # Data models
│       └── Services/      # Database service
├── Database-Schema/       # SQL schema files
├── deploy.sh             # Standard deployment script
├── deploy-with-chat.sh   # Full deployment with AI
├── run-sql.py            # SQL script runner
├── script.sql            # Managed Identity setup
├── app.zip               # Deployment package
└── ARCHITECTURE.md       # Detailed architecture diagram
```

## Contributing

This project was created as a demonstration of modernizing legacy applications with Azure and AI. Feel free to fork and adapt for your own needs.

## License

See LICENSE file for details.

---

**Note for Collaborators:** Fork this repo before running the coding agent to avoid polluting the base template. Use a name like "AMA-FridayTest01" instead of "App-Mod-Assist".
