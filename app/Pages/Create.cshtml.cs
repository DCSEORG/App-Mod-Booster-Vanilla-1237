using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages;

public class CreateModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<CreateModel> _logger;

    public List<User> Users { get; set; } = new();
    public List<ExpenseCategory> Categories { get; set; } = new();
    public ErrorInfo? ErrorInfo { get; set; }
    public string? SuccessMessage { get; set; }

    public CreateModel(IExpenseService expenseService, ILogger<CreateModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task OnGetAsync()
    {
        await LoadFormDataAsync();
    }

    public async Task<IActionResult> OnPostAsync(
        int UserId, int CategoryId, decimal AmountGBP,
        DateTime ExpenseDate, string? Description)
    {
        await LoadFormDataAsync();

        if (UserId <= 0 || CategoryId <= 0 || AmountGBP <= 0)
        {
            ErrorInfo = new ErrorInfo { Message = "Please fill in all required fields." };
            return Page();
        }

        var request = new CreateExpenseRequest
        {
            UserId = UserId,
            CategoryId = CategoryId,
            AmountMinor = (int)Math.Round(AmountGBP * 100),
            Currency = "GBP",
            ExpenseDate = ExpenseDate,
            Description = Description
        };

        try
        {
            var expense = await _expenseService.CreateExpenseAsync(request);
            if (_expenseService.LastError != null)
            {
                ErrorInfo = _expenseService.LastError;
                return Page();
            }
            return RedirectToPage("/Details", new { id = expense.ExpenseId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating expense");
            ErrorInfo = new ErrorInfo { Message = ex.Message };
            return Page();
        }
    }

    private async Task LoadFormDataAsync()
    {
        Users = await _expenseService.GetUsersAsync();
        Categories = await _expenseService.GetCategoriesAsync();
    }
}
