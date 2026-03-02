using System.Text.Json;
using Azure;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using ExpenseApp.Models;

namespace ExpenseApp.Services;

public class ChatService : IChatService
{
    private readonly IExpenseService _expenseService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ChatService> _logger;

    public ChatService(
        IExpenseService expenseService,
        IConfiguration configuration,
        ILogger<ChatService> logger)
    {
        _expenseService = expenseService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage> history)
    {
        var endpoint = _configuration["OpenAI:Endpoint"] ?? "";
        var deploymentName = _configuration["OpenAI:DeploymentName"] ?? "gpt-4o";
        var managedIdentityClientId = _configuration["ManagedIdentityClientId"] ?? "";

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return "ℹ️ **GenAI services are not deployed.**\n\n" +
                   "To enable AI chat, redeploy with `deployGenAI=true` using `deploy-with-chat.sh`.\n\n" +
                   "In the meantime, you can use the expense management features directly from the navigation menu.";
        }

        try
        {
            // Build Azure credential - prefer explicit managed identity client ID
            TokenCredential credential = string.IsNullOrWhiteSpace(managedIdentityClientId)
                ? new DefaultAzureCredential()
                : new ManagedIdentityCredential(managedIdentityClientId);

            var client = new OpenAIClient(new Uri(endpoint), credential);

            // Build the messages list
            var messages = new List<ChatRequestMessage>
            {
                new ChatRequestSystemMessage(
                    "You are an intelligent assistant for an expense management application. " +
                    "You can help users view expenses, create new expenses, update expense statuses, " +
                    "and answer questions about the expense data. " +
                    "Use the available functions to retrieve real data when needed. " +
                    "Amounts are in GBP. Always format currency values as £X.XX.")
            };

            // Add conversation history
            foreach (var msg in history)
            {
                if (msg.Role == "user")
                    messages.Add(new ChatRequestUserMessage(msg.Content));
                else if (msg.Role == "assistant")
                    messages.Add(new ChatRequestAssistantMessage(msg.Content));
            }

            // Add current user message
            messages.Add(new ChatRequestUserMessage(userMessage));

            // Define available functions
            var tools = BuildTools();

            var options = new ChatCompletionsOptions(deploymentName, messages)
            {
                MaxTokens = 1024,
                Temperature = 0.7f
            };

            foreach (var tool in tools)
            {
                options.Tools.Add(tool);
            }

            // Orchestration loop for function calling
            int maxIterations = 5;
            for (int i = 0; i < maxIterations; i++)
            {
                var response = await client.GetChatCompletionsAsync(options);
                var choice = response.Value.Choices[0];

                if (choice.FinishReason == CompletionsFinishReason.ToolCalls)
                {
                    // Handle function calls
                    var assistantMsg = new ChatRequestAssistantMessage(choice.Message.Content ?? "");
                    foreach (var toolCall in choice.Message.ToolCalls)
                    {
                        if (toolCall is ChatCompletionsFunctionToolCall funcToolCall)
                        {
                            assistantMsg.ToolCalls.Add(
                                new ChatCompletionsFunctionToolCall(funcToolCall.Id, funcToolCall.Name, funcToolCall.Arguments));
                        }
                    }
                    messages.Add(assistantMsg);

                    // Execute each function call
                    foreach (var toolCall in choice.Message.ToolCalls)
                    {
                        if (toolCall is ChatCompletionsFunctionToolCall funcCall)
                        {
                            var result = await ExecuteFunctionAsync(funcCall.Name, funcCall.Arguments);
                            messages.Add(new ChatRequestToolMessage(result, funcCall.Id));
                        }
                    }

                    // Update options with new messages
                    options = new ChatCompletionsOptions(deploymentName, messages)
                    {
                        MaxTokens = 1024,
                        Temperature = 0.7f
                    };
                    foreach (var tool in tools) options.Tools.Add(tool);
                }
                else
                {
                    // Final text response
                    return choice.Message.Content ?? "I'm sorry, I couldn't generate a response.";
                }
            }

            return "I processed your request but reached the maximum number of steps. Please try a simpler query.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Azure OpenAI");
            return $"⚠️ Error communicating with AI service: {ex.Message}";
        }
    }

    private List<ChatCompletionsFunctionToolDefinition> BuildTools()
    {
        return new List<ChatCompletionsFunctionToolDefinition>
        {
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_expenses",
                Description = "Get a list of expenses, optionally filtered by status (Draft, Submitted, Approved, Rejected)",
                Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "status_filter": {
                            "type": "string",
                            "description": "Optional status filter: Draft, Submitted, Approved, or Rejected",
                            "enum": ["Draft", "Submitted", "Approved", "Rejected"]
                        }
                    }
                }
                """)
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_expense_by_id",
                Description = "Get details of a specific expense by its ID",
                Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expense_id": {
                            "type": "integer",
                            "description": "The expense ID to look up"
                        }
                    },
                    "required": ["expense_id"]
                }
                """)
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "create_expense",
                Description = "Create a new expense record",
                Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "user_id": { "type": "integer", "description": "ID of the user submitting the expense" },
                        "category_id": { "type": "integer", "description": "ID of the expense category" },
                        "amount_pence": { "type": "integer", "description": "Amount in pence (e.g., 1234 = £12.34)" },
                        "expense_date": { "type": "string", "description": "Date of the expense in YYYY-MM-DD format" },
                        "description": { "type": "string", "description": "Description of the expense" }
                    },
                    "required": ["user_id", "category_id", "amount_pence", "expense_date"]
                }
                """)
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "update_expense_status",
                Description = "Update the status of an expense (e.g., approve or reject)",
                Parameters = BinaryData.FromString("""
                {
                    "type": "object",
                    "properties": {
                        "expense_id": { "type": "integer", "description": "The expense ID to update" },
                        "status_id": { "type": "integer", "description": "New status ID (1=Draft, 2=Submitted, 3=Approved, 4=Rejected)" },
                        "reviewed_by": { "type": "integer", "description": "Optional user ID of the reviewer" }
                    },
                    "required": ["expense_id", "status_id"]
                }
                """)
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_users",
                Description = "Get list of all active users in the system",
                Parameters = BinaryData.FromString("""{ "type": "object", "properties": {} }""")
            },
            new ChatCompletionsFunctionToolDefinition
            {
                Name = "get_categories",
                Description = "Get list of expense categories",
                Parameters = BinaryData.FromString("""{ "type": "object", "properties": {} }""")
            }
        };
    }

    private async Task<string> ExecuteFunctionAsync(string functionName, string arguments)
    {
        try
        {
            using var doc = JsonDocument.Parse(arguments);
            var root = doc.RootElement;

            switch (functionName)
            {
                case "get_expenses":
                {
                    string? statusFilter = root.TryGetProperty("status_filter", out var sf)
                        ? sf.GetString() : null;
                    var expenses = await _expenseService.GetExpensesAsync(statusFilter);
                    return JsonSerializer.Serialize(expenses.Select(e => new
                    {
                        e.ExpenseId, e.UserName, e.CategoryName, e.StatusName,
                        AmountGBP = $"£{e.AmountGBP:F2}", e.ExpenseDate, e.Description
                    }));
                }
                case "get_expense_by_id":
                {
                    int id = root.GetProperty("expense_id").GetInt32();
                    var expense = await _expenseService.GetExpenseByIdAsync(id);
                    if (expense == null) return "{ \"error\": \"Expense not found\" }";
                    return JsonSerializer.Serialize(new
                    {
                        expense.ExpenseId, expense.UserName, expense.CategoryName, expense.StatusName,
                        AmountGBP = $"£{expense.AmountGBP:F2}", expense.ExpenseDate, expense.Description,
                        expense.SubmittedAt, expense.ReviewedByName, expense.ReviewedAt
                    });
                }
                case "create_expense":
                {
                    var req = new CreateExpenseRequest
                    {
                        UserId = root.GetProperty("user_id").GetInt32(),
                        CategoryId = root.GetProperty("category_id").GetInt32(),
                        AmountMinor = root.GetProperty("amount_pence").GetInt32(),
                        Currency = "GBP",
                        ExpenseDate = DateTime.Parse(root.GetProperty("expense_date").GetString()!),
                        Description = root.TryGetProperty("description", out var desc) ? desc.GetString() : null
                    };
                    var created = await _expenseService.CreateExpenseAsync(req);
                    return JsonSerializer.Serialize(new
                    {
                        created.ExpenseId, created.UserName, created.CategoryName,
                        AmountGBP = $"£{created.AmountGBP:F2}", created.StatusName
                    });
                }
                case "update_expense_status":
                {
                    int expId = root.GetProperty("expense_id").GetInt32();
                    var req = new UpdateExpenseStatusRequest
                    {
                        StatusId = root.GetProperty("status_id").GetInt32(),
                        ReviewedBy = root.TryGetProperty("reviewed_by", out var rb) ? rb.GetInt32() : null
                    };
                    bool success = await _expenseService.UpdateExpenseStatusAsync(expId, req);
                    return JsonSerializer.Serialize(new { success, expenseId = expId });
                }
                case "get_users":
                {
                    var users = await _expenseService.GetUsersAsync();
                    return JsonSerializer.Serialize(users.Select(u => new
                    { u.UserId, u.UserName, u.Email, u.RoleName }));
                }
                case "get_categories":
                {
                    var cats = await _expenseService.GetCategoriesAsync();
                    return JsonSerializer.Serialize(cats.Select(c => new
                    { c.CategoryId, c.CategoryName }));
                }
                default:
                    return $"{{ \"error\": \"Unknown function: {functionName}\" }}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
            return JsonSerializer.Serialize(new { error = ex.Message });
        }
    }
}
