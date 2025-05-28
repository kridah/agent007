namespace src;

public class OllamaSettings
{
    public string BaseUrl { get; set; } = "http://localhost:11434";
    public List<OllamaAgentSettings> Agents { get; set; }

}