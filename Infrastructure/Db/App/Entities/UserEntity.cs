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
    /// Пользователь подтвердил ознакомление с политикой и документами.
    /// </summary>
    [Comment("Пользователь подтвердил ознакомление с политикой и документами.")]
    public bool? ConfirmedPolicyAndDocuments { get; set; }

    /// <summary>
    /// Профиль пользователя
    /// </summary>
    public UserProfileEntity? Profile { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public Guid? UpdatedById { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }
}
