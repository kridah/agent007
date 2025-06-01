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
        private string _body = string.Empty;
        private string _status = "complete"; // "generating", "complete", "error"
        private string _role = string.Empty;
        private string? _agentName;

        public int Id { get; set; }
        public int ConversationId { get; set; }

        // if ParentId is null, this message is a root message in the conversation
        public int? ParentId { get; set; }

        public string Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
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
            set => SetProperty(ref _status, value);
        }

        public DateTime CreatedAt { get; set; }

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