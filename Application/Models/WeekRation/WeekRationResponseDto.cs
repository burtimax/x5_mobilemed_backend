namespace Application.Models.WeekRation;

/// <summary>Тело ответа API после маппинга ответа LLM (массив дней) в типизированный рацион.</summary>
public sealed class WeekRationResponseDto
{
    public IReadOnlyList<WeekRationMealSlotDto>? Ration { get; set; }

    public string? Error { get; set; }

    public string? RawAssistantContent { get; set; }
}
