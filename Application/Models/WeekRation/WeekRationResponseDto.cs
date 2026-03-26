namespace Application.Models.WeekRation;

/// <summary>Тело ответа API после маппинга ответа LLM (массив дней) в типизированный рацион.</summary>
public sealed class WeekRationResponseDto
{
    public Guid? Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Guid RppgScanId { get; set; }

    /// <summary>
    /// Ккал.
    /// </summary>
    public decimal? Kcal => Ration?.Sum(r => r.Kcal);

    /// <summary>
    /// Белки (г).
    /// </summary>
    public decimal? Proteins => Ration?.Sum(r => r.Proteins);

    /// <summary>
    /// Жиры (г).
    /// </summary>
    public decimal? Fats => Ration?.Sum(r => r.Fats);

    /// <summary>
    /// Углеводы (г).
    /// </summary>
    public decimal? Carbs => Ration?.Sum(r => r.Carbs);

    public IReadOnlyList<DayRationMealSlotDto>? Ration { get; set; }

    public string? Error { get; set; }

    public string? RawAssistantContent { get; set; }
}
