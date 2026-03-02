using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages;

public class IndexModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<IndexModel> _logger;

    public List<Expense> Expenses { get; set; } = new();
    public ErrorInfo? ErrorInfo { get; set; }
    public string? StatusFilter { get; set; }

    public IndexModel(IExpenseService expenseService, ILogger<IndexModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync(string? status = null)
    {
        StatusFilter = status;
        Expenses = await _expenseService.GetExpensesAsync(status);

        if (_expenseService.LastError != null)
        {
            ErrorInfo = _expenseService.LastError;
            _logger.LogWarning("DB error on Index page: {Message}", ErrorInfo.Message);
            // Fall back to dummy data
            Expenses = await _expenseService.GetDummyExpensesAsync();

            // Apply status filter to dummy data
            if (!string.IsNullOrWhiteSpace(status))
            {
                Expenses = Expenses.Where(e =>
                    e.StatusName.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
            }
        }
    }
}
