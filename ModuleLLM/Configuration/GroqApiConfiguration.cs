namespace ModuleLLM.Configuration;

public class GroqApiConfiguration
{
    public const string Section = "Groq";
    public string ApiKey { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = "https://api.groq.com/openai/v1";
    public int Timeout { get; set; } = 120;
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
}

