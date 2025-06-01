using OllamaSharp;

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
            return new OllamaBackend(client, backendSpec, systemPrompt);
        }
    }

}
