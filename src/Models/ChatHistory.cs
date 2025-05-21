using ChatMessage = src.Models.ChatMessage;

namespace src;

public class ChatHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public List<ChatMessage> Messages { get; set; } = new();
    
}