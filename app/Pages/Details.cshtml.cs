using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ExpenseApp.Pages;

public class DetailsModel : PageModel
{
    private readonly IExpenseService _expenseService;
    private readonly ILogger<DetailsModel> _logger;

    public Expense? Expense { get; set; }
    public List<User> Users { get; set; } = new();
    public ErrorInfo? ErrorInfo { get; set; }
    public string? SuccessMessage { get; set; }

    public DetailsModel(IExpenseService expenseService, ILogger<DetailsModel> logger)
    {
        _expenseService = expenseService;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Expense = await _expenseService.GetExpenseByIdAsync(id);
        if (_expenseService.LastError != null) ErrorInfo = _expenseService.LastError;
        Users = await _expenseService.GetUsersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, string action, int? ReviewedBy)
    {
        // Determine status ID from action
        int statusId = action switch
        {
            "approve" => 3,  // Approved
            "reject"  => 4,  // Rejected
            "submit"  => 2,  // Submitted
            _ => 0
        };

        if (statusId == 0)
        {
            ErrorInfo = new ErrorInfo { Message = "Invalid action." };
            Expense = await _expenseService.GetExpenseByIdAsync(id);
            Users = await _expenseService.GetUsersAsync();
            return Page();
        }

        var request = new UpdateExpenseStatusRequest
        {
            StatusId = statusId,
            ReviewedBy = (action == "approve" || action == "reject") ? ReviewedBy : null
        };

        var success = await _expenseService.UpdateExpenseStatusAsync(id, request);

        if (_expenseService.LastError != null)
        {
            ErrorInfo = _expenseService.LastError;
            Expense = await _expenseService.GetExpenseByIdAsync(id);
            Users = await _expenseService.GetUsersAsync();
            return Page();
        }

        if (success)
        {
            string actionLabel = action switch
            {
                "approve" => "Approved",
                "reject"  => "Rejected",
                "submit"  => "Submitted",
                _ => "Updated"
            };
            SuccessMessage = $"Expense #{id} has been {actionLabel} successfully.";
        }

        Expense = await _expenseService.GetExpenseByIdAsync(id);
        Users = await _expenseService.GetUsersAsync();
        return Page();
    }
}
