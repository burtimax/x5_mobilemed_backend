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

    /// <summary>То же число, что и начальная ёмкость <see cref="Semaphore"/> (лимит параллельных генераций).</summary>
    public int MaxParallelism { get; }

    public WeekRationGenerationConcurrencyGate(IOptions<WeekRationGenerationJobOptions> options)
    {
        var n = Math.Clamp(options.Value.MaxParallelGenerations, 1, MaxAllowedParallelism);
        MaxParallelism = n;
        Semaphore = new SemaphoreSlim(n, n);
    }

    public void Dispose() => Semaphore.Dispose();
}
