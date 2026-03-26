using Infrastructure.Db.App.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

namespace Infrastructure.Db.App;

public sealed class LlmUsageJournalService : ILlmUsageJournal
{
    private readonly IServiceScopeFactory _scopeFactory;

    public LlmUsageJournalService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task AppendAsync(LlmUsageJournalEntry entry, CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.LlmUsages.Add(new LlmUsageEntity
        {
            InputJson = entry.InputJson,
            DurationMs = entry.DurationMs,
            IsSuccess = entry.IsSuccess,
            LlmResponse = entry.LlmResponse,
            ErrorMessage = entry.ErrorMessage,
            PromptTokens = entry.PromptTokens,
            CompletionTokens = entry.CompletionTokens,
            LlmModel = entry.LlmModel,
            Cost = entry.Cost,
            LlmRequestId = entry.LlmRequestId
        });
        await db.SaveChangesAsync(cancellationToken);
    }
}
