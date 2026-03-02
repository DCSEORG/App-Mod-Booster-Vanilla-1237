using ExpenseApp.Models;
using Microsoft.Data.SqlClient;

namespace ExpenseApp.Services;

public class ExpenseService : IExpenseService
{
    private readonly string _connectionString;
    public ErrorInfo? LastError { get; private set; }

    public ExpenseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private SqlConnection CreateConnection() => new SqlConnection(_connectionString);

    private ErrorInfo BuildError(Exception ex, string? file = null, int line = 0)
    {
        bool isManagedIdentityError =
            ex.Message.Contains("Login failed", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Cannot open server", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Managed Identity", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("Active Directory", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("token", StringComparison.OrdinalIgnoreCase);

        return new ErrorInfo
        {
            Message = ex.Message,
            File = file,
            LineNumber = line,
            IsManagedIdentityError = isManagedIdentityError,
            ManagedIdentityFix = isManagedIdentityError
                ? "Ensure the Managed Identity has been added to the database with db_datareader, db_datawriter, and EXECUTE permissions. Run: python3 run-sql-dbrole.py"
                : null
        };
    }

    public async Task<List<Expense>> GetExpensesAsync(string? statusFilter = null)
    {
        LastError = null;
        try
        {
            var expenses = new List<Expense>();
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetExpenses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@StatusFilter", (object?)statusFilter ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                expenses.Add(MapExpense(reader));
            }
            return expenses;
        }
        catch (Exception ex)
        {
            LastError = BuildError(ex, nameof(GetExpensesAsync));
            return new List<Expense>();
        }
    }

    public async Task<Expense?> GetExpenseByIdAsync(int id)
    {
        LastError = null;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetExpenseById", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapExpense(reader);
            }
            return null;
        }
        catch (Exception ex)
        {
            LastError = BuildError(ex, nameof(GetExpenseByIdAsync));
            return null;
        }
    }

    public async Task<Expense> CreateExpenseAsync(CreateExpenseRequest request)
    {
        LastError = null;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_CreateExpense", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@UserId", request.UserId);
            cmd.Parameters.AddWithValue("@CategoryId", request.CategoryId);
            cmd.Parameters.AddWithValue("@AmountMinor", request.AmountMinor);
            cmd.Parameters.AddWithValue("@Currency", request.Currency);
            cmd.Parameters.AddWithValue("@ExpenseDate", request.ExpenseDate.Date);
            cmd.Parameters.AddWithValue("@Description", (object?)request.Description ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int newId = Convert.ToInt32(reader["ExpenseId"]);
                // Retrieve the newly created expense
                await reader.CloseAsync();
                await conn.CloseAsync();
                var created = await GetExpenseByIdAsync(newId);
                return created ?? new Expense { ExpenseId = newId };
            }
            return new Expense();
        }
        catch (Exception ex)
        {
            LastError = BuildError(ex, nameof(CreateExpenseAsync));
            throw;
        }
    }

    public async Task<bool> UpdateExpenseStatusAsync(int id, UpdateExpenseStatusRequest request)
    {
        LastError = null;
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_UpdateExpenseStatus", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };
            cmd.Parameters.AddWithValue("@ExpenseId", id);
            cmd.Parameters.AddWithValue("@StatusId", request.StatusId);
            cmd.Parameters.AddWithValue("@ReviewedBy", (object?)request.ReviewedBy ?? DBNull.Value);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                int rows = Convert.ToInt32(reader["RowsAffected"]);
                return rows > 0;
            }
            return false;
        }
        catch (Exception ex)
        {
            LastError = BuildError(ex, nameof(UpdateExpenseStatusAsync));
            return false;
        }
    }

    public async Task<List<User>> GetUsersAsync()
    {
        LastError = null;
        try
        {
            var users = new List<User>();
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetUsers", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                users.Add(new User
                {
                    UserId = Convert.ToInt32(reader["UserId"]),
                    UserName = reader["UserName"].ToString() ?? "",
                    Email = reader["Email"].ToString() ?? "",
                    RoleId = Convert.ToInt32(reader["RoleId"]),
                    RoleName = reader["RoleName"].ToString() ?? "",
                    ManagerId = reader["ManagerId"] == DBNull.Value ? null : Convert.ToInt32(reader["ManagerId"]),
                    IsActive = Convert.ToBoolean(reader["IsActive"])
                });
            }
            return users;
        }
        catch (Exception ex)
        {
            LastError = BuildError(ex, nameof(GetUsersAsync));
            return new List<User>();
        }
    }

    public async Task<List<ExpenseCategory>> GetCategoriesAsync()
    {
        LastError = null;
        try
        {
            var categories = new List<ExpenseCategory>();
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetCategories", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ExpenseCategory
                {
                    CategoryId = Convert.ToInt32(reader["CategoryId"]),
                    CategoryName = reader["CategoryName"].ToString() ?? "",
                    IsActive = Convert.ToBoolean(reader["IsActive"])
                });
            }
            return categories;
        }
        catch (Exception ex)
        {
            LastError = BuildError(ex, nameof(GetCategoriesAsync));
            return new List<ExpenseCategory>();
        }
    }

    public async Task<List<ExpenseStatus>> GetStatusesAsync()
    {
        LastError = null;
        try
        {
            var statuses = new List<ExpenseStatus>();
            await using var conn = CreateConnection();
            await conn.OpenAsync();
            await using var cmd = new SqlCommand("dbo.sp_GetStatuses", conn)
            {
                CommandType = System.Data.CommandType.StoredProcedure
            };

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                statuses.Add(new ExpenseStatus
                {
                    StatusId = Convert.ToInt32(reader["StatusId"]),
                    StatusName = reader["StatusName"].ToString() ?? ""
                });
            }
            return statuses;
        }
        catch (Exception ex)
        {
            LastError = BuildError(ex, nameof(GetStatusesAsync));
            return new List<ExpenseStatus>();
        }
    }

    public Task<List<Expense>> GetDummyExpensesAsync()
    {
        var dummy = new List<Expense>
        {
            new Expense
            {
                ExpenseId = 1,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 2,
                StatusName = "Submitted",
                AmountMinor = 2540,
                Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-10),
                Description = "Taxi from airport to client site",
                SubmittedAt = DateTime.UtcNow.AddDays(-9),
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            },
            new Expense
            {
                ExpenseId = 2,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 2,
                CategoryName = "Meals",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 1425,
                Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-25),
                Description = "Client lunch meeting",
                SubmittedAt = DateTime.UtcNow.AddDays(-24),
                ReviewedBy = 2,
                ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-23),
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new Expense
            {
                ExpenseId = 3,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 3,
                CategoryName = "Supplies",
                StatusId = 1,
                StatusName = "Draft",
                AmountMinor = 799,
                Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-2),
                Description = "Office stationery",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new Expense
            {
                ExpenseId = 4,
                UserId = 1,
                UserName = "Alice Example",
                CategoryId = 4,
                CategoryName = "Accommodation",
                StatusId = 3,
                StatusName = "Approved",
                AmountMinor = 12300,
                Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-50),
                Description = "Hotel during client visit",
                SubmittedAt = DateTime.UtcNow.AddDays(-49),
                ReviewedBy = 2,
                ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-48),
                CreatedAt = DateTime.UtcNow.AddDays(-50)
            },
            new Expense
            {
                ExpenseId = 5,
                UserId = 2,
                UserName = "Bob Manager",
                CategoryId = 1,
                CategoryName = "Travel",
                StatusId = 4,
                StatusName = "Rejected",
                AmountMinor = 5000,
                Currency = "GBP",
                ExpenseDate = DateTime.UtcNow.AddDays(-15),
                Description = "Conference travel - missing receipt",
                SubmittedAt = DateTime.UtcNow.AddDays(-14),
                ReviewedBy = 2,
                ReviewedByName = "Bob Manager",
                ReviewedAt = DateTime.UtcNow.AddDays(-13),
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            }
        };
        return Task.FromResult(dummy);
    }

    private static Expense MapExpense(SqlDataReader reader)
    {
        return new Expense
        {
            ExpenseId = Convert.ToInt32(reader["ExpenseId"]),
            UserId = Convert.ToInt32(reader["UserId"]),
            UserName = reader["UserName"].ToString() ?? "",
            CategoryId = Convert.ToInt32(reader["CategoryId"]),
            CategoryName = reader["CategoryName"].ToString() ?? "",
            StatusId = Convert.ToInt32(reader["StatusId"]),
            StatusName = reader["StatusName"].ToString() ?? "",
            AmountMinor = Convert.ToInt32(reader["AmountMinor"]),
            Currency = reader["Currency"].ToString() ?? "GBP",
            ExpenseDate = Convert.ToDateTime(reader["ExpenseDate"]),
            Description = reader["Description"] == DBNull.Value ? null : reader["Description"].ToString(),
            ReceiptFile = reader["ReceiptFile"] == DBNull.Value ? null : reader["ReceiptFile"].ToString(),
            SubmittedAt = reader["SubmittedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["SubmittedAt"]),
            ReviewedBy = reader["ReviewedBy"] == DBNull.Value ? null : Convert.ToInt32(reader["ReviewedBy"]),
            ReviewedByName = reader["ReviewedByName"] == DBNull.Value ? null : reader["ReviewedByName"].ToString(),
            ReviewedAt = reader["ReviewedAt"] == DBNull.Value ? null : Convert.ToDateTime(reader["ReviewedAt"]),
            CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
        };
    }
}
