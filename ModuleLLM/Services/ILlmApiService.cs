using ModuleLLM.Models;
using Shared.Contracts;

namespace ModuleLLM.Services;

/// <summary>
/// Интерфейс сервиса для работы с Groq API
/// </summary>
public interface ILlmApiService
{
    /// <summary>
    /// Отправляет запрос на обработку транскрибации консультации
    /// </summary>
    /// <param name="systemPrompt">Системный промпт</param>
    /// <param name="transcription">Транскрибация консультации</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат обработки с ответом от LLM</returns>
    Task<string> ProcessConsultationTranscriptionAsync(
        string transcription,
        string prompt,
        bool isJsonResponse = true,
        CancellationToken cancellationToken = default);
}

