namespace Application.Services.Llm;

/// <summary>
/// Интерфейс сервиса для работы с LLM
/// </summary>
public interface ILlmService
{
    /// <summary>
    /// Обрабатывает транскрипцию консультации через LLM и возвращает структурированный результат
    /// </summary>
    /// <param name="transcription">Текст транскрипции консультации</param>
    /// <param name="cancellationToken">Токен отмены операции</param>
    /// <returns>Результат обработки с ConsultationResultModel</returns>
    Task<string> ProcessConsultationTranscriptionAsync(
        string transcription,
        string prompt,
        bool isJsonResponse = true,
        CancellationToken cancellationToken = default);
}
