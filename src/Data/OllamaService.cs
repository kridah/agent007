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

    // demo tool call
    public async Task<Message> CalculateFunction(string message, string model = "llama3.2")
    {
        Console.WriteLine("Calculating expression with model: {0}", model);
        var responseStream = _client.ChatAsync(new ChatRequest
        {
            Model = model,
            Messages = new List<Message>
            {
                new Message { Role = ChatRole.System, Content = "You are a calculator. Please evaluate the expression provided. Respond with a short poem related to the result." },
                new Message { Role = ChatRole.User, Content = message }
            },
            Stream = false,
            Tools = new List<Tool>
            {
                new Tool
                {
                    Type = "function",
                    Function = new Function
                    {
                        Name = "calculate",
                        Description = "Calculates a mathematical expression",
                        Parameters = new Parameters
                        {
                            Type = "object",
                            Properties = new Dictionary<string, Property>
                            {
                                {
                                    "expression",
                                    new Property
                                    {
                                        Type = "string",
                                        Description = "The mathematical expression to calculate"
                                    }
                                }
                            },
                            Required = new List<string> { "expression" }
                        }
                    }
                }
            }
        });
        await foreach (var responseChunk in responseStream)
        {
            if (responseChunk?.Message.ToolCalls != null && responseChunk.Message.ToolCalls.Any())
            {
                // Extract the expression from the tool call
                var toolCall = responseChunk.Message.ToolCalls.First();
                var expression = toolCall.Function.Arguments["expression"].ToString();

                // Evaluate the expression in C#
                var result = EvaluateMathExpression(expression);

                // Return a message with the result
                return new Message
                {
                    Role = ChatRole.Assistant,
                    Content = result.ToString()
                };
            }

            // Normal message, just return it
            return responseChunk.Message;
        }

        // If we reach here, no tool call was made
        return new Message
        {
            Role = ChatRole.Assistant,
            Content = "No valid mathematical expression found in the message."
        };
    }

    // Example math evaluation (simple, for demo)
    private object EvaluateMathExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return "Invalid expression: empty input.";

        // Only allow digits, whitespace, decimal points, and arithmetic operators
        var allowedPattern = @"^[\d\s\.\+\-\*\/\(\)]+$";
        if (!System.Text.RegularExpressions.Regex.IsMatch(expression, allowedPattern))
            return "Invalid expression: only numbers and + - * / ( ) are allowed.";

        try
        {
            // Prevent dangerous expressions (e.g., division by zero)
            var table = new System.Data.DataTable();
            table.CaseSensitive = false;
            var value = table.Compute(expression, "");
            return value;
        }
        catch
        {
            return "Invalid expression: could not evaluate.";
        }
    }

    public async Task<string> GenerateAsync(string prompt, string model = "llama3.2")
    {
        var response = await _client.GenerateAsync(new GenerateRequest
        {
            Model = model,
            Prompt = prompt,
        }).StreamToEndAsync();

        return response.Response;
    }

    public async Task<string> ChatAsync(string message, string model = "llama3.2")
    {
        _logger.LogInformation("Chatting with model: {Model}", model);
        var response = await _client.ChatAsync(new ChatRequest
        {
            Model = model,
            Messages = new List<Message>
            {
                new Message { Role = ChatRole.User, Content = message }
            },
            Stream = true,
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
        string firstModel = "llama3.2",
        string secondModel = "llama3.2",
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

    public async Task<string> UseTemplateAsync(string prompt, string templateContent, string model = "llama3.2")
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

    public class AvailableModel
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Size { get; set; }
        public bool IsInstalled { get; set; }
    }

    public class DownloadProgress
    {
        public string ModelName { get; set; }
        public long BytesDownloaded { get; set; }
        public long TotalBytes { get; set; }
        public int PercentComplete { get; set; }
        public bool IsCompleted { get; set; }
    }

    public class DownloadStatus
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public async Task<List<AvailableModel>> GetAvailableModelsAsync()
    {
        // Since OllamaSharp doesn't provide a direct method to list downloadable models,
        // we'll use a curated list of popular models
        var availableModels = new List<AvailableModel>
        {
            new AvailableModel
            {
                Name = "llama3", DisplayName = "Llama 3 8B", Description = "Meta's Llama 3 8B model", Size = "4.7 GB"
            },
            new AvailableModel
            {
                Name = "granite3.2:2b", DisplayName = "Granite 3.2 2B",
                Description =
                    "Granite-3.2 is a family of long-context AI models from IBM Granite fine-tuned for thinking capabilities.",
                Size = "1.5 GB"
            },
            new AvailableModel
            {
                Name = "llama3:8b", DisplayName = "Llama 3 8B", Description = "Meta's Llama 3 8B model", Size = "4.7 GB"
            },
            new AvailableModel
            {
                Name = "llama3:70b", DisplayName = "Llama 3 70B", Description = "Meta's Llama 3 70B model",
                Size = "39.8 GB"
            },
            new AvailableModel
            {
                Name = "mistral", DisplayName = "Mistral 7B", Description = "Mistral AI's 7B model", Size = "4.1 GB"
            },
            new AvailableModel
            {
                Name = "mixtral", DisplayName = "Mixtral 8x7B", Description = "Mistral AI's mixture of experts model",
                Size = "26.1 GB"
            },
            new AvailableModel
            {
                Name = "gemma:2b", DisplayName = "Gemma 2B", Description = "Google's lightweight Gemma model",
                Size = "1.4 GB"
            },
            new AvailableModel
                { Name = "gemma:7b", DisplayName = "Gemma 7B", Description = "Google's Gemma model", Size = "4.8 GB" },
            new AvailableModel
            {
                Name = "phi3", DisplayName = "Phi-3 Mini", Description = "Microsoft's Phi-3 Mini model", Size = "4.1 GB"
            },
            new AvailableModel
            {
                Name = "codellama", DisplayName = "CodeLlama 7B", Description = "Meta's CodeLlama for code generation",
                Size = "4.2 GB"
            },
            new AvailableModel
            {
                Name = "nous-hermes2", DisplayName = "Nous Hermes 2", Description = "Fine-tuned model by Nous Research",
                Size = "4.5 GB"
            },
            new AvailableModel
            {
                Name = "llava", DisplayName = "LLaVA", Description = "Large language and vision assistant",
                Size = "4.5 GB"
            },
            new AvailableModel
            {
                Name = "neural-chat", DisplayName = "Neural Chat", Description = "Intel's Neural Chat model",
                Size = "4.1 GB"
            },
            new AvailableModel
                { Name = "solar", DisplayName = "SOLAR", Description = "Upstage's SOLAR model", Size = "4.2 GB" },
            new AvailableModel
            {
                Name = "stable-code", DisplayName = "Stable Code", Description = "Model optimized for code generation",
                Size = "4.1 GB"
            },
            new AvailableModel
            {
                Name = "whisper", DisplayName = "Whisper", Description = "OpenAI's speech recognition model",
                Size = "1.5 GB"
            }
        };

        // Check which models are already installed
        var installedModels = await ListModelsAsync();
        var installedModelNames = installedModels.Select(m => m.Name.Split(':')[0].ToLower()).ToHashSet();

        foreach (var model in availableModels)
        {
            var baseName = model.Name.Split(':')[0].ToLower();
            model.IsInstalled = installedModelNames.Contains(baseName);
        }

        return availableModels;
    }

    public async Task<DownloadStatus> DownloadModelAsync(string modelName, IProgress<DownloadProgress> progress = null)
    {
        try
        {
            IProgress<PullModelResponse> progressReporter = new Progress<PullModelResponse>((response) =>
            {
                if (progress != null && response.Status != null)
                {
                    // Ollama API doesn't consistently provide download stats through the client
                    // But we'll handle what we can
                    var downloadProgress = new DownloadProgress
                    {
                        ModelName = modelName,
                        PercentComplete = CalculateProgress(response)
                    };

                    progress.Report(downloadProgress);
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
                progressReporter.Report(update);

                // Check if download is complete
                if (update.Status == "success")
                {
                    progress.Report(new DownloadProgress
                    {
                        ModelName = modelName,
                        PercentComplete = 100,
                        IsCompleted = true
                    });

                    return new DownloadStatus
                    {
                        Success = true,
                        Message = "Model downloaded successfully"
                    };
                }
            }

            // If we get here, we should check if the model is now available
            var models = await ListModelsAsync();
            if (models.Any(m => m.Name == modelName || m.Name.StartsWith(modelName + ":")))
            {
                return new DownloadStatus
                {
                    Success = true,
                    Message = "Model download completed"
                };
            }
            else
            {
                return new DownloadStatus
                {
                    Success = false,
                    Message = "Download may have failed or is still in progress"
                };
            }
        }
        catch (Exception ex)
        {
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
}