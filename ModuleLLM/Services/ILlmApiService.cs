using ModuleLLM.Models.OpenRouter;
using Shared.Contracts;

namespace ModuleLLM.Services;

/// <summary>
/// LLM через OpenRouter: обобщённый вызов chat completions и сценарий транскрипции консультации.
/// </summary>
public interface ILlmApiService
{
    /// <summary>
    /// Отправляет запрос на обработку транскрибации консультации.
    /// </summary>
    Task<string> ProcessAsync(
        string transcription,
        string prompt,
        bool isJsonResponse = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Вызов OpenRouter Chat Completions (<c>/chat/completions</c>).
    /// </summary>
    Task<Result<OpenRouterChatResponse>> SendChatCompletionAsync(
        OpenRouterChatRequest request,
        CancellationToken cancellationToken = default);
}
