namespace Agent007.LLM
{
    using Models.Chat;

    public interface ILLMBackend
    {
        string SystemMessage { get; set; }

        Task GenerateAsync(
            IEnumerable<Message> history,
            Message userMessage,
            Message targetAssistantMessage,
            CancellationToken cancellationToken = default);
    }

}
