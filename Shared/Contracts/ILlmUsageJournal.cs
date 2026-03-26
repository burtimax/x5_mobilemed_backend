namespace Shared.Contracts;

/// <summary>Журнал вызовов LLM (каждая HTTP-попытка).</summary>
public interface ILlmUsageJournal
{
    Task AppendAsync(LlmUsageJournalEntry entry, CancellationToken cancellationToken = default);
}

/// <param name="InputJson">Тело запроса к провайдеру (JSON).</param>
/// <param name="DurationMs">Длительность запроса, мс.</param>
/// <param name="LlmRequestId">Идентификатор ответа провайдера (например OpenRouter <c>id</c>).</param>
public sealed record LlmUsageJournalEntry(
    string InputJson,
    long DurationMs,
    bool IsSuccess,
    string? LlmResponse,
    string? ErrorMessage,
    int? PromptTokens,
    int? CompletionTokens,
    string? LlmModel,
    decimal? Cost,
    string? LlmRequestId);
