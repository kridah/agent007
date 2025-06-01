using OllamaSharp;
using OllamaSharp.Models;
using OllamaSharp.Models.Chat;
using System.Runtime.CompilerServices;

namespace Agent007.LLM
{
    using Models.Chat;

    public class OllamaBackend : ILLMBackend
    {
        private readonly OllamaApiClient _client;
        private readonly string _model;
        private bool _isBusy;

        public string SystemMessage { get; set; }

        public OllamaBackend(OllamaApiClient client, string model, string systemPrompt)
        {
            _client = client;
            _model = model;
            SystemMessage = systemPrompt;
        }

        public async Task GenerateAsync(
            IEnumerable<Message> history,
            Message userMessage,
            Message targetAssistantMessage,
            CancellationToken cancellationToken = default)
        {
            if (_isBusy)
                throw new InvalidOperationException("Agent is already generating. Complete or cancel the previous task before starting a new one.");

            _isBusy = true;

            try
            {
                targetAssistantMessage.Role = "assistant";
                targetAssistantMessage.Status = "generating";
                targetAssistantMessage.Body = string.Empty;

                var chatMessages = new List<OllamaSharp.Models.Chat.Message>();

                if (!string.IsNullOrWhiteSpace(SystemMessage))
                {
                    chatMessages.Add(new OllamaSharp.Models.Chat.Message
                    {
                        Role = "system",
                        Content = SystemMessage
                    });
                }

                foreach (var msg in history)
                {
                    chatMessages.Add(new OllamaSharp.Models.Chat.Message
                    {
                        Role = msg.Role,
                        Content = msg.Body
                    });
                }

                chatMessages.Add(new OllamaSharp.Models.Chat.Message
                {
                    Role = userMessage.Role,
                    Content = userMessage.Body
                });

                var request = new ChatRequest
                {
                    Model = _model,
                    Messages = chatMessages,
                    Stream = true
                };

                await foreach (var chunk in _client.ChatAsync(request, cancellationToken))
                {
                    if (chunk?.Message?.Content is { Length: > 0 } content)
                    {
                        targetAssistantMessage.Body += content;
                    }
                }

                targetAssistantMessage.Status = "complete";
            }
            finally
            {
                _isBusy = false;
            }
        }
    }

}
