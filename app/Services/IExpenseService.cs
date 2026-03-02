using ExpenseApp.Models;

namespace ExpenseApp.Services;

public interface IExpenseService
{
    Task<List<Expense>> GetExpensesAsync(string? statusFilter = null);
    Task<Expense?> GetExpenseByIdAsync(int id);
    Task<Expense> CreateExpenseAsync(CreateExpenseRequest request);
    Task<bool> UpdateExpenseStatusAsync(int id, UpdateExpenseStatusRequest request);
    Task<List<User>> GetUsersAsync();
    Task<List<ExpenseCategory>> GetCategoriesAsync();
    Task<List<ExpenseStatus>> GetStatusesAsync();
    Task<List<Expense>> GetDummyExpensesAsync();
    ErrorInfo? LastError { get; }
}
