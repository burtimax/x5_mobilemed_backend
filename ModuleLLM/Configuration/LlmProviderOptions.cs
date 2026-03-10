namespace ModuleLLM.Configuration;

/// <summary>
/// Опции выбора провайдера LLM
/// </summary>
public class LlmProviderOptions
{
    public const string Section = "Llm";

    /// <summary>
    /// Провайдер LLM: groq, router-ai или gigachat
    /// </summary>
    public string Provider { get; set; } = "groq";
}
