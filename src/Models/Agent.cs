namespace Agent007.Models
{
    public class Agent
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SystemMessage { get; set; } = string.Empty;
        public string? ModelName { get; set; } = null;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
