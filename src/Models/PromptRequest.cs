namespace src.Models;

public class PromptRequest
{
    public string Prompt { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Model { get; set; }
}