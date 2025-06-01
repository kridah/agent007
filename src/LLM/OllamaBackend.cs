using Agent007.Tools;
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
        private bool _isBusy;

        public string SystemMessage { get; set; }

        public OllamaBackend(OllamaApiClient client, string model, string systemPrompt)
        {
            _client = client;
            _model = model;
            SystemMessage = systemPrompt;
        }

        public async Task GenerateAsync(
            IEnumerable<Message> history,
            Message userMessage,
            Message targetAssistantMessage,
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

                foreach (var msg in history)
                {
                    chatMessages.Add(new OllamaSharp.Models.Chat.Message
                    {
                        Role = msg.Role,
                        Content = msg.Body
                    });
                }

                chatMessages.Add(new OllamaSharp.Models.Chat.Message
                {
                    Role = userMessage.Role,
                    Content = userMessage.Body
                });

                // Create tool instances for the request
                var tools = new object[]
                {
                    new RollDiceTool()
                };

                var request = new ChatRequest
                {
                    Model = _model,
                    Messages = chatMessages,
                    Stream = true,
                    Tools = tools // Add tools to the original working approach
                };

                var responseMessage = new OllamaSharp.Models.Chat.Message();
                var toolCalls = new List<ToolCall>();

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

                // If there were tool calls, execute them and continue the conversation
                if (toolCalls.Any())
                {
                    targetAssistantMessage.Body += "\n\n[Executing tools...]";

                    // Execute tools and create child messages
                    foreach (var toolCall in toolCalls)
                    {
                        var toolResult = await ExecuteTool(toolCall);

                        // Create child message for tool execution
                        var toolMessage = new Message
                        {
                            ConversationId = targetAssistantMessage.ConversationId,
                            ParentId = targetAssistantMessage.Id,
                            Role = "tool",
                            AgentName = toolCall.Function?.Name ?? "Unknown Tool",
                            Body = toolResult,
                            Status = "complete",
                            CreatedAt = DateTime.UtcNow
                        };

                        targetAssistantMessage.Children.Add(toolMessage);
                    }

                    // Continue conversation with tool results
                    await ContinueWithToolResults(chatMessages, toolCalls, targetAssistantMessage, cancellationToken);
                }

                targetAssistantMessage.Status = "complete";
            }
            finally
            {
                _isBusy = false;
            }
        }

        private async Task<string> ExecuteTool(ToolCall toolCall)
        {
            try
            {
                if (toolCall?.Function == null)
                    return "Error: Invalid tool call";

                var functionName = toolCall.Function.Name;
                var args = toolCall.Function.Arguments;

                return functionName switch
                {
                    "RollDice" => ExecuteRollDice(args),
                    _ => $"Error: Unknown tool '{functionName}'"
                };
            }
            catch (Exception ex)
            {
                return $"Error executing tool: {ex.Message}";
            }
        }

        private string ExecuteRollDice(IDictionary<string, object?>? args)
        {
            var sides = 6;

            if (args != null && args.ContainsKey("sides") && int.TryParse(args["sides"]?.ToString(), out var sidesVal))
                sides = sidesVal;

            var result = Random.Shared.Next(1, sides + 1);
            return $"Rolled {sides}-sided dice: {result}";
        }

        private async Task ContinueWithToolResults(
            List<OllamaSharp.Models.Chat.Message> chatMessages,
            List<ToolCall> toolCalls,
            Message targetAssistantMessage,
            CancellationToken cancellationToken)
        {
            // Add tool results to the conversation
            foreach (var toolCall in toolCalls)
            {
                chatMessages.Add(new OllamaSharp.Models.Chat.Message
                {
                    Role = "tool",
                    Content = await ExecuteTool(toolCall)
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