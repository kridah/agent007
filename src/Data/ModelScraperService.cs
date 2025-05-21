using System.Text.Json;
using System.Text.Json.Serialization;

namespace OllamaModelScraper.Services
{
    public class ModelScraperService
    {
        private readonly HttpClient _httpClient;

        public ModelScraperService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<OllamaApiModel>> FetchModelsFromApiAsync()
        {
            var url = "https://ollama-models.zwz.workers.dev";
            var models = new List<OllamaApiModel>();
            try
            {
                var response = await _httpClient.GetStringAsync(url);
                models = JsonSerializer.Deserialize<List<OllamaApiModel>>(response) ?? new List<OllamaApiModel>();
            }
            catch (Exception ex)
            {
                // Handle/log error as needed
                Console.WriteLine($"Error fetching models: {ex.Message}");
            }
            return models;
        }
    }

    public class OllamaApiModel
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("description")]
        public string Description { get; set; } = "";

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();
    }
}