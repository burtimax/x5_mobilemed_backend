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

    public List<WeekRationItemEntity> Items { get; set; } = [];
}
