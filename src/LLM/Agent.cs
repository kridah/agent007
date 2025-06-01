namespace Agent007.LLM
{
    using Models.Chat;

    public class Agent
    {
        private readonly ILLMBackend _backend;

        public Agent(ILLMBackend backend)
        {
            _backend = backend;
        }

        public string SystemMessage
        {
            get => _backend.SystemMessage;
            set => _backend.SystemMessage = value;
        }

        public async Task GenerateAsync(
            List<Message> messageHistory,
            Message assistantMessage,
            CancellationToken cancellationToken = default)
        {
            var lastUserMessage = messageHistory.LastOrDefault(m => m.Role == "user")
                ?? throw new InvalidOperationException("No user message found to respond to.");

            await _backend.GenerateAsync(messageHistory, lastUserMessage, assistantMessage, cancellationToken);
        }
    }
}
