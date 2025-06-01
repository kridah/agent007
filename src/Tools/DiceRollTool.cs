using OllamaSharp.Models.Chat;
namespace Agent007.Tools;
public class DiceRollTool : IToolInterface
{
    private int _lastRoll;
    private int _lastSides;

    public Tool GetToolDefinition() => new Tool
    {
        Function = new Function
        {
            Name = "roll_dice",
            Description = "Roll a dice with specified number of sides",
            Parameters = new Parameters
            {
                Type = "object",
                Properties = new Dictionary<string, Property>
                {
                    ["sides"] = new Property
                    {
                        Type = "integer",
                        Description = "Number of sides on the dice",
                        Default = 6
                    }
                },
                Required = new string[0]
            }
        },
        Type = "function"
    };

    public async Task ExecuteAsync(
        IDictionary<string, object?> parameters,
        Agent007.Models.Chat.Message toolMessage,
        CancellationToken cancellationToken = default)
    {
        // Parse parameters
        _lastSides = parameters.ContainsKey("sides") && parameters["sides"] != null
            ? Convert.ToInt32(parameters["sides"])
            : 6;

        // Perform the dice roll
        _lastRoll = Random.Shared.Next(1, _lastSides + 1);

        // Create the tool conversation thread

        // 1. User message (dice tool is acting as user requesting the roll)
        var userMessage = new Agent007.Models.Chat.Message
        {
            ConversationId = toolMessage.ConversationId,
            ParentId = toolMessage.Id,
            Role = "user",
            AgentName = "DiceTool",
            Body = $"Requested to roll a {_lastSides}-sided dice",
            Status = "complete",
            CreatedAt = DateTime.UtcNow
        };

        // 2. Agent message (dice tool responding with result)
        var agentMessage = new Agent007.Models.Chat.Message
        {
            ConversationId = toolMessage.ConversationId,
            ParentId = toolMessage.Id,
            Role = "agent",
            AgentName = "DiceTool",
            Body = $"The dice rolled {_lastRoll}",
            Status = "complete",
            CreatedAt = DateTime.UtcNow.AddMilliseconds(1) // Ensure ordering
        };

        // Add the conversation thread to the tool message
        toolMessage.Children.Add(userMessage);
        toolMessage.Children.Add(agentMessage);
    }

    public string GetLLMResult()
    {
        return _lastRoll.ToString();
    }
}