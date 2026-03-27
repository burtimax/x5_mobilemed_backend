namespace Application.Services.RppgScan;

/// <summary>
/// Тексты для промптов LLM при генерации рациона: фокусные показатели и остаток отчёта скана.
/// </summary>
public sealed record RppgScanRationContext(
    string FocusMetricsReportText,
    string SupplementaryReportText);
