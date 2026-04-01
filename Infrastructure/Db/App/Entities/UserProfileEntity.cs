using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Профиль пользователя с дополнительными данными
/// </summary>
public class UserProfileEntity : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя (FK к UserEntity)
    /// </summary>
    [Comment("Идентификатор пользователя")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Навигационное свойство к пользователю
    /// </summary>
    [JsonIgnore]
    public UserEntity User { get; set; } = null!;

    /// <summary>
    /// Возраст (лет)
    /// </summary>
    [Comment("Возраст (лет)")]
    public int? Age { get; set; }

    /// <summary>
    /// Рост в см.
    /// </summary>
    [Comment("Рост в см.")]
    public int? Height { get; set; }

    /// <summary>
    /// Вес в кг.
    /// </summary>
    [Comment("Вес в кг")]
    public int? Weight { get; set; }

    /// <summary>
    /// Пол пользователя
    /// </summary>
    [Comment("Пол пользователя: 0 - муж, 1 - жен.")]
    public Gender? Gender { get; set; }

    /// <summary>
    /// Статус курения
    /// </summary>
    [Comment("Статус курения: 0 - не курит, 1 - курит")]
    public SmokeStatus? SmokeStatus { get; set; }

    /// <summary>
    /// Цели пользователя
    /// </summary>
    [Comment("Цели пользователя")]
    public List<string>? Goals { get; set; } = new List<string>();

    /// <summary>
    /// UTM метка, с которой пришел пользователь.
    /// </summary>
    public string? UtmSource { get; set; }
}

/// <summary>
/// Пол
/// </summary>
public enum Gender
{
    Male = 0,
    Female = 1
}

/// <summary>
/// Статус курения
/// </summary>
public enum SmokeStatus
{
    NotSmoking = 0,
    Smoking = 1,
}
