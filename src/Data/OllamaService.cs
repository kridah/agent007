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
}