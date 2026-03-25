using System.Text.Json.Serialization;
using Infrastructure.Db.App.Entities;

namespace Application.Models.WeekRation;

/// <summary>Вариант замены (в JSON только <c>id</c> и <c>weigth</c>).</summary>
public sealed class WeekRationProductReplaceCandidateDto
{
    public long Id { get; set; }

    /// <summary>Рекомендуемая порция замены, г (ключ <c>weigth</c>).</summary>
    [JsonPropertyName("weigth")]
    public int PortionGrams { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProductEntity? Product { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }
}

/// <summary>Позиция в списке <see cref="WeekRationMealSlotDto.Food"/>.</summary>
public sealed class WeekRationProductRefDto
{
    public long Id { get; set; }

    /// <summary>Краткая причина выбора (в схеме LLM необязательна).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    [JsonPropertyName("weigth")]
    public int PortionGrams { get; set; }

    public List<WeekRationProductReplaceCandidateDto> Replace { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProductEntity? Product { get; set; }
}

/// <summary>Один приём пищи: день недели, тип и список товаров.</summary>
public sealed class WeekRationMealSlotDto
{
    /// <summary>День 1–7.</summary>
    public int Day { get; set; }

    /// <summary>Одно из: breakfast, lunch, snack, dinner.</summary>
    public string Type { get; set; } = string.Empty;

    public List<WeekRationProductRefDto> Food { get; set; } = [];
}
