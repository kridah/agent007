using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace src.Data;


public class OllamaService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly OllamaApiClient _client;

    public OllamaService(IOptions<OllamaSettings> options)
    {
        _client = new OllamaApiClient(options.Value.BaseUrl);
    }

    public async Task<string> GenerateAsync(string prompt, string model = "llama3")
    {
        var response = await _client.GenerateAsync(new GenerateRequest
        {
            Model = model,
            Prompt = prompt
        }).StreamToEndAsync();

        return response.Response;
    }

    public async Task<string> ChatAsync(string message, string model = "llama3")
    {
        var response = await _client.ChatAsync(new ChatRequest
        {
            Model = model,
            Messages = new List<OllamaSharp.Models.Chat.Message>
            {
                new Message { Role = OllamaSharp.Models.Chat.ChatRole.User, Content = message }
            }
        }).StreamToEndAsync();

        return response.Message.Content;
    }

    public async Task<IEnumerable<Model>> ListModelsAsync()
    {
        var models = await _client.ListLocalModelsAsync();
        return models;
    }

    public string GetBaseUrl()
    {
        return Environment.GetEnvironmentVariable("OLLAMA_API_URL") ?? "http://localhost:11434";
    }

    /// <summary>
    /// Creates a conversation between two different Ollama models
    /// </summary>
    public async Task<string> CreateMultiAgentConversationAsync(
        string initialPrompt,
        string firstModel = "llama3",
        string secondModel = "llama3",
        int maxTurns = 3)
    {
        var conversation = new List<Message>();

        // Add the initial prompt as system message
        conversation.Add(new Message
        {
            Role = ChatRole.System,
            Content = initialPrompt
        });

        string latestResponse = "Let's discuss this topic.";

        // Alternate between models for the specified number of turns
        for (int i = 0; i < maxTurns; i++)
        {
            // First model responds
            conversation.Add(new Message
            {
                Role = ChatRole.User,
                Content = latestResponse
            });

            var firstModelResponse = await _client.ChatAsync(new ChatRequest
            {
                Model = firstModel,
                Messages = conversation
            }).StreamToEndAsync();

            latestResponse = firstModelResponse.Message.Content;
            conversation.Add(new Message
            {
                Role = ChatRole.Assistant,
                Content = latestResponse
            });

            // Second model responds
            conversation.Add(new Message
            {
                Role = ChatRole.User,
                Content = latestResponse
            });

            var secondModelResponse = await _client.ChatAsync(new ChatRequest
            {
                Model = secondModel,
                Messages = conversation
            }).StreamToEndAsync();

            latestResponse = secondModelResponse.Message.Content;
            conversation.Add(new Message
            {
                Role = ChatRole.Assistant,
                Content = latestResponse
            });
        }

        // Return the full conversation as a formatted string
        return FormatConversation(conversation);
    }

    private string FormatConversation(List<Message> conversation)
    {
        var result = new System.Text.StringBuilder();

        foreach (var message in conversation)
        {
            result.AppendLine($"{message.Role}: {message.Content}");
            result.AppendLine();
        }

        return result.ToString();
    }

    // Add this method to your OllamaService class
    public async Task<string> GenerateAgentResponseAsync(
        string latestMessage,
        List<Pages.Multiagent.MessageDisplay> conversation,
        string model,
        string systemInstruction)
    {
        // Convert the UI messages to the format expected by Ollama
        var ollamaMessages = new List<Message>();

        // Add system instruction
        ollamaMessages.Add(new Message
        {
            Role = ChatRole.System,
            Content = systemInstruction
        });

        // Add conversation history
        foreach (var msg in conversation)
        {
            // Skip system messages in the history to avoid confusion
            if (msg.Role == "system")
                continue;

            ollamaMessages.Add(new Message
            {
                Role = ChatRole.User,
                Content = msg.Content
            });
        }

        // Add the latest message
        ollamaMessages.Add(new Message
        {
            Role = ChatRole.User,
            Content = latestMessage
        });

        // Send request to Ollama
        var response = await _client.ChatAsync(new ChatRequest
        {
            Model = model,
            Messages = ollamaMessages
        }).StreamToEndAsync();

        return response.Message.Content;
    }
    public async Task<string> GenerateWithTemplateAsync(
        string model,
        string input,
        string template)
    {
        // Create a generate request with custom template
        var request = new GenerateRequest
        {
            Model = model,
            Prompt = input,
            Template = template,
            Stream = false
        };

        var response = await _client.GenerateAsync(request).StreamToEndAsync();
        return response.Response;
    }

    public async Task<string> UseTemplateAsync(string prompt, string templateContent, string model = "llama3")
    {
        using var client = new HttpClient();
        client.BaseAddress = new Uri("http://localhost:11434");

        var requestBody = new
        {
            model = model,
            prompt = prompt,
            template = templateContent,
            stream = false
        };

        var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            System.Text.Encoding.UTF8,
            "application/json");

        var response = await client.PostAsync("/api/generate", content);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadAsStringAsync();
        var jsonResponse = JsonSerializer.Deserialize<JsonElement>(result);

        return jsonResponse.GetProperty("response").GetString() ?? string.Empty;
    }

}

