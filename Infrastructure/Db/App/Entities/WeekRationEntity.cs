using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Сохранённый недельный рацион пользователя, привязанный к скану RPPG.
/// </summary>
public class WeekRationEntity : BaseEntity
{
    [Comment("Идентификатор пользователя")]
    public Guid UserId { get; set; }

    [JsonIgnore]
    public UserEntity? User { get; set; }

    [Comment("Идентификатор скана RPPG")]
    public Guid RppgScanId { get; set; }

    [JsonIgnore]
    public UserRppgScanEntity? RppgScan { get; set; }

    [JsonPropertyOrder(int.MaxValue)]
    public List<WeekRationItemEntity> Items { get; set; } = [];

    /// <summary>
    /// Ккал.
    /// </summary>
    public decimal? Kcal => Items.Sum(i => i.Kcal);

    /// <summary>
    /// Белки (г).
    /// </summary>
    public decimal? Proteins => Items.Sum(i => i.Proteins);

    /// <summary>
    /// Жиры (г).
    /// </summary>
    public decimal? Fats => Items.Sum(i => i.Fats);

    /// <summary>
    /// Углеводы (г).
    /// </summary>
    public decimal? Carbs => Items.Sum(i => i.Carbs);
}
