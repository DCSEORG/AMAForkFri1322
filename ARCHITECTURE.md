# Azure Architecture Diagram

## Expense Management System - Cloud Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                         Azure Cloud                                  │
│                                                                       │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                    Resource Group: rg-expensemgmt-dev          │  │
│  │                                                                 │  │
│  │  ┌──────────────────────┐                                      │  │
│  │  │  User-Assigned       │                                      │  │
│  │  │  Managed Identity    │──────────┐                           │  │
│  │  │  mid-ExpenseMgmt-    │          │                           │  │
│  │  │  [timestamp]         │          │                           │  │
│  │  └──────────────────────┘          │                           │  │
│  │           │                         │                           │  │
│  │           │ assigned to             │ authenticates             │  │
│  │           │                         │                           │  │
│  │  ┌────────▼────────────┐           │                           │  │
│  │  │  Azure App Service  │           │                           │  │
│  │  │  Linux + .NET 8.0   │           │                           │  │
│  │  │  Basic SKU (B1)     │           │                           │  │
│  │  │                     │           │                           │  │
│  │  │  • Razor Pages UI   │           │                           │  │
│  │  │  • REST APIs        │───────────┼────────────┐              │  │
│  │  │  • Swagger Docs     │           │            │              │  │
│  │  │  • Chat UI          │           │            │              │  │
│  │  └─────────────────────┘           │            │              │  │
│  │           │                         │            │              │  │
│  │           │ connects using          │            │ uses         │  │
│  │           │ managed identity        │            │              │  │
│  │           │                         │            │              │  │
│  │  ┌────────▼────────────┐           │   ┌────────▼──────────┐  │  │
│  │  │  Azure SQL Database │◄──────────┘   │  Azure OpenAI     │  │  │
│  │  │  Basic Tier         │               │  S0 SKU           │  │  │
│  │  │                     │               │  Sweden Central   │  │  │
│  │  │  • ExpenseMgmtDB    │               │                   │  │  │
│  │  │  • AAD-only Auth    │               │  • GPT-4o model   │  │  │
│  │  │  • Managed Identity │               │  • Function calls │  │  │
│  │  │    has db_reader/   │               └───────────────────┘  │  │
│  │  │    writer roles     │                         │            │  │
│  │  └─────────────────────┘                         │ indexes    │  │
│  │                                                   │ context    │  │
│  │                                          ┌────────▼──────────┐ │  │
│  │                                          │ Azure Cognitive   │ │  │
│  │                                          │ Search            │ │  │
│  │                                          │ Basic Tier        │ │  │
│  │                                          │                   │ │  │
│  │                                          │ • RAG support     │ │  │
│  │                                          └───────────────────┘ │  │
│  └─────────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────────┘

                    ┌──────────────────┐
                    │   Deployment     │
                    │   Scripts        │
                    │                  │
                    │  • deploy.sh     │
                    │    (no GenAI)    │
                    │                  │
                    │  • deploy-with-  │
                    │    chat.sh       │
                    │    (with GenAI)  │
                    └──────────────────┘
```

## Component Descriptions

### 1. **User-Assigned Managed Identity**
   - Provides secure, passwordless authentication
   - Assigned to App Service
   - Grants permissions to Azure SQL and Azure OpenAI

### 2. **Azure App Service (Linux + .NET 8.0)**
   - Hosts the Expense Management web application
   - **Features:**
     - Modern Razor Pages UI for expense management
     - RESTful APIs with Swagger documentation
     - AI-powered chat interface
   - **SKU:** Basic B1 (cost-effective for development)

### 3. **Azure SQL Database**
   - Stores all expense data, categories, users, and statuses
   - **Security:**
     - Azure AD-only authentication (no SQL auth)
     - Managed Identity access
     - Firewall rules for Azure services
   - **Tier:** Basic (5 DTUs, 2GB)

### 4. **Azure OpenAI Service** (Optional - deployed with deploy-with-chat.sh)
   - Provides AI capabilities for natural language interaction
   - **Model:** GPT-4o (deployed in Sweden Central region)
   - **Features:**
     - Function calling to interact with expense APIs
     - Natural language querying and data manipulation

### 5. **Azure Cognitive Search** (Optional - deployed with deploy-with-chat.sh)
   - Enables Retrieval-Augmented Generation (RAG)
   - Indexes contextual information for better AI responses
   - **Tier:** Basic

## Authentication Flow

```
User → App Service → Uses Managed Identity → Connects to Azure SQL
                  ↓
           Uses Managed Identity → Calls Azure OpenAI
```

## Data Flow

1. **User Action:** User interacts with web UI or AI chat
2. **API Call:** Frontend calls REST API endpoint
3. **Database Query:** App Service queries Azure SQL using Managed Identity
4. **AI Processing (if enabled):** AI chat uses function calling to invoke APIs
5. **Response:** Data returned to user through UI

## Deployment Options

### Standard Deployment (deploy.sh)
- Deploys: App Service, SQL Database, Managed Identity
- No AI capabilities
- Chat UI shows dummy responses

### Full Deployment with AI (deploy-with-chat.sh)
- Deploys: Everything in standard + Azure OpenAI + Cognitive Search
- Full AI chat capabilities with function calling
- Natural language expense management

## Security Features

- ✅ Azure AD-only authentication for SQL
- ✅ Managed Identity (no stored credentials)
- ✅ HTTPS only
- ✅ Firewall rules
- ✅ Role-based access control (RBAC)
