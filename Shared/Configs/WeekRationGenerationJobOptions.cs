namespace Shared.Configs;

/// <summary>Параметры фоновой генерации недельного рациона (Quartz).</summary>
public sealed class WeekRationGenerationJobOptions
{
    public const string SectionName = "WeekRationGenerationJob";

    /// <summary>Сколько сканов могут одновременно проходить вызов LLM и сохранение рациона.</summary>
    public int MaxParallelGenerations { get; set; } = 2;
}
