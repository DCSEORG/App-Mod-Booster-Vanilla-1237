namespace ExpenseApp.Models;

public class UpdateExpenseStatusRequest
{
    public int StatusId { get; set; }
    public int? ReviewedBy { get; set; }
}
