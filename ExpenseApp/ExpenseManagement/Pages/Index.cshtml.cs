using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class IndexModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(DatabaseService dbService, ILogger<IndexModel> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public List<Expense> Expenses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? FilterText { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        FilterText = filter;
        Expenses = await _dbService.GetExpensesAsync();
        
        // Apply client-side filter if provided
        if (!string.IsNullOrWhiteSpace(filter))
        {
            Expenses = Expenses.Where(e =>
                e.CategoryName?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                e.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                e.StatusName?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();
        }
        
        // Check for database errors
        var lastError = _dbService.GetLastError();
        if (!string.IsNullOrEmpty(lastError))
        {
            ErrorMessage = lastError;
        }
    }
}
