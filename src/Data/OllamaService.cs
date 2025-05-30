using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace src.Data;


/// <summary>
/// Provides services for interacting with the Ollama API, including generating text, chatting, and managing models.
/// </summary>
public class OllamaService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly OllamaApiClient _client;

    /// <summary>
    /// Initializes a new instance of the <see cref="OllamaService"/> class.
    /// </summary>
    /// <param name="options">The configuration options for the Ollama API.</param>
    public OllamaService(IOptions<OllamaSettings> options, ILogger<OllamaService> logger)
    {
        _client = new OllamaApiClient(options.Value.BaseUrl);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a response from the specified model based on the given prompt.
    /// </summary>
    /// <param name="prompt">The input prompt for the model.</param>
    /// <param name="model">The name of the model to use. Defaults to "llama3".</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the generated response.</returns>
    public async Task<string> GenerateAsync(string prompt, string model = "llama3")
    {
        var response = await _client.GenerateAsync(new GenerateRequest
        {
            Model = model,
            Prompt = prompt
        }).StreamToEndAsync();

        return response.Response;
    }

    /// <summary>
    /// Sends a chat message to the specified model and retrieves the response.
    /// </summary>
    /// <param name="message">The message to send to the model.</param>
    /// <param name="model">The name of the model to use. Defaults to "llama3".</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat response.</returns>
    public async Task<string> ChatAsync(string message, string model = "llama3")
    {
        _logger.LogInformation("Chatting with model: {Model}", model);
        var response = await _client.ChatAsync(new ChatRequest
        {
            Model = model,
            Messages = new List<OllamaSharp.Models.Chat.Message>
            {
                new Message { Role = OllamaSharp.Models.Chat.ChatRole.User, Content = message }
            },
            Stream = true
        }).StreamToEndAsync();

        return response.Message.Content;
    }

    /// <summary>
    /// Lists all locally available models.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a collection of available models.</returns>
    public async Task<IEnumerable<Model>> ListModelsAsync()
    {
        var models = await _client.ListLocalModelsAsync();
        return models;
    }

    /// <summary>
    /// Gets the base URL for the Ollama API.
    /// </summary>
    /// <returns>The base URL as a string.</returns>
    public string GetBaseUrl()
    {
        return Environment.GetEnvironmentVariable("OLLAMA_API_URL") ?? "http://localhost:11434";
    }

    /// <summary>
    /// Creates a conversation between two different Ollama models.
    /// </summary>
    /// <param name="initialPrompt">The initial prompt to start the conversation.</param>
    /// <param name="firstModel">The name of the first model. Defaults to "llama3".</param>
    /// <param name="secondModel">The name of the second model. Defaults to "llama3".</param>
    /// <param name="maxTurns">The maximum number of turns in the conversation. Defaults to 3.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the formatted conversation.</returns>
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

    /// <summary>
    /// Formats a conversation into a readable string.
    /// </summary>
    /// <param name="conversation">The list of messages in the conversation.</param>
    /// <returns>The formatted conversation as a string.</returns>
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

    /// <summary>
    /// Retrieves a list of available models, including their installation status.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of available models.</returns>
    public async Task<List<AvailableModel>> GetAvailableModelsAsync()
    {
        // Example curated list; expand as needed
        var availableModels = new List<AvailableModel>
        {
            new AvailableModel { Name = "llama3", DisplayName = "Llama 3", Description = "Meta's Llama 3 model", Size = "4.7 GB" },
            new AvailableModel { Name = "mistral", DisplayName = "Mistral 7B", Description = "Mistral AI's 7B model", Size = "4.1 GB" }
            // Add more models as needed
        };

        // Check which models are already installed
        var installedModels = await ListModelsAsync();
        var installedNames = installedModels.Select(m => m.Name.Split(':')[0].ToLower()).ToHashSet();

        foreach (var model in availableModels)
        {
            var baseName = model.Name.Split(':')[0].ToLower();
            model.IsInstalled = installedNames.Contains(baseName);
        }

        return availableModels;
    }

    /// <summary>
    /// Downloads a model and reports progress.
    /// </summary>
    /// <param name="modelName">The name of the model to download.</param>
    /// <param name="onProgress"></param>
    /// <param name="progress">An optional progress reporter.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the download progress.</returns>
    public async Task<DownloadProgress> DownloadModelAsync(
        string modelName,
        Action<DownloadProgress> onProgress = null,
        CancellationToken cancellationToken = default)
    {
        var downloadProgress = new DownloadProgress
        {
            ModelName = modelName,
            PercentComplete = 0,
            IsCompleted = false,
            Success = false,
            Message = "Initializing"
        };

        try
        {
            var request = new PullModelRequest
            {
                Model = modelName,
                Stream = true
            };
            _logger.LogInformation("Starting download for model: {ModelName}", modelName);

            cancellationToken.Register(() =>
            {
               _logger.LogInformation("Download cancelled for model: {ModelName}", modelName);
                downloadProgress.IsCompleted = true;
                downloadProgress.Success = false;
                downloadProgress.Message = "Download cancelled by user.";
                onProgress?.Invoke(downloadProgress);
            });

            await foreach (var response in _client.PullModelAsync(request, cancellationToken))
            {
                if (response == null)
                    continue;

                if (cancellationToken.IsCancellationRequested)
                {
                    return downloadProgress;
                }

                downloadProgress.PercentComplete = (int)Math.Round(response.Percent);
                downloadProgress.Message = response.Status;
                downloadProgress.IsCompleted = downloadProgress.PercentComplete >= 100;
                downloadProgress.Success = false;

                _logger.LogInformation("Reporting progress: {Percent}%", downloadProgress.PercentComplete);
                onProgress?.Invoke(downloadProgress);
                _logger.LogInformation("Progress reported.");
            }

            downloadProgress.IsCompleted = true;
            downloadProgress.Success = true;
            downloadProgress.Message = "Download completed successfully.";
            onProgress?.Invoke(downloadProgress);
            return downloadProgress;
        }
        catch (OperationCanceledException)
        {
            downloadProgress.IsCompleted = true;
            downloadProgress.Success = false;
            downloadProgress.Message = "Download aborted by user.";
            onProgress?.Invoke(downloadProgress);
            return downloadProgress;
        }
        catch (Exception ex)
        {
#if DEBUG
            throw;
#else
            downloadProgress.IsCompleted = true;
            downloadProgress.Success = false;
            downloadProgress.Message = $"Error: {ex.Message}";
            progress?.Report(downloadProgress);
            return downloadProgress;
#endif
        }
    }
}

/// <summary>
/// Represents an available model with its details.
/// </summary>
public class AvailableModel
{
    public string Name { get; set; }
    public string DisplayName { get; set; }
    public string Description { get; set; }
    public string Size { get; set; }
    public bool IsInstalled { get; set; }
}

/// <summary>
/// Represents the download progress of a model.
/// </summary>
public class DownloadProgress
{
    public string ModelName { get; set; }
    public int PercentComplete { get; set; }
    public bool IsCompleted { get; set; }
    public bool Success { get; set; }
    public string Message { get; set; } // Add this property
}

