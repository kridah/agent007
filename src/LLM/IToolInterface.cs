using Agent007.Models.Chat;
using Agent007.Tools;

namespace Agent007.LLM;
public interface IToolInterface
{
    OllamaSharp.Models.Chat.Tool GetToolDefinition();

    /**
     * The tool is expected to perform its operation and continuously keep the toolResultMessage updated
     * with its progress. When the ExecuteAsync method completes, the toolResultMessage should be in a final state
     * and contain the result of the tool's operation in the Body property.
     * 
     * The toolResultMessage is a Message object that is already added to the conversation, and it's being monitored
     * and dynamically updated by the UI.
     * 
     * The toolResultMessage.Body should be empty because this will display a spinner in the UI, indicating that the
     * tool is still executing. Progress updates should be added as child messages to the toolResultMessage, and only
     * when the final result is ready, the Body property should be set.
     * 
     * Tools that are expected to take a while, should provide feedback to the user by adding child messages to 
     * the toolResultMessage.
     */
    Task ExecuteAsync(
        ParamParser parameters,
        Message toolResultMessage,
        CancellationToken cancellationToken = default);
}