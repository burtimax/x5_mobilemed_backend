using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Сущность пользователя в базе данных (ASP.NET Identity)
/// </summary>
public class UserEntity : IBaseEntity
{
    public Guid Id { get; set; }

    /// <summary>
    /// Идентификатор от X5 для пользователя
    /// </summary>
    public string ExternalId { get; set; }

    /// <summary>
    /// Профиль пользователя
    /// </summary>
    [JsonIgnore]
    public UserProfileEntity? Profile { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }
}
