namespace ExpenseApp.Models;

public class ErrorInfo
{
    public string Message { get; set; } = string.Empty;
    public string? File { get; set; }
    public int LineNumber { get; set; }
    public bool IsMangedIdentityError { get; set; }
    public string? ManagedIdentityFix { get; set; }
}
