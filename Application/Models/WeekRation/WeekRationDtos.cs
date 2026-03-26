using System.Net;
using System.Text.Json.Serialization;
using Application.Utils;
using Infrastructure.Db.App.Entities;

namespace Application.Models.WeekRation;

/// <summary>Вариант замены (в JSON только <c>id</c> и <c>weigth</c>).</summary>
public sealed class WeekRationProductReplaceCandidateDto
{
    public long Id { get; set; }

    /// <summary>Рекомендуемая порция замены, г (ключ <c>weigth</c>).</summary>
    [JsonPropertyName("weigth")]
    public int Weigth { get; set; }

    /// <summary>
    /// Ккал.
    /// </summary>
    public decimal? Kcal => WeigthUtil.Convert(Weigth, Product?.KcalPer100G);

    /// <summary>
    /// Белки (г).
    /// </summary>
    public decimal? Proteins => WeigthUtil.Convert(Weigth, Product?.ProteinsGPer100G);

    /// <summary>
    /// Жиры (г).
    /// </summary>
    public decimal? Fats => WeigthUtil.Convert(Weigth, Product?.FatsGPer100G);

    /// <summary>
    /// Углеводы (г).
    /// </summary>
    public decimal? Carbs => WeigthUtil.Convert(Weigth, Product?.CarbsGPer100G);

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProductEntity? Product { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }
}

/// <summary>Позиция в списке <see cref="DayRationMealSlotDto.Food"/>.</summary>
public sealed class DayRationProductRefDto
{
    public long Id { get; set; }

    /// <summary>Краткая причина выбора (в схеме LLM необязательна).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Reason { get; set; }

    [JsonPropertyName("weigth")]
    public int Weigth { get; set; }

    /// <summary>
    /// Ккал.
    /// </summary>
    public decimal? Kcal => WeigthUtil.Convert(Weigth, Product?.KcalPer100G);

    /// <summary>
    /// Белки (г).
    /// </summary>
    public decimal? Proteins => WeigthUtil.Convert(Weigth, Product?.ProteinsGPer100G);

    /// <summary>
    /// Жиры (г).
    /// </summary>
    public decimal? Fats => WeigthUtil.Convert(Weigth, Product?.FatsGPer100G);

    /// <summary>
    /// Углеводы (г).
    /// </summary>
    public decimal? Carbs => WeigthUtil.Convert(Weigth, Product?.CarbsGPer100G);

    public List<WeekRationProductReplaceCandidateDto> Replace { get; set; } = [];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ProductEntity? Product { get; set; }
}

/// <summary>Один приём пищи: день недели, тип и список товаров.</summary>
public sealed class DayRationMealSlotDto
{
    /// <summary>День 1–7.</summary>
    public int Day { get; set; }

    /// <summary>Одно из: breakfast, lunch, snack, dinner.</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Ккал.
    /// </summary>
    public decimal? Kcal => Food.Sum(f => f.Kcal);

    /// <summary>
    /// Белки (г).
    /// </summary>
    public decimal? Proteins => Food.Sum(f => f.Proteins);

    /// <summary>
    /// Жиры (г).
    /// </summary>
    public decimal? Fats => Food.Sum(f => f.Fats);

    /// <summary>
    /// Углеводы (г).
    /// </summary>
    public decimal? Carbs => Food.Sum(f => f.Carbs);

    public List<DayRationProductRefDto> Food { get; set; } = [];
}
