using OllamaSharp;
using Agent007.Data;

namespace Agent007.LLM
{
    public class LLMBackendFactory
    {
        private readonly IServiceProvider _services;

        public LLMBackendFactory(IServiceProvider services)
        {
            _services = services;
        }

        public ILLMBackend CreateBackend(string backendSpec, string systemPrompt)
        {
            var client = _services.GetRequiredService<OllamaApiClient>();
            var dbContext = _services.GetRequiredService<ChatDbContext>();
            var logger = _services.GetRequiredService<ILogger<OllamaBackend>>();
            return new OllamaBackend(client, backendSpec, systemPrompt, dbContext, logger);
        }
    }
}