using Microsoft.Extensions.Logging;
using ModuleLLM.Services;
using Shared.Contracts;

namespace Application.Services.Llm;

/// <summary>
/// Сервис для работы с LLM
/// </summary>
public class LlmService : ILlmService
{
    private readonly ILlmApiService _llmApiService;
    private readonly ILogger<LlmService> _logger;

    public LlmService(
        ILlmApiService llmApiService,
        ILogger<LlmService> logger)
    {
        _llmApiService = llmApiService;
        _logger = logger;
    }

    /// <summary>
    /// Обрабатывает транскрипцию консультации через LLM и возвращает структурированный результат
    /// </summary>
    public async Task<string> ProcessConsultationTranscriptionAsync(
        string transcription,
        string prompt,
        bool isJsonResponse = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(transcription))
                throw new Exception("Транскрипция не может быть пустой");

            var result = await _llmApiService.ProcessAsync(
                transcription, prompt, isJsonResponse, cancellationToken);

            _logger.LogInformation("Успешно обработана транскрипция консультации через LLM (OpenRouter)");
            return result;
        }
        catch (Exception ex)
        {
            //_logger.LogError(ex, "Критическая ошибка при обработке транскрипции консультации");
            throw;
        }
    }
}
