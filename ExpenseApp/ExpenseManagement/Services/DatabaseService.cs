using Microsoft.Data.SqlClient;
using ExpenseManagement.Models;
using Azure.Identity;
using System.Data;

namespace ExpenseManagement.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseService> _logger;
    private bool _useDummyData = false;
    private string? _lastError = null;

    public DatabaseService(IConfiguration configuration, ILogger<DatabaseService> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
        
        // Test connection on startup
        try
        {
            using var connection = CreateConnection();
            connection.Open();
            connection.Close();
            _logger.LogInformation("Database connection successful");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to database. Using dummy data fallback.");
            _useDummyData = true;
            _lastError = $"Database connection error in DatabaseService constructor: {ex.Message}";
        }
    }

    public string? GetLastError() => _lastError;

    private SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }

    public async Task<List<Expense>> GetExpensesAsync(int? statusId = null, int? userId = null)
    {
        if (_useDummyData)
        {
            return GetDummyExpenses(statusId, userId);
        }

        try
        {
            var expenses = new List<Expense>();
            
            using var connection = CreateConnection();
            await connection.OpenAsync();
            
            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, 
                       e.Currency, e.ExpenseDate, e.Description, e.ReceiptFile,
                       e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt,
                       u.UserName, c.CategoryName, s.StatusName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                WHERE (@StatusId IS NULL OR e.StatusId = @StatusId)
                  AND (@UserId IS NULL OR e.UserId = @UserId)
                ORDER BY e.ExpenseDate DESC";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@StatusId", statusId.HasValue ? statusId.Value : DBNull.Value);
            command.Parameters.AddWithValue("@UserId", userId.HasValue ? userId.Value : DBNull.Value);
            
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(new Expense
                {
                    ExpenseId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    CategoryId = reader.GetInt32(2),
                    StatusId = reader.GetInt32(3),
                    AmountMinor = reader.GetInt32(4),
                    Currency = reader.GetString(5),
                    ExpenseDate = reader.GetDateTime(6),
                    Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ReceiptFile = reader.IsDBNull(8) ? null : reader.GetString(8),
                    SubmittedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    ReviewedBy = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    ReviewedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    CreatedAt = reader.GetDateTime(12),
                    UserName = reader.GetString(13),
                    CategoryName = reader.GetString(14),
                    StatusName = reader.GetString(15)
                });
            }
            
            return expenses;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expenses");
            _lastError = $"Error in GetExpensesAsync: {ex.Message}";
            _useDummyData = true;
            return GetDummyExpenses(statusId, userId);
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int expenseId)
    {
        if (_useDummyData)
        {
            return GetDummyExpenses().FirstOrDefault(e => e.ExpenseId == expenseId);
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            
            var query = @"
                SELECT e.ExpenseId, e.UserId, e.CategoryId, e.StatusId, e.AmountMinor, 
                       e.Currency, e.ExpenseDate, e.Description, e.ReceiptFile,
                       e.SubmittedAt, e.ReviewedBy, e.ReviewedAt, e.CreatedAt,
                       u.UserName, c.CategoryName, s.StatusName
                FROM dbo.Expenses e
                JOIN dbo.Users u ON e.UserId = u.UserId
                JOIN dbo.ExpenseCategories c ON e.CategoryId = c.CategoryId
                JOIN dbo.ExpenseStatus s ON e.StatusId = s.StatusId
                WHERE e.ExpenseId = @ExpenseId";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            
            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new Expense
                {
                    ExpenseId = reader.GetInt32(0),
                    UserId = reader.GetInt32(1),
                    CategoryId = reader.GetInt32(2),
                    StatusId = reader.GetInt32(3),
                    AmountMinor = reader.GetInt32(4),
                    Currency = reader.GetString(5),
                    ExpenseDate = reader.GetDateTime(6),
                    Description = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ReceiptFile = reader.IsDBNull(8) ? null : reader.GetString(8),
                    SubmittedAt = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                    ReviewedBy = reader.IsDBNull(10) ? null : reader.GetInt32(10),
                    ReviewedAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    CreatedAt = reader.GetDateTime(12),
                    UserName = reader.GetString(13),
                    CategoryName = reader.GetString(14),
                    StatusName = reader.GetString(15)
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expense {ExpenseId}", expenseId);
            _lastError = $"Error in GetExpenseByIdAsync: {ex.Message}";
            return null;
        }
    }

    public async Task<int> CreateExpenseAsync(CreateExpenseRequest request, int userId = 1)
    {
        if (_useDummyData)
        {
            _logger.LogWarning("Cannot create expense - using dummy data mode");
            return -1;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            
            var query = @"
                INSERT INTO dbo.Expenses (UserId, CategoryId, StatusId, AmountMinor, Currency, ExpenseDate, Description, SubmittedAt, CreatedAt)
                VALUES (@UserId, @CategoryId, 1, @AmountMinor, 'GBP', @ExpenseDate, @Description, NULL, SYSUTCDATETIME());
                SELECT CAST(SCOPE_IDENTITY() as int);";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            command.Parameters.AddWithValue("@AmountMinor", (int)(request.Amount * 100));
            command.Parameters.AddWithValue("@ExpenseDate", request.Date);
            command.Parameters.AddWithValue("@Description", request.Description ?? (object)DBNull.Value);
            
            var newId = await command.ExecuteScalarAsync();
            return newId != null ? Convert.ToInt32(newId) : -1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            _lastError = $"Error in CreateExpenseAsync: {ex.Message}";
            throw;
        }
    }

    public async Task<bool> UpdateExpenseStatusAsync(int expenseId, int newStatusId, int? reviewerId = null)
    {
        if (_useDummyData)
        {
            _logger.LogWarning("Cannot update expense - using dummy data mode");
            return false;
        }

        try
        {
            using var connection = CreateConnection();
            await connection.OpenAsync();
            
            var query = @"
                UPDATE dbo.Expenses 
                SET StatusId = @StatusId,
                    SubmittedAt = CASE WHEN @StatusId = 2 AND SubmittedAt IS NULL THEN SYSUTCDATETIME() ELSE SubmittedAt END,
                    ReviewedBy = @ReviewerId,
                    ReviewedAt = CASE WHEN @StatusId IN (3, 4) THEN SYSUTCDATETIME() ELSE ReviewedAt END
                WHERE ExpenseId = @ExpenseId";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@ExpenseId", expenseId);
            command.Parameters.AddWithValue("@StatusId", newStatusId);
            command.Parameters.AddWithValue("@ReviewerId", reviewerId.HasValue ? reviewerId.Value : DBNull.Value);
            
            var rowsAffected = await command.ExecuteNonQueryAsync();
            return rowsAffected > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating expense status");
            _lastError = $"Error in UpdateExpenseStatusAsync: {ex.Message}";
            return false;
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        if (_useDummyData)
        {
            return GetDummyCategories();
        }

        try
        {
            var categories = new List<ExpenseCategory>();
            
            using var connection = CreateConnection();
            await connection.OpenAsync();
            
            var query = "SELECT CategoryId, CategoryName, IsActive FROM dbo.ExpenseCategories WHERE IsActive = 1 ORDER BY CategoryName";
            
            using var command = new SqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = reader.GetInt32(0),
                    CategoryName = reader.GetString(1),
                    IsActive = reader.GetBoolean(2)
                });
            }
            
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving categories");
            _lastError = $"Error in GetCategoriesAsync: {ex.Message}";
            return GetDummyCategories();
        }
    }

    private List<Expense> GetDummyExpenses(int? statusId = null, int? userId = null)
    {
        var dummyData = new List<Expense>
        {
            new Expense { ExpenseId = 1, UserId = 1, UserName = "Alice Example", CategoryId = 1, CategoryName = "Travel", StatusId = 2, StatusName = "Submitted", AmountMinor = 12000, ExpenseDate = new DateTime(2024, 1, 15), Description = "Taxi to client site", CreatedAt = DateTime.Now },
            new Expense { ExpenseId = 2, UserId = 1, UserName = "Alice Example", CategoryId = 2, CategoryName = "Food", StatusId = 2, StatusName = "Submitted", AmountMinor = 6900, ExpenseDate = new DateTime(2023, 1, 10), Description = "Team lunch", CreatedAt = DateTime.Now },
            new Expense { ExpenseId = 3, UserId = 1, UserName = "Alice Example", CategoryId = 3, CategoryName = "Office Supplies", StatusId = 3, StatusName = "Approved", AmountMinor = 9950, ExpenseDate = new DateTime(2023, 12, 4), Description = "Notebooks and pens", CreatedAt = DateTime.Now },
            new Expense { ExpenseId = 4, UserId = 1, UserName = "Alice Example", CategoryId = 4, CategoryName = "Transport", StatusId = 4, StatusName = "Rejected", AmountMinor = 1920, ExpenseDate = new DateTime(2023, 21, 18), Description = "Bus fare", CreatedAt = DateTime.Now }
        };

        return dummyData
            .Where(e => !statusId.HasValue || e.StatusId == statusId.Value)
            .Where(e => !userId.HasValue || e.UserId == userId.Value)
            .ToList();
    }

    private List<ExpenseCategory> GetDummyCategories()
    {
        return new List<ExpenseCategory>
        {
            new ExpenseCategory { CategoryId = 1, CategoryName = "Travel", IsActive = true },
            new ExpenseCategory { CategoryId = 2, CategoryName = "Food", IsActive = true },
            new ExpenseCategory { CategoryId = 3, CategoryName = "Office Supplies", IsActive = true },
            new ExpenseCategory { CategoryId = 4, CategoryName = "Transport", IsActive = true }
        };
    }
}
