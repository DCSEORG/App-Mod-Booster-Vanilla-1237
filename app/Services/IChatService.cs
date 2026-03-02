namespace ExpenseApp.Services;

public interface IChatService
{
    Task<string> GetChatResponseAsync(string userMessage, List<ChatMessage> history);
}

public class ChatMessage
{
    public string Role { get; set; } = "";
    public string Content { get; set; } = "";
}
