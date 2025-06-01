using Agent007.Data;
using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;

namespace Agent007.LLM
{
    using Models.Chat;
    using System.Collections.Generic;
    using static OllamaSharp.Models.Chat.Message;

    public class OllamaBackend : ILLMBackend
    {
        private readonly OllamaApiClient _client;
        private readonly string _model;
        private readonly ChatDbContext _dbContext;
        private readonly int? _contextLength; // null means use default context length
        private bool _isBusy;
        private ILogger<OllamaBackend> _logger;
       

        public string SystemMessage { get; set; }

        public OllamaBackend(OllamaApiClient client, string model, string systemPrompt, ChatDbContext dbContext, ILogger<OllamaBackend> logger, int? contextLength)
        {
            _client = client;
            _model = model;
            SystemMessage = systemPrompt;
            _dbContext = dbContext;
            _logger = logger;
            _contextLength = contextLength;
        }

        public async Task GenerateAsync(
            IEnumerable<Message> history,
            Message targetAssistantMessage,
            IEnumerable<IToolInterface>? tools = null,
            Func<string, Task<Message>>? createToolMessageCallback = null,
            CancellationToken cancellationToken = default)
        {
            if (_isBusy)
                throw new InvalidOperationException("Agent is already generating. Complete or cancel the previous task before starting a new one.");

            _isBusy = true;

            try
            {
                targetAssistantMessage.Role = "assistant";
                targetAssistantMessage.Status = "generating";
                targetAssistantMessage.Body = string.Empty;

                var chatMessages = new List<OllamaSharp.Models.Chat.Message>();

                if (!string.IsNullOrWhiteSpace(SystemMessage))
                {
                    chatMessages.Add(new OllamaSharp.Models.Chat.Message
                    {
                        Role = "system",
                        Content = SystemMessage
                    });
                }

                // Add all history messages (including the latest user message)
                foreach (var msg in history)
                {
                    chatMessages.Add(new OllamaSharp.Models.Chat.Message
                    {
                        Role = msg.Role,
                        Content = msg.Body
                    });
                }

                // Convert tools to OllamaSharp format
                var ollamaTools = tools?.Select(tool => tool.GetToolDefinition()).ToArray();

                var request = new ChatRequest
                {
                    Model = _model,
                    Messages = chatMessages,
                    Stream = true,
                    Tools = ollamaTools,
                    Options = _contextLength.HasValue ? new RequestOptions { NumCtx = _contextLength.Value } : null
                };


                var toolCalls = new List<ToolCall>();
                var toolLookup = tools?.ToDictionary(t => t.GetToolDefinition().Function.Name, t => t)
                    ?? new Dictionary<string, IToolInterface>();

                await foreach (var chunk in _client.ChatAsync(request, cancellationToken))
                {
                    if (chunk?.Message?.Content is { Length: > 0 } content)
                    {
                        targetAssistantMessage.Body += content;
                    }

                    // Collect tool calls
                    if (chunk?.Message?.ToolCalls?.Any() == true)
                    {
                        toolCalls.AddRange(chunk.Message.ToolCalls);
                    }
                }


                // Mark the assistant message as complete (it now contains the function call)
                targetAssistantMessage.Status = "complete";

                // If there were tool calls, execute them and continue the conversation
                if (toolCalls.Any() && createToolMessageCallback != null)
                {
                    await ExecuteToolsWithCallback(toolCalls, toolLookup, chatMessages, createToolMessageCallback, cancellationToken);
                }
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Task ExecuteToolsWithCallback(
            List<ToolCall> toolCalls,
            Dictionary<string, IToolInterface> toolLookup,
            List<OllamaSharp.Models.Chat.Message> chatMessages,
            Func<string, Task<Message>> createToolMessageCallback,
            CancellationToken cancellationToken)
        {
            var toolResults = new List<string>();

            foreach (var toolCall in toolCalls)
            {
                try
                {
                    if (toolCall?.Function == null)
                    {
                        toolResults.Add("Error: Invalid tool call");
                        continue;
                    }

                    var functionName = toolCall.Function.Name;

                    if (!toolLookup.TryGetValue(functionName, out var tool))
                    {
                        toolResults.Add($"Error: Unknown tool '{functionName}'");
                        continue;
                    }

                    // Use callback to create tool message at root level
                    var toolMessage = await createToolMessageCallback(functionName);

                    // Debug: Log the raw arguments to understand the structure
                    _logger.LogWarning("=== TOOL CALL DEBUG ===");
                    _logger.LogWarning("Raw toolCall.Function.Arguments: {Args}",
                                     System.Text.Json.JsonSerializer.Serialize(toolCall.Function.Arguments));
                    _logger.LogWarning("Arguments type: {Type}", toolCall.Function.Arguments?.GetType());

                    // Parse the tool call arguments properly
                    var toolCallParser = new Tools.ParamParser(toolCall.Function.Arguments);

                    // If the arguments are nested under an "arguments" key, extract them
                    // Otherwise, use the arguments directly
                    var argumentsParser = toolCallParser.ContainsKey("arguments")
                        ? toolCallParser.Get("arguments")?.AsObject()
                        : toolCallParser;

                    // Debug: Show what the tool will actually receive
                    _logger.LogWarning("Tool will receive - sides value: {Sides}",
                                     argumentsParser?.Get("sides")?.AsInt() ?? -1);

                    // Execute the tool
                    await tool.ExecuteAsync(
                        argumentsParser ?? new Tools.ParamParser(null),
                        toolMessage,
                        cancellationToken);

                    // Tool should have set toolMessage.Body with the result
                    toolResults.Add(toolMessage.Body);

                    // Update the tool message status and save
                    toolMessage.Status = "complete";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var errorResult = $"Error executing tool: {ex.Message}";
                    toolResults.Add(errorResult);

                    // Create error tool message at root level
                    var errorMessage = await createToolMessageCallback(toolCall.Function?.Name ?? "Unknown");
                    errorMessage.Body = errorResult;
                    errorMessage.Status = "error";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
            }

            // Add tool results to chat history for LLM context
            foreach (var result in toolResults)
            {
                chatMessages.Add(new OllamaSharp.Models.Chat.Message
                {
                    Role = "tool",
                    Content = result
                });
            }

            // Create a new assistant message for the follow-up response
            var followUpMessage = await createToolMessageCallback("Assistant");
            followUpMessage.Role = "assistant";
            followUpMessage.Status = "generating";
            followUpMessage.Body = string.Empty;

            // Continue the conversation with tool results
            var followUpRequest = new ChatRequest
            {
                Model = _model,
                Messages = chatMessages,
                Stream = true
            };

            await foreach (var chunk in _client.ChatAsync(followUpRequest, cancellationToken))
            {
                if (chunk?.Message?.Content is { Length: > 0 } content)
                {
                    followUpMessage.Body += content;
                }
            }

            followUpMessage.Status = "complete";
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        // Keep the old method for backward compatibility, but mark it as obsolete
        [Obsolete("Use the overload with createToolMessageCallback parameter")]
        private async Task ExecuteTools(
            List<ToolCall> toolCalls,
            Dictionary<string, IToolInterface> toolLookup,
            Message targetAssistantMessage,
            List<OllamaSharp.Models.Chat.Message> chatMessages,
            CancellationToken cancellationToken)
        {
            // Old implementation for backward compatibility
            var toolResults = new List<string>();

            foreach (var toolCall in toolCalls)
            {
                try
                {
                    if (toolCall?.Function == null)
                    {
                        toolResults.Add("Error: Invalid tool call");
                        continue;
                    }

                    var functionName = toolCall.Function.Name;

                    if (!toolLookup.TryGetValue(functionName, out var tool))
                    {
                        toolResults.Add($"Error: Unknown tool '{functionName}'");
                        continue;
                    }

                    // Create tool result message and save it to database
                    var toolMessage = new Message
                    {
                        ConversationId = targetAssistantMessage.ConversationId,
                        ParentId = targetAssistantMessage.Id,
                        Role = "tool",
                        AgentName = functionName,
                        Body = "", // Starts empty (shows spinner in UI)
                        Status = "generating",
                        CreatedAt = DateTime.UtcNow
                    };

                    // Save the tool message to database first (so it has an ID)
                    _dbContext.Messages.Add(toolMessage);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Add to parent's children collection
                    targetAssistantMessage.Children.Add(toolMessage);

                    // Execute the tool
                    await tool.ExecuteAsync(
                        new Tools.ParamParser(toolCall.Function.Arguments),
                        toolMessage,
                        cancellationToken);

                    // Tool should have set toolMessage.Body with the result
                    toolResults.Add(toolMessage.Body);

                    // Update the tool message status and save
                    toolMessage.Status = "complete";
                    await _dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    var errorResult = $"Error executing tool: {ex.Message}";
                    toolResults.Add(errorResult);

                    // Create error tool message
                    var errorMessage = new Message
                    {
                        ConversationId = targetAssistantMessage.ConversationId,
                        ParentId = targetAssistantMessage.Id,
                        Role = "tool",
                        AgentName = toolCall.Function?.Name ?? "Unknown",
                        Body = errorResult,
                        Status = "error",
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.Messages.Add(errorMessage);
                    await _dbContext.SaveChangesAsync(cancellationToken);
                    targetAssistantMessage.Children.Add(errorMessage);
                }
            }

            // Continue conversation with tool results
            await ContinueWithToolResults(chatMessages, toolResults, targetAssistantMessage, cancellationToken);
        }

        private async Task ContinueWithToolResults(
            List<OllamaSharp.Models.Chat.Message> chatMessages,
            List<string> toolResults,
            Message targetAssistantMessage,
            CancellationToken cancellationToken)
        {
            // Add tool results to the conversation for the LLM to see
            foreach (var result in toolResults)
            {
                chatMessages.Add(new OllamaSharp.Models.Chat.Message
                {
                    Role = "tool",
                    Content = result
                });
            }

            // Continue the conversation with tool results
            var followUpRequest = new ChatRequest
            {
                Model = _model,
                Messages = chatMessages,
                Stream = true
            };

            targetAssistantMessage.Body += "\n\n";

            await foreach (var chunk in _client.ChatAsync(followUpRequest, cancellationToken))
            {
                if (chunk?.Message?.Content is { Length: > 0 } content)
                {
                    targetAssistantMessage.Body += content;
                }
            }
        }
    }
}