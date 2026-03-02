namespace ExpenseApp.Models;

public class CreateExpenseRequest
{
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int AmountMinor { get; set; }
    public string Currency { get; set; } = "GBP";
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
}
