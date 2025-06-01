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

        // In Agent.cs
        public async Task GenerateAsync(
            List<Message> messageHistory,
            Message assistantMessage,
            IEnumerable<IToolInterface>? tools = null,
            Func<string, Task<Message>>? createToolMessageCallback = null,
            CancellationToken cancellationToken = default) 
            =>
            await _backend.GenerateAsync(messageHistory, assistantMessage, tools, createToolMessageCallback, cancellationToken);
    }
}
