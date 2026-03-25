using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Вариант замены товара в позиции рациона.
/// </summary>
public class WeekRationItemReplaceEntity : BaseEntity
{
    [Comment("Идентификатор позиции рациона")]
    public Guid WeekRationItemId { get; set; }

    [JsonIgnore]
    public WeekRationItemEntity? WeekRationItem { get; set; }

    [Comment("Идентификатор товара-замены из каталога X5")]
    public long ProductId { get; set; }

    [JsonIgnore]
    public ProductEntity? Product { get; set; }

    [Comment("Вес порции замены, г")]
    public int Weight { get; set; }

    [Comment("Пояснение к замене")]
    [MaxLength(2000)]
    public string? Reason { get; set; }
}
