using OllamaSharp.Models.Chat;
using Agent007.LLM;
using Agent007.Data;
using System.Text.Json;

namespace Agent007.Tools
{
    public class DiceRollTool : IToolInterface
    {
        private readonly ChatDbContext _dbContext;
        private readonly ILogger<DiceRollTool> _logger;

        public DiceRollTool(ChatDbContext dbContext, ILogger<DiceRollTool> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task ExecuteAsync(
            ParamParser parameters,
            Models.Chat.Message toolResultMessage,
            CancellationToken cancellationToken = default)
        {
            try
            {

                var sidesItem = parameters.Get("sides");

                // Parse the sides parameter with fallback to 6
                var sides = parameters.Get("sides")?.AsInt() ?? 6;
                _logger.LogError("Final sides value: {Sides}", sides);

                // Perform the dice roll
                var roll = Random.Shared.Next(1, sides + 1);

                // Add user message (tool request)
                var userMessage = toolResultMessage.AddMessage("user", $"Please roll the {sides}-sided dice for me");

                // Save user message to database
                //_dbContext.Messages.Add(userMessage);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Add agent message (tool response)
                var agentMessage = toolResultMessage.AddMessage("assistant", $"🎲 Rolled the {sides}-sided dice and got {roll}");

                // Save agent message to database
                //_dbContext.Messages.Add(agentMessage);
                await _dbContext.SaveChangesAsync(cancellationToken);

                // Set the final result for the LLM (JSON format)
                toolResultMessage.Body = JsonSerializer.Serialize(new
                {
                    roll = roll,
                    sides = sides
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DiceRollTool Error: {Message}", ex.Message);

                // Handle errors gracefully
                var errorMessage = toolResultMessage.AddMessage("agent", "DiceRoller");
                errorMessage.Body = $"❌ Error rolling dice: {ex.Message}";
                errorMessage.Status = "error";

                //_dbContext.Messages.Add(errorMessage);
                await _dbContext.SaveChangesAsync(cancellationToken);

                toolResultMessage.Body = JsonSerializer.Serialize(new
                {
                    error = ex.Message,
                    status = "failed"
                });
                toolResultMessage.Status = "error";
            }
        }

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
                            Description = "Number of sides on the dice (default: 6)"
                        }
                    },
                    Required = new string[0] // sides is optional
                }
            },
            Type = "function"
        };
    }
}