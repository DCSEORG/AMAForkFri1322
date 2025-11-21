using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure;
using Azure.AI.OpenAI;
using Azure.Identity;
using ExpenseManagement.Services;
using ExpenseManagement.Models;
using System.Text.Json;

namespace ExpenseManagement.Pages;

public class ChatMessage
{
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
}

public class ChatModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly DatabaseService _dbService;
    private readonly ILogger<ChatModel> _logger;

    public ChatModel(IConfiguration configuration, DatabaseService dbService, ILogger<ChatModel> logger)
    {
        _configuration = configuration;
        _dbService = dbService;
        _logger = logger;
    }

    public string? ErrorMessage { get; set; }
    public bool GenAIAvailable { get; set; }

    public void OnGet()
    {
        // Check if GenAI is configured
        var endpoint = _configuration["OpenAI__Endpoint"];
        GenAIAvailable = !string.IsNullOrEmpty(endpoint);
        
        if (!GenAIAvailable)
        {
            ErrorMessage = "GenAI services not deployed. Deploy with 'deploy-with-chat.sh' to enable AI chat features.";
        }
    }

    public async Task<IActionResult> OnPostAsync([FromBody] ChatRequest request)
    {
        try
        {
            var endpoint = _configuration["OpenAI__Endpoint"];
            var deploymentName = _configuration["OpenAI__DeploymentName"];

            if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(deploymentName))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "GenAI services not configured. Please deploy with 'deploy-with-chat.sh' to enable AI features.",
                    isDummyResponse = true
                });
            }

            // For now, return a simulated AI response
            // Full OpenAI integration will be completed when GenAI resources are deployed
            var userMessage = request.UserMessage ?? request.Messages?.LastOrDefault()?.Content ?? "";
            var response = await GenerateSimpleResponse(userMessage);

            return new JsonResult(new
            {
                success = true,
                message = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in chat");
            return new JsonResult(new
            {
                success = false,
                message = $"Error: {ex.Message}",
                isDummyResponse = true
            });
        }
    }

    private async Task<string> GenerateSimpleResponse(string userMessage)
    {
        var lowerMsg = userMessage.ToLower();
        
        // Simple keyword-based responses with actual data from database
        if (lowerMsg.Contains("show") || lowerMsg.Contains("list") || lowerMsg.Contains("expense"))
        {
            var expenses = await _dbService.GetExpensesAsync();
            if (expenses.Any())
            {
                var summary = $"I found {expenses.Count} expenses:\n\n";
                foreach (var expense in expenses.Take(5))
                {
                    summary += $"• {expense.ExpenseDate:dd/MM/yyyy} - {expense.CategoryName}: £{expense.Amount:F2} ({expense.StatusName})\n";
                }
                if (expenses.Count > 5)
                {
                    summary += $"\n...and {expenses.Count - 5} more. Visit the main page to see all expenses.";
                }
                return summary;
            }
            return "No expenses found in the system.";
        }
        
        if (lowerMsg.Contains("pending") || lowerMsg.Contains("submit"))
        {
            var pending = await _dbService.GetExpensesAsync(statusId: 2);
            return pending.Any() 
                ? $"There are {pending.Count} pending expenses waiting for approval." 
                : "No pending expenses at the moment.";
        }
        
        if (lowerMsg.Contains("categor"))
        {
            var categories = await _dbService.GetCategoriesAsync();
            return $"Available categories: {string.Join(", ", categories.Select(c => c.CategoryName))}";
        }
        
        if (lowerMsg.Contains("help"))
        {
            return "I can help you with:\n• View all expenses\n• Check pending expenses\n• List categories\n• Get expense information\n\nNote: Full AI capabilities require deploying with 'deploy-with-chat.sh'";
        }
        
        return "I understand you're asking about expenses. Try asking to 'show expenses', 'list pending', or 'show categories'. For full AI chat with natural language understanding, deploy with 'deploy-with-chat.sh'.";
    }
}

public class ChatRequest
{
    public List<ChatMessage>? Messages { get; set; }
    public string? UserMessage { get; set; }
}
