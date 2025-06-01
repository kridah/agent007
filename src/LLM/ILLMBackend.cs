namespace Agent007.LLM
{
    using Models.Chat;

    public interface ILLMBackend
    {
        string SystemMessage { get; set; }

        Task GenerateAsync(
            IEnumerable<Message> history,
            Message targetAssistantMessage,
            IEnumerable<IToolInterface>? tools = null,
            CancellationToken cancellationToken = default);
    }
}