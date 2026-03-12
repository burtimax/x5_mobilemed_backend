using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Продукты-исключения для пользователя. Продукты добавляются текстом, а не по идентификатору.
/// </summary>
public class UserExcludeProductEntity : BaseEntity
{
    /// <summary>
    /// Идентификатор пользователя.
    /// </summary>
    [Comment("Идентификатор пользователя")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Связь с пользователем.
    /// </summary>
    public UserEntity? User { get; set; }

    /// <summary>
    /// Наименование продукта-исключения (текстом).
    /// </summary>
    [Comment("Наименование продукта-исключения")]
    [MaxLength(200)]
    public required string ExcludeProduct { get; set; }
}
