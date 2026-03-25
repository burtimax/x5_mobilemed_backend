using Microsoft.Extensions.Options;
using Shared.Configs;

namespace Application.Services.WeekRation;

/// <summary>
/// Общий семафор на число параллельных генераций рациона (настраивается через <see cref="WeekRationGenerationJobOptions"/>).
/// </summary>
public sealed class WeekRationGenerationConcurrencyGate : IDisposable
{
    private const int MaxAllowedParallelism = 64;

    public SemaphoreSlim Semaphore { get; }

    public WeekRationGenerationConcurrencyGate(IOptions<WeekRationGenerationJobOptions> options)
    {
        var n = Math.Clamp(options.Value.MaxParallelGenerations, 1, MaxAllowedParallelism);
        Semaphore = new SemaphoreSlim(n, n);
    }

    public void Dispose() => Semaphore.Dispose();
}
