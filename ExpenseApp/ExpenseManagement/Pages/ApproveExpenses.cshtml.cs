using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class ApproveExpensesModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<ApproveExpensesModel> _logger;

    public ApproveExpensesModel(DatabaseService dbService, ILogger<ApproveExpensesModel> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    public List<Expense> PendingExpenses { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }
    public string? FilterText { get; set; }

    public async Task OnGetAsync(string? filter)
    {
        FilterText = filter;
        // StatusId 2 = Submitted
        PendingExpenses = await _dbService.GetExpensesAsync(statusId: 2);
        
        // Apply client-side filter if provided
        if (!string.IsNullOrWhiteSpace(filter))
        {
            PendingExpenses = PendingExpenses.Where(e =>
                e.CategoryName?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true ||
                e.Description?.Contains(filter, StringComparison.OrdinalIgnoreCase) == true
            ).ToList();
        }
        
        var lastError = _dbService.GetLastError();
        if (!string.IsNullOrEmpty(lastError))
        {
            ErrorMessage = lastError;
        }
    }

    public async Task<IActionResult> OnPostApproveAsync(int expenseId)
    {
        try
        {
            // StatusId 3 = Approved, ReviewerId = 2 (Bob Manager)
            var success = await _dbService.UpdateExpenseStatusAsync(expenseId, 3, reviewerId: 2);
            if (success)
            {
                SuccessMessage = $"Expense {expenseId} approved successfully.";
            }
            else
            {
                ErrorMessage = "Failed to approve expense.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving expense");
            ErrorMessage = $"Error: {ex.Message}";
        }
        
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRejectAsync(int expenseId)
    {
        try
        {
            // StatusId 4 = Rejected, ReviewerId = 2 (Bob Manager)
            var success = await _dbService.UpdateExpenseStatusAsync(expenseId, 4, reviewerId: 2);
            if (success)
            {
                SuccessMessage = $"Expense {expenseId} rejected.";
            }
            else
            {
                ErrorMessage = "Failed to reject expense.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting expense");
            ErrorMessage = $"Error: {ex.Message}";
        }
        
        return RedirectToPage();
    }
}
