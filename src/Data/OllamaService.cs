using Agent007.Models;
using Microsoft.Extensions.Options;
using OllamaSharp;
using OllamaSharp.Models;

namespace Agent007.Data;

public class OllamaService
{
    private readonly ILogger<OllamaService> _logger;
    private readonly OllamaApiClient _client;
    public readonly Dictionary<string, OllamaModel> _models = new Dictionary<string, OllamaModel>();
    private readonly Dictionary<string, CancellationTokenSource> _downloadCancellations = new();

    // Events
    public event EventHandler<ModelsChangedEventArgs>? ModelsChanged;
    public event EventHandler<ModelProgressEventArgs>? ModelProgressChanged;

    public OllamaService(IOptions<OllamaSettings> options, ILogger<OllamaService> logger)
    {
        _logger = logger;
        _logger.LogInformation("Ollama API URL: {Url}", options.Value.BaseUrl);
        _client = new OllamaApiClient(options.Value.BaseUrl);
    }

    private void OnModelsChanged(ModelsChangedEventArgs args)
    {
        ModelsChanged?.Invoke(this, args);
    }

    private void OnModelProgressChanged(ModelProgressEventArgs args)
    {
        ModelProgressChanged?.Invoke(this, args);
    }

    public async Task<IEnumerable<OllamaModel>> ListModelsAsync()
    {
        _logger.LogInformation("Listing available Ollama models");
        try
        {
            var models = await _client.ListLocalModelsAsync();
            _logger.LogInformation("Found {ModelCount} installed models", models.Count());

            var ollamaModels = models.Select(model => new OllamaModel
            {
                Status = OllamaModel.Statuses.Installed,
                Name = model.Name,
                DisplayName = model.Name,
                Description = GenerateDescription(model.Details),
                Size = FormatBytes(model.Size),
                DownloadProgress = 100.0f
            });

            // Update internal dictionary
            _models.Clear();
            foreach (var model in ollamaModels)
            {
                _models[model.Name] = model;
            }

            OnModelsChanged(new ModelsChangedEventArgs(ModelsChangedType.Refreshed, _models.Values));
            return ollamaModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Ollama models");
            throw;
        }
    }

    public async Task<OllamaModel> StartDownloadAsync(string modelName, IProgress<float>? progress = null, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting download for model: {ModelName}", modelName);

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _downloadCancellations[modelName] = cts;

        // Create temporary model entry for download tracking
        var model = new OllamaModel
        {
            Status = OllamaModel.Statuses.Downloading,
            Name = modelName,
            DisplayName = modelName,
            Description = "",
            Size = "Unknown",
            DownloadProgress = 0.0f
        };
        _models[modelName] = model;

        OnModelsChanged(new ModelsChangedEventArgs(ModelsChangedType.Added, model));

        // Start download in background - don't await here!
        _ = Task.Run(async () => await PerformDownloadAsync(modelName, model, progress, cts.Token));

        return model; // Return immediately
    }

    private async Task PerformDownloadAsync(string modelName, OllamaModel model, IProgress<float>? progress, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var status in _client.PullModelAsync(modelName, cancellationToken))
            {
                model.DownloadProgress = (float)status.Percent;
                progress?.Report(model.DownloadProgress);
                OnModelProgressChanged(new ModelProgressEventArgs(modelName, model.DownloadProgress));
                _logger.LogDebug("Download progress for {ModelName}: {Progress}%", modelName, model.DownloadProgress);
            }

            // Remove the temporary download entry
            _models.Remove(modelName);

            // Refresh the complete model list to get the actual downloaded model name
            await ListModelsAsync();

            _logger.LogInformation("Successfully downloaded model: {ModelName}", modelName);
        }
        catch (OperationCanceledException)
        {
            _models.Remove(modelName);
            OnModelsChanged(new ModelsChangedEventArgs(ModelsChangedType.Removed, modelName));
            _logger.LogInformation("Download cancelled for model: {ModelName}", modelName);
        }
        catch (Exception ex)
        {
            _models.Remove(modelName);
            OnModelsChanged(new ModelsChangedEventArgs(ModelsChangedType.Removed, modelName));
            _logger.LogError(ex, "Error downloading model: {ModelName}", modelName);
        }
        finally
        {
            _downloadCancellations.Remove(modelName);
        }
    }

    public async Task DeleteModelAsync(string modelName)
    {
        _logger.LogInformation("Deleting model: {ModelName}", modelName);
        try
        {
            await _client.DeleteModelAsync(modelName);
            if (_models.Remove(modelName))
            {
                OnModelsChanged(new ModelsChangedEventArgs(ModelsChangedType.Removed, modelName));
            }
            _logger.LogInformation("Successfully deleted model: {ModelName}", modelName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model: {ModelName}", modelName);
            throw;
        }
    }

    public void CancelDownload(string modelName)
    {
        if (_downloadCancellations.TryGetValue(modelName, out var cts))
        {
            cts.Cancel();
            _logger.LogInformation("Cancelled download for model: {ModelName}", modelName);
        }
    }

    private static string GenerateDescription(Details? details)
    {
        if (details == null)
            return "No details available";

        var parts = new List<string>();

        if (!string.IsNullOrEmpty(details.Family))
            parts.Add($"{details.Family} family");

        if (!string.IsNullOrEmpty(details.ParameterSize))
            parts.Add($"{details.ParameterSize} parameters");

        if (!string.IsNullOrEmpty(details.QuantizationLevel))
            parts.Add($"{details.QuantizationLevel} quantization");

        if (!string.IsNullOrEmpty(details.Format))
            parts.Add($"{details.Format} format");

        return parts.Count > 0 ? string.Join(", ", parts) : "Model details available";
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int counter = 0;
        decimal number = bytes;
        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }
        return $"{number:n1} {suffixes[counter]}";
    }
    /*
    public async Task<string> GenerateAsync(string prompt, string model = "llama3.2")
    {
        _logger.LogInformation("Generating text with model: {Model}", model);
        _logger.LogDebug("Prompt length: {PromptLength} characters", prompt.Length);

        try
        {
            var response = await _client.GenerateAsync(new GenerateRequest
            {
                Model = model,
                Prompt = prompt
            }).StreamToEndAsync();

            var responseText = response.Response ?? string.Empty;
            _logger.LogInformation("Text generation completed. Generated {ResponseLength} characters", responseText.Length);
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating text with model {Model}", model);
            throw;
        }
    }

    public async Task<string> ChatAsync(string message, string model = "llama3.2")
    {
        _logger.LogInformation("Chatting with model: {Model}", model);
        _logger.LogDebug("Message length: {MessageLength} characters", message.Length);

        try
        {
            var response = await _client.ChatAsync(new ChatRequest
            {
                Model = model,
                Messages = new List<OllamaSharp.Models.Chat.Message>
                {
                    new Message { Role = OllamaSharp.Models.Chat.ChatRole.User, Content = message }
                },
                Stream = true
            }).StreamToEndAsync();

            var content = response.Message?.Content ?? string.Empty;
            _logger.LogInformation("Chat response received. Length: {ResponseLength} characters", content.Length);
            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error chatting with model {Model}", model);
            throw;
        }
    }

    // TODO: Fetch model list from https://ollama.com/library
    public async Task<IEnumerable<Model>> ListModelsAsync()
    {
        _logger.LogInformation("Listing available Ollama models");
        try
        {
            var models = await _client.ListLocalModelsAsync();
            _logger.LogInformation("Found {ModelCount} installed models", models.Count());
            return models;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing Ollama models");
            throw;
        }
    }

    public string GetBaseUrl()
    {
        var url = Environment.GetEnvironmentVariable("OLLAMA_API_URL") ?? "http://localhost:11434";
        _logger.LogDebug("Using Ollama API base URL: {Url}", url);
        return url;
    }

    /// <summary>
    /// Creates a conversation between two different Ollama models
    /// </summary>
    public async Task<string> CreateMultiAgentConversationAsync(
        string initialPrompt,
        string firstModel = "llama3.2",
        string secondModel = "llama3.2",
        int maxTurns = 3)
    {
        _logger.LogInformation("Starting multi-agent conversation between models {FirstModel} and {SecondModel} for {MaxTurns} turns",
            firstModel, secondModel, maxTurns);
        _logger.LogDebug("Initial prompt length: {PromptLength} characters", initialPrompt.Length);

        try
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
                _logger.LogInformation("Starting conversation turn {TurnNumber}/{MaxTurns}", i + 1, maxTurns);

                // First model responds
                conversation.Add(new Message
                {
                    Role = ChatRole.User,
                    Content = latestResponse
                });

                _logger.LogDebug("Sending message to first model {Model}", firstModel);
                var firstModelResponse = await _client.ChatAsync(new ChatRequest
                {
                    Model = firstModel,
                    Messages = conversation
                }).StreamToEndAsync();

                var firstModelContent = firstModelResponse.Message?.Content ?? string.Empty;
                latestResponse = firstModelContent;
                _logger.LogDebug("First model response length: {ResponseLength} characters", firstModelContent.Length);

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

                _logger.LogDebug("Sending message to second model {Model}", secondModel);
                var secondModelResponse = await _client.ChatAsync(new ChatRequest
                {
                    Model = secondModel,
                    Messages = conversation
                }).StreamToEndAsync();

                var secondModelContent = secondModelResponse.Message?.Content ?? string.Empty;
                latestResponse = secondModelContent;
                _logger.LogDebug("Second model response length: {ResponseLength} characters", secondModelContent.Length);

                conversation.Add(new Message
                {
                    Role = ChatRole.Assistant,
                    Content = latestResponse
                });
            }

            _logger.LogInformation("Multi-agent conversation completed with {TurnCount} turns and {MessageCount} messages",
                maxTurns, conversation.Count);

            // Return the full conversation as a formatted string
            return FormatConversation(conversation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in multi-agent conversation between {FirstModel} and {SecondModel}",
                firstModel, secondModel);
            throw;
        }
    }

    private string FormatConversation(List<Message> conversation)
    {
        _logger.LogDebug("Formatting conversation with {MessageCount} messages", conversation.Count);
        var result = new System.Text.StringBuilder();

        foreach (var message in conversation)
        {
            result.AppendLine($"{message.Role}: {message.Content}");
            result.AppendLine();
        }

        return result.ToString();
    }
    public async Task<string> GenerateWithTemplateAsync(
        string model,
        string input,
        string template)
    {
        _logger.LogInformation("Generating text with template using model: {Model}", model);
        _logger.LogDebug("Input length: {InputLength} characters, template length: {TemplateLength} characters",
            input.Length, template?.Length ?? 0);

        try
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
            var responseText = response.Response ?? string.Empty;
            _logger.LogInformation("Template-based generation completed. Generated {ResponseLength} characters", responseText.Length);
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating with template using model {Model}", model);
            throw;
        }
    }
    */
    /*
    public async Task<string> UseTemplateAsync(string prompt, string templateContent, string model = "llama3.2")
    {
        _logger.LogInformation("Using custom template with model: {Model}", model);
        _logger.LogDebug("Prompt length: {PromptLength} characters, template content length: {TemplateLength} characters",
            prompt.Length, templateContent?.Length ?? 0);

        try
        {
            using var client = new HttpClient();
            client.BaseAddress = new Uri(GetBaseUrl());

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

            _logger.LogDebug("Sending template request to Ollama API");
            var response = await client.PostAsync("/api/generate", content);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync();
            var jsonResponse = JsonSerializer.Deserialize<JsonElement>(result);

            var responseText = jsonResponse.GetProperty("response").GetString() ?? string.Empty;
            _logger.LogInformation("Template-based generation through HTTP completed. Generated {ResponseLength} characters",
                responseText.Length);
            return responseText;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error using template with model {Model}", model);
            throw;
        }
    }



    public class DownloadProgress
    {
        public required string ModelName { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public int PercentComplete { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class DownloadStatus
    {
        public bool Success { get; set; }
        public required string Message { get; set; }
    }
    */
    /*
    public async Task<List<AvailableModel>> GetAvailableModelsAsync()
    {
        _logger.LogInformation("Getting available Ollama models");
        try
        {
            // Since OllamaSharp doesn't provide a direct method to list downloadable models,
            // we'll use a curated list of popular models
            var availableModels = new List<AvailableModel>
            {
                new AvailableModel { Name = "llama3.2:latest", DisplayName = "Llama 3.2 8B", Description = "Meta's Llama 3.2 1B model", Size = "2.0 GB" },
                new AvailableModel { Name = "llama3", DisplayName = "Llama 3 8B", Description = "Meta's Llama 3 8B model", Size = "4.7 GB" },
                new AvailableModel { Name = "granite3.2:2b", DisplayName = "Granite 3.2 2B", Description = "IBM Granite-3.2 2B model.", Size = "1.5 GB" },
                new AvailableModel { Name = "llama3:8b", DisplayName = "Llama 3 8B", Description = "Meta's Llama 3 8B model", Size = "4.7 GB" },
                new AvailableModel { Name = "llama3:70b", DisplayName = "Llama 3 70B", Description = "Meta's Llama 3 70B model", Size = "39.8 GB" },
                new AvailableModel { Name = "mistral", DisplayName = "Mistral 7B", Description = "Mistral AI's 7B model", Size = "4.1 GB" },
                new AvailableModel { Name = "mixtral", DisplayName = "Mixtral 8x7B", Description = "Mistral AI's mixture of experts model", Size = "26.1 GB" },
                new AvailableModel { Name = "gemma:2b", DisplayName = "Gemma 2B", Description = "Google's lightweight Gemma model", Size = "1.4 GB" },
                new AvailableModel { Name = "gemma:7b", DisplayName = "Gemma 7B", Description = "Google's Gemma model", Size = "4.8 GB" },
                new AvailableModel { Name = "phi3", DisplayName = "Phi-3 Mini", Description = "Microsoft's Phi-3 Mini model", Size = "4.1 GB" },
                new AvailableModel { Name = "codellama", DisplayName = "CodeLlama 7B", Description = "Meta's CodeLlama for code generation", Size = "4.2 GB" },
                new AvailableModel { Name = "nous-hermes2", DisplayName = "Nous Hermes 2", Description = "Fine-tuned model by Nous Research", Size = "4.5 GB" },
                new AvailableModel { Name = "llava", DisplayName = "LLaVA", Description = "Large language and vision assistant", Size = "4.5 GB" },
                new AvailableModel { Name = "neural-chat", DisplayName = "Neural Chat", Description = "Intel's Neural Chat model", Size = "4.1 GB" },
                new AvailableModel { Name = "solar", DisplayName = "SOLAR", Description = "Upstage's SOLAR model", Size = "4.2 GB" },
                new AvailableModel { Name = "stable-code", DisplayName = "Stable Code", Description = "Model optimized for code generation", Size = "4.1 GB" },
                new AvailableModel { Name = "whisper", DisplayName = "Whisper", Description = "OpenAI's speech recognition model", Size = "1.5 GB" }
            };

            // Check which models are already installed
            var installedModels = await ListModelsAsync();
            var installedModelNames = installedModels.Select(m => m.Name.Split(':')[0].ToLower()).ToHashSet();

            foreach (var model in availableModels)
            {
                var baseName = model.Name.Split(':')[0].ToLower();
                model.IsInstalled = installedModelNames.Contains(baseName);
            }

            _logger.LogInformation("Retrieved {TotalCount} available models, {InstalledCount} are installed",
                availableModels.Count, availableModels.Count(m => m.IsInstalled));
            return availableModels;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available Ollama models");
            throw;
        }
    }
    */
    /*
    public async Task<DownloadStatus> DeleteModelAsync(string modelName)
    {
        _logger.LogInformation("Deleting model: {ModelName}", modelName);
        try
        {
            var deleteRequest = new DeleteModelRequest
            {
                Model = modelName
            };

            await _client.DeleteModelAsync(deleteRequest);
            _logger.LogInformation("Model {ModelName} deleted successfully", modelName);
            return new DownloadStatus
            {
                Success = true,
                Message = "Model deleted successfully"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting model {ModelName}", modelName);
            return new DownloadStatus
            {
                Success = false,
                Message = $"Error deleting model: {ex.Message}"
            };
        }
    }

    // TODO: Allow the user to specify a model
    public async Task<DownloadStatus> DownloadModelAsync(string modelName, IProgress<DownloadProgress>? progress = null)
    {
        _logger.LogInformation("Starting download of model: {ModelName}", modelName);

        try
        {
            IProgress<PullModelResponse> progressReporter = new Progress<PullModelResponse>((response) =>
            {
                if (progress != null && response?.Status != null)
                {
                    // Ollama API doesn't consistently provide download stats through the client
                    // But we'll handle what we can
                    var downloadProgress = new DownloadProgress
                    {
                        ModelName = modelName,
                        PercentComplete = CalculateProgress(response)
                    };

                    progress.Report(downloadProgress);
                    _logger.LogDebug("Model download progress: {ProgressPercent}%", downloadProgress.PercentComplete);
                }
            });

            // Request the model pull with streaming to get progress updates
            var pullRequest = new PullModelRequest
            {
                Model = modelName,
                Stream = true
            };

            // Use the streaming API to get progress updates
            await foreach (var update in _client.PullModelAsync(pullRequest))
            {
                if (update != null)
                {
                    progressReporter.Report(update);

                    // Check if download is complete
                    if (update.Status == "success")
                    {
                        progress?.Report(new DownloadProgress
                        {
                            ModelName = modelName,
                            PercentComplete = 100,
                            IsCompleted = true
                        });

                        _logger.LogInformation("Model {ModelName} download completed successfully", modelName);
                        return new DownloadStatus
                        {
                            Success = true,
                            Message = "Model downloaded successfully"
                        };
                    }
                }
            }

            // If we get here, we should check if the model is now available
            _logger.LogInformation("Checking if model {ModelName} is now available", modelName);
            var models = await ListModelsAsync();
            if (models.Any(m => m.Name == modelName || m.Name.StartsWith(modelName + ":")))
            {
                _logger.LogInformation("Model {ModelName} is now available", modelName);

                return new DownloadStatus
                {
                    Success = true,
                    Message = "Model download completed"
                };
            }
            else
            {
                _logger.LogWarning("Model {ModelName} download may have failed or is still in progress", modelName);
                return new DownloadStatus
                {
                    Success = false,
                    Message = "Download may have failed or is still in progress"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading model {ModelName}", modelName);
            return new DownloadStatus
            {
                Success = false,
                Message = $"Error downloading model: {ex.Message}"
            };
        }
    }

    private int CalculateProgress(PullModelResponse response)
    {
        // The PullModelResponse doesn't have consistent progress information
        // We'll make a best effort estimate based on what's available
        if (response.Completed == response.Total && response.Total > 0)
        {
            return (int)((response.Completed / (double)response.Total) * 100);
        }
        else if (!string.IsNullOrEmpty(response.Status))
        {
            if (response.Status == "downloading")
                return 50; // Generic progress indicator if we don't have specifics
            else if (response.Status == "success")
                return 100;
        }
        return 0; // Default if we can't determine progress
    }
    */
}
