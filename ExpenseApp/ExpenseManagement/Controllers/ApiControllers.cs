using Microsoft.AspNetCore.Mvc;
using ExpenseManagement.Models;
using ExpenseManagement.Services;

namespace ExpenseManagement.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ExpensesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(DatabaseService dbService, ILogger<ExpensesController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Get all expenses, optionally filtered by status or user
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetExpenses([FromQuery] int? statusId, [FromQuery] int? userId)
    {
        try
        {
            var expenses = await _dbService.GetExpensesAsync(statusId, userId);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            return StatusCode(500, new { error = "Failed to retrieve expenses", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific expense by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetExpense(int id)
    {
        try
        {
            var expense = await _dbService.GetExpenseByIdAsync(id);
            if (expense == null)
            {
                return NotFound(new { error = $"Expense with ID {id} not found" });
            }
            return Ok(expense);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expense {ExpenseId}", id);
            return StatusCode(500, new { error = "Failed to retrieve expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Create a new expense
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<int>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        try
        {
            var newId = await _dbService.CreateExpenseAsync(request, userId: 1); // Default to user 1 for demo
            if (newId > 0)
            {
                return CreatedAtAction(nameof(GetExpense), new { id = newId }, new { expenseId = newId });
            }
            return BadRequest(new { error = "Failed to create expense" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            return StatusCode(500, new { error = "Failed to create expense", details = ex.Message });
        }
    }

    /// <summary>
    /// Update expense status (submit, approve, reject)
    /// </summary>
    [HttpPut("{id}/status")]
    public async Task<ActionResult> UpdateExpenseStatus(int id, [FromBody] UpdateExpenseStatusRequest request)
    {
        try
        {
            if (id != request.ExpenseId)
            {
                return BadRequest(new { error = "ID mismatch" });
            }

            var success = await _dbService.UpdateExpenseStatusAsync(id, request.NewStatusId, request.ReviewerId);
            if (success)
            {
                return Ok(new { message = "Status updated successfully" });
            }
            return NotFound(new { error = "Expense not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense status");
            return StatusCode(500, new { error = "Failed to update expense status", details = ex.Message });
        }
    }

    /// <summary>
    /// Get expenses pending approval
    /// </summary>
    [HttpGet("pending")]
    public async Task<ActionResult<List<Expense>>> GetPendingExpenses()
    {
        try
        {
            // StatusId 2 = Submitted
            var expenses = await _dbService.GetExpensesAsync(statusId: 2);
            return Ok(expenses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending expenses");
            return StatusCode(500, new { error = "Failed to retrieve pending expenses", details = ex.Message });
        }
    }
}

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly DatabaseService _dbService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(DatabaseService dbService, ILogger<CategoriesController> logger)
    {
        _dbService = dbService;
        _logger = logger;
    }

    /// <summary>
    /// Get all active expense categories
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ExpenseCategory>>> GetCategories()
    {
        try
        {
            var categories = await _dbService.GetCategoriesAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting categories");
            return StatusCode(500, new { error = "Failed to retrieve categories", details = ex.Message });
        }
    }
}
