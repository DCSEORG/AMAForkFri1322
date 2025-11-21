using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Pages;

public class AddExpenseModel : PageModel
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<AddExpenseModel> _logger;

    public AddExpenseModel(DatabaseService dbService, ILogger<AddExpenseModel> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    [BindProperty]
    public CreateExpenseRequest ExpenseRequest { get; set; } = new();
    
    public List<ExpenseCategory> Categories { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public string? SuccessMessage { get; set; }

    public async Task OnGetAsync()
    {
        Categories = await _dbService.GetCategoriesAsync();
        ExpenseRequest.Date = DateTime.Today;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Categories = await _dbService.GetCategoriesAsync();
        
        if (!ModelState.IsValid)
        {
            ErrorMessage = "Please fill in all required fields.";
            return Page();
        }

        try
        {
            var newId = await _dbService.CreateExpenseAsync(ExpenseRequest, userId: 1);
            if (newId > 0)
            {
                SuccessMessage = $"Expense created successfully (ID: {newId})";
                // Reset form
                ExpenseRequest = new CreateExpenseRequest { Date = DateTime.Today };
                return Page();
            }
            else
            {
                ErrorMessage = "Failed to create expense. Using dummy data mode.";
                return Page();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorMessage = $"Error creating expense: {ex.Message}";
            return Page();
        }
    }
}
