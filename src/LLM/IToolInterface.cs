using Agent007.Models.Chat;
using OllamaSharp.Models.Chat;

public interface IToolInterface
{
    Tool GetToolDefinition();

    // Execute and store results internally
    Task ExecuteAsync(
        IDictionary<string, object?> parameters,
        Agent007.Models.Chat.Message toolMessage,
        CancellationToken cancellationToken = default);

    // What the LLM sees
    string GetLLMResult();
}