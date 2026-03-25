using Application.Models.WeekRation;

namespace Application.Services.WeekRation;

/// <summary>Результат генерации рациона: тело ответа API и HTTP-код.</summary>
public sealed record WeekRationGeneratorOutcome(int StatusCode, WeekRationResponseDto Response);
