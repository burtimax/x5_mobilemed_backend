using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace ModuleLLM.Services;

internal static class LlmUsageJournalSupport
{
    public static decimal? ToDecimalCost(double? value) =>
        value is null ? null : (decimal)value.Value;

    public static async Task TryAppendAsync(
        ILlmUsageJournal journal,
        ILogger logger,
        LlmUsageJournalEntry entry,
        CancellationToken cancellationToken)
    {
        try
        {
            await journal.AppendAsync(entry, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Не удалось записать использование LLM в stat.llm_usages");
        }
    }
}
