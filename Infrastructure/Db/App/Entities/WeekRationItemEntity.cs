using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Application.Utils;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Позиция рациона: день, тип приёма пищи и основной товар.
/// </summary>
public class WeekRationItemEntity : BaseEntity
{
    [Comment("Идентификатор рациона")]
    public Guid WeekRationId { get; set; }

    [JsonIgnore]
    public WeekRationEntity? WeekRation { get; set; }

    /// <summary>Например: breakfast, lunch, snack, dinner.</summary>
    [Comment("Тип приёма пищи")]
    [MaxLength(20)]
    public required string Type { get; set; }

    public int Order { get; set; }

    [Comment("День недели (1–7)")]
    public int Day { get; set; }

    [Comment("Идентификатор товара из каталога X5")]
    public long ProductId { get; set; }

    public ProductEntity? Product { get; set; }

    /// <summary>Вес порции в граммах (орфография как в контракте LLM: weigth).</summary>
    [Comment("Вес порции, г")]
    public int Weigth { get; set; }

    [Comment("Краткая причина включения товара")]
    [MaxLength(2000)]
    public string? Reason { get; set; }

    public List<WeekRationItemReplaceEntity> Replaces { get; set; } = [];

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

}
