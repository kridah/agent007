using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Agent007.Models.Chat
{
    public class Conversation
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
    }

    public class Message : INotifyPropertyChanged
    {
        // Valid values for validation - following Ollama/qwen3 standard
        public static readonly HashSet<string> ValidStatuses = new() { "generating", "complete", "error" };
        public static readonly HashSet<string> ValidRoles = new() { "user", "assistant", "tool", "system" };

        private string _body = string.Empty;
        private string _status = "complete"; // "generating", "complete", "error"
        private string _role = "user";
        private string? _agentName;

        public int Id { get; set; }
        public int ConversationId { get; set; }

        // if ParentId is null, this message is a root message in the conversation
        public int? ParentId { get; set; }

        public string Role
        {
            get => _role;
            set
            {
                if (!string.IsNullOrEmpty(value) && !ValidRoles.Contains(value))
                {
                    throw new ArgumentException($"Invalid role '{value}'. Valid roles are: {string.Join(", ", ValidRoles)}");
                }
                SetProperty(ref _role, value);
            }
        }

        public string? AgentName
        {
            get => _agentName;
            set => SetProperty(ref _agentName, value);
        }

        public string Body
        {
            get => _body;
            set => SetProperty(ref _body, value);
        }

        public string Status
        {
            get => _status;
            set
            {
                if (!string.IsNullOrEmpty(value) && !ValidStatuses.Contains(value))
                {
                    throw new ArgumentException($"Invalid status '{value}'. Valid statuses are: {string.Join(", ", ValidStatuses)}");
                }
                SetProperty(ref _status, value);
            }
        }

        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Adds a child message to this message. This message must be saved to the database first.
        /// The child message will start with "complete" status.
        /// </summary>
        /// <param name="role">Role of the child message (e.g., "user", "agent", "tool")</param>
        /// <param name="initialBody">Initial body content (optional, defaults to empty)</param>
        /// <param name="agentName">Name of the agent creating this message (optional)</param>
        /// <returns>The newly created child message</returns>
        /// <exception cref="InvalidOperationException">Thrown if this message hasn't been saved to database</exception>
        /// <exception cref="ArgumentException">Thrown if role is invalid</exception>
        public Message AddMessage(string role, string initialBody = "", string? agentName = null)
        {
            if (Id == 0)
                throw new InvalidOperationException("Cannot add child messages to a message that hasn't been saved to the database yet.");

            if (ConversationId == 0)
                throw new InvalidOperationException("Cannot add child messages to a message without a valid ConversationId.");

            // Validate role before creating message
            if (string.IsNullOrEmpty(role) || !ValidRoles.Contains(role))
            {
                throw new ArgumentException($"Invalid role '{role}'. Valid roles are: {string.Join(", ", ValidRoles)}");
            }

            var childMessage = new Message
            {
                ConversationId = ConversationId,
                ParentId = Id,
                Role = role, // This will be validated by the Role setter
                AgentName = agentName,
                Body = initialBody,
                Status = "complete", // Default to complete
                CreatedAt = DateTime.UtcNow
            };

            Children.Add(childMessage);
            OnPropertyChanged(nameof(Children)); // Add this line
            return childMessage;
        }

        // Navigation properties
        public virtual Conversation Conversation { get; set; } = null!;
        public virtual Message? Parent { get; set; }
        public virtual ICollection<Message> Children { get; set; } = new List<Message>();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}