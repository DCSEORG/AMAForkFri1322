# Azure OpenAI Configuration

This document describes the Azure OpenAI setup for the Expense Management System.

## Resources Created

When deploying with `deploy-with-chat.sh`, the following GenAI resources are created:

### 1. Azure OpenAI Service
- **Name:** `oai-expensemgmt-[timestamp]`
- **Region:** Sweden Central (swedencentral)
- **SKU:** S0 (Standard)
- **Reason for Region:** GPT-4o model availability

### 2. Model Deployment
- **Model:** GPT-4o
- **Deployment Name:** gpt-4o
- **Version:** 2024-08-06
- **Capacity:** 10K tokens per minute

### 3. Azure Cognitive Search
- **Name:** `srch-expensemgmt-[timestamp]`
- **Region:** UK South (same as App Service)
- **SKU:** Basic
- **Purpose:** RAG (Retrieval-Augmented Generation) support

## Authentication

The application uses **Managed Identity** to authenticate with Azure OpenAI:

- No API keys stored in configuration
- User-assigned Managed Identity has the **"Cognitive Services OpenAI User"** role
- Managed Identity also has **"Search Index Data Contributor"** role for Azure Cognitive Search

## Application Configuration

After deployment, these settings are automatically configured in App Service:

```json
{
  "OpenAI__Endpoint": "https://oai-expensemgmt-[timestamp].openai.azure.com/",
  "OpenAI__DeploymentName": "gpt-4o"
}
```

## Function Calling

The chat interface uses OpenAI function calling to interact with the expense management system:

### Available Functions

1. **get_expenses**
   - Retrieves expenses from the database
   - Parameters:
     - `statusId` (optional): Filter by status (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)
     - `userId` (optional): Filter by user ID
   - Returns: List of expense objects

2. **create_expense**
   - Creates a new expense
   - Parameters:
     - `amount` (required): Expense amount in GBP
     - `date` (required): Expense date (YYYY-MM-DD format)
     - `categoryId` (required): Category ID (1=Travel, 2=Meals, 3=Supplies, 4=Accommodation, 5=Other)
     - `description` (optional): Expense description
   - Returns: New expense ID and success status

3. **get_categories**
   - Gets available expense categories
   - Parameters: None
   - Returns: List of category objects

## Usage Examples

### Example Conversations

**User:** "Show me all submitted expenses"
- AI calls `get_expenses(statusId=2)`
- Returns list of expenses with Submitted status
- AI formats response in natural language

**User:** "Create a new travel expense for £50 on 2024-11-20"
- AI calls `create_expense(amount=50, date="2024-11-20", categoryId=1, description="Travel expense")`
- Returns new expense ID
- AI confirms creation

**User:** "What categories can I use?"
- AI calls `get_categories()`
- Returns list of categories
- AI presents them in a user-friendly format

## RAG Implementation

Azure Cognitive Search enables Retrieval-Augmented Generation (RAG):

1. **Indexing**: Documents and contextual information are indexed in Azure Cognitive Search
2. **Query**: When users ask questions, relevant context is retrieved from the search index
3. **Augmentation**: Retrieved context is added to the AI prompt
4. **Generation**: GPT-4o generates responses with accurate, context-aware information

## Token Usage and Costs

### GPT-4o Pricing (as of Nov 2024)
- Input tokens: ~$0.03 per 1K tokens
- Output tokens: ~$0.06 per 1K tokens

### Typical Conversation Costs
- Simple query: ~500 tokens ≈ $0.015
- Complex conversation: ~2000 tokens ≈ $0.06
- With function calling: +20-30% tokens

### Cost Optimization
- Use streaming for long responses
- Cache common queries
- Set appropriate token limits (currently 10K TPM)

## Security Considerations

1. **No Secrets in Code**: All authentication uses Managed Identity
2. **Role-Based Access**: Principle of least privilege applied
3. **Network Security**: Resources deployed in same Azure region where possible
4. **Data Privacy**: No customer data leaves Azure
5. **Audit Logging**: All OpenAI calls are logged in Azure

## Monitoring

Monitor your Azure OpenAI usage:

```bash
# View OpenAI metrics in Azure Portal
az monitor metrics list \
  --resource /subscriptions/{subscription}/resourceGroups/{rg}/providers/Microsoft.CognitiveServices/accounts/{openai-name} \
  --metric "ActiveTokens" "GeneratedTokens" "ProcessedPromptTokens"
```

## Troubleshooting

### Chat Returns Errors

**Symptom:** Chat shows error messages
**Solution:**
1. Verify OpenAI endpoint is configured: `az webapp config appsettings list -n <app-name> -g <rg-name>`
2. Check Managed Identity has correct roles
3. Verify model deployment is complete in Azure Portal

### Slow Response Times

**Symptom:** Chat takes a long time to respond
**Solutions:**
1. Check Azure OpenAI throttling limits
2. Verify you're not hitting TPM (tokens per minute) limits
3. Consider upgrading to higher capacity

### Function Calling Not Working

**Symptom:** AI doesn't use functions properly
**Solutions:**
1. Verify function schemas are correct
2. Check function implementation logs
3. Ensure database connectivity is working

## Fallback Behavior

When GenAI services are NOT deployed:
- Chat interface still accessible
- Shows warning message about missing GenAI services
- Provides keyword-based responses using dummy data
- Suggests running `deploy-with-chat.sh`

## Future Enhancements

Potential improvements:
- [ ] Add more function tools (update expense, delete expense, etc.)
- [ ] Implement conversation history persistence
- [ ] Add file upload capability for receipts
- [ ] Integrate with Azure Content Safety
- [ ] Add multilingual support
- [ ] Implement user-specific conversations
- [ ] Add expense analytics and insights via AI

## References

- [Azure OpenAI Service Documentation](https://learn.microsoft.com/azure/ai-services/openai/)
- [Function Calling Guide](https://learn.microsoft.com/azure/ai-services/openai/how-to/function-calling)
- [Managed Identity with Azure OpenAI](https://learn.microsoft.com/azure/ai-services/openai/how-to/managed-identity)
- [Azure Cognitive Search RAG](https://learn.microsoft.com/azure/search/retrieval-augmented-generation-overview)
