using OllamaSharp.Models.Chat;
using Agent007.LLM;
using Agent007.Data;
using Agent007.Models.Chat;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace Agent007.Tools
{
    public class AssistantTool : IToolInterface
    {
        private readonly ChatDbContext _dbContext;
        private readonly IServiceProvider _serviceProvider;
        private string _functionName = "assistant";
        private string _modelName = "qwen3:4b";
        private string _systemMessage = "You are a helpful assistant.";
        private string _toolDescription;
        private string _compressResponseMessage;
        private int? _contextLength = null; // null means use default
        private bool _thinking = false;

        // DI-compatible constructor
        public AssistantTool(ChatDbContext dbContext, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _serviceProvider = serviceProvider;
        }

        // Configuration method to set parameters
        public AssistantTool Configure(string functionName, string toolDescription, string modelName, string systemMessage, string compressResponseMessage = "Can you provide a very concise summary of your answer?", int? contextLength = null, bool thinking = false)
        {
            _functionName = functionName;
            _toolDescription = toolDescription;
            _modelName = modelName;
            _systemMessage = systemMessage;
            _compressResponseMessage = compressResponseMessage;
            _contextLength = contextLength;
            _thinking = thinking;
            return this;
        }

        public async Task ExecuteAsync(
            ParamParser parameters,
            Agent007.Models.Chat.Message toolResultMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Extract the prompt parameter
                var prompt = parameters.Get("prompt")?.AsString() ?? "";
                if (string.IsNullOrEmpty(prompt))
                {
                    throw new ArgumentException("Prompt parameter is required");
                }

                // Add user message (the question from root agent)
                var userMessage = toolResultMessage.AddMessage("user", prompt);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Create assistant message for the sub-agent response
                var assistantMessage = toolResultMessage.AddMessage("assistant", "", _functionName);
                assistantMessage.Status = "generating";
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Create the sub-agent
                var backendFactory = _serviceProvider.GetRequiredService<LLMBackendFactory>();
                var backend = backendFactory.CreateBackend(_modelName, _systemMessage, _contextLength);
                var subAgent = new Agent(backend);

                // Create a callback for the sub-agent to create its own tool messages
                Func<string, Task<Agent007.Models.Chat.Message>> createSubToolMessageCallback = async (toolName) =>
                {
                    var subToolMessage = new Agent007.Models.Chat.Message
                    {
                        ConversationId = toolResultMessage.ConversationId,
                        ParentId = assistantMessage.Id, // Child of the assistant message
                        Role = "tool",
                        AgentName = toolName,
                        Body = "",
                        Status = "generating",
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.Messages.Add(subToolMessage); // ✅ Add to DbContext first
                    await _dbContext.SaveChangesAsync(cancellationToken); // ✅ Now save it

                    return subToolMessage; // ✅ Return the saved message with valid ID
                };

                // Prepare message history for sub-agent (empty for new conversation)
                var subAgentHistory = new List<Agent007.Models.Chat.Message> { userMessage };

                // Get available tools for the sub-agent
                var subAgentTools = GetSubAgentTools();

                // Generate response using the sub-agent
                await subAgent.GenerateAsync(
                    subAgentHistory,
                    assistantMessage,
                    subAgentTools,
                    createSubToolMessageCallback,
                    cancellationToken);

                // Check if we need to compress the response
                string finalResponse = assistantMessage.Body;
                if (!string.IsNullOrEmpty(assistantMessage.Body) && assistantMessage.Body.Length > 100)
                {
                    // Create a compression request
                    var compressionUserMessage = toolResultMessage.AddMessage("user", _compressResponseMessage);
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Create assistant message for the compressed response
                    var compressionAssistantMessage = toolResultMessage.AddMessage("assistant", "", $"{_functionName}_summary");
                    compressionAssistantMessage.Status = "generating";
                    await _dbContext.SaveChangesAsync(cancellationToken);

                    // Prepare history for compression (include original conversation + compression request)
                    var compressionHistory = new List<Agent007.Models.Chat.Message>
                    {
                        userMessage,
                        assistantMessage,
                        compressionUserMessage
                    };

                    // Generate compressed response
                    await subAgent.GenerateAsync(
                        compressionHistory,
                        compressionAssistantMessage,
                        subAgentTools,
                        createSubToolMessageCallback,
                        cancellationToken);

                    // Use the compressed response as the final result
                    finalResponse = compressionAssistantMessage.Body;
                }

                // Set the final result for the parent LLM
                toolResultMessage.Body = finalResponse;
                toolResultMessage.Status = "complete";
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                // Handle errors gracefully
                var errorMessage = toolResultMessage.AddMessage("assistant", $"❌ Error in {_functionName}: {ex.Message}", _functionName);
                errorMessage.Status = "error";
                await _dbContext.SaveChangesAsync(cancellationToken);

                toolResultMessage.Body = JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    status = "failed",
                    agent = _functionName
                });
                toolResultMessage.Status = "error";
            }
        }

        private IEnumerable<IToolInterface> GetSubAgentTools()
        {
            // Sub-agents can have their own tools - you can customize this
            return new IToolInterface[]
            {
                _serviceProvider.GetRequiredService<DiceRollTool>(),
                // Add more tools that sub-agents can use
                // Could even include other AssistantTools for deeper nesting
            };
        }

        public Tool GetToolDefinition() => new Tool
        {
            Function = new Function
            {
                Name = _functionName,
                Description = _toolDescription,
                Parameters = new Parameters
                {
                    Type = "object",
                    Properties = new Dictionary<string, Property>
                    {
                        ["prompt"] = new Property
                        {
                            Type = "string",
                            Description = "Prompt"
                        }
                    },
                    Required = new[] { "prompt" }
                }
            },
            Type = "function"
        };
    }
}