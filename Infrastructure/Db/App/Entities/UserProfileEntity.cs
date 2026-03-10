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
    /// Дата рождения
    /// </summary>
    [Comment("Дата рождения")]
    public DateOnly? BirthDate { get; set; }

    /// <summary>
    /// Пол пользователя
    /// </summary>
    [Comment("Пол пользователя")]
    public int? Gender { get; set; }

    /// <summary>
    /// Дополнительные поля пользователя в формате JSON
    /// </summary>
    [Comment("Доп поля пользователя")]
    public JsonDocument? Additional { get; set; }
}
