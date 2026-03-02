using ExpenseApp.Models;
using ExpenseApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExpenseApp.Controllers;

[ApiController]
[Route("api")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly IChatService _chatService;
    private readonly ILogger<ExpensesController> _logger;

    public ExpensesController(
        IExpenseService expenseService,
        IChatService chatService,
        ILogger<ExpensesController> logger)
    {
        _expenseService = expenseService;
        _chatService = chatService;
        _logger = logger;
    }

    // GET /api/expenses?status=xxx
    [HttpGet("expenses")]
    [ProducesResponseType(typeof(List<Expense>), 200)]
    public async Task<IActionResult> GetExpenses([FromQuery] string? status = null)
    {
        var expenses = await _expenseService.GetExpensesAsync(status);
        return Ok(expenses);
    }

    // GET /api/expenses/{id}
    [HttpGet("expenses/{id:int}")]
    [ProducesResponseType(typeof(Expense), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetExpenseById(int id)
    {
        var expense = await _expenseService.GetExpenseByIdAsync(id);
        if (expense == null) return NotFound(new { message = $"Expense {id} not found" });
        return Ok(expense);
    }

    // POST /api/expenses
    [HttpPost("expenses")]
    [ProducesResponseType(typeof(Expense), 201)]
    public async Task<IActionResult> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var expense = await _expenseService.CreateExpenseAsync(request);
        return CreatedAtAction(nameof(GetExpenseById), new { id = expense.ExpenseId }, expense);
    }

    // PUT /api/expenses/{id}/status
    [HttpPut("expenses/{id:int}/status")]
    [ProducesResponseType(200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateExpenseStatusRequest request)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var success = await _expenseService.UpdateExpenseStatusAsync(id, request);
        if (!success) return NotFound(new { message = $"Expense {id} not found or not updated" });
        return Ok(new { message = "Status updated successfully" });
    }

    // GET /api/users
    [HttpGet("users")]
    [ProducesResponseType(typeof(List<User>), 200)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _expenseService.GetUsersAsync();
        return Ok(users);
    }

    // GET /api/categories
    [HttpGet("categories")]
    [ProducesResponseType(typeof(List<ExpenseCategory>), 200)]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _expenseService.GetCategoriesAsync();
        return Ok(categories);
    }

    // GET /api/statuses
    [HttpGet("statuses")]
    [ProducesResponseType(typeof(List<ExpenseStatus>), 200)]
    public async Task<IActionResult> GetStatuses()
    {
        var statuses = await _expenseService.GetStatusesAsync();
        return Ok(statuses);
    }

    // POST /api/chat
    [HttpPost("chat")]
    [ProducesResponseType(typeof(object), 200)]
    public async Task<IActionResult> Chat([FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest(new { message = "Message cannot be empty" });

        var history = request.History ?? new List<ChatMessage>();
        var response = await _chatService.GetChatResponseAsync(request.Message, history);
        return Ok(new { response });
    }
}

public class ChatRequest
{
    public string Message { get; set; } = "";
    public List<ChatMessage>? History { get; set; }
}
