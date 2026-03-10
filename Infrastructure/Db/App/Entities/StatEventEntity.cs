using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Сущность события статистики использования приложения
/// </summary>
public class StatEventEntity : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя
    /// </summary>
    [Comment("Идентификатор пользователя")]
    public Guid? UserId { get; set; }

    /// <summary>
    /// Связь с пользователем
    /// </summary>
    public UserEntity? User { get; set; }

    /// <summary>
    /// Идентификатор сессии
    /// </summary>
    [Comment("Идентификатор сессии")]
    public long? SessionId { get; set; }

    /// <summary>
    /// Тип события
    /// </summary>
    [Comment("Тип события")]
    [MaxLength(30)]
    public string? Type { get; set; }

    /// <summary>
    /// Данные о событии
    /// </summary>
    [Comment("Данные о событии")]
    [MaxLength(100)]
    public string? Data { get; set; }

    [Comment("Длительность выполняемого события")]
    public double? DurationSeconds { get; set; }
}
