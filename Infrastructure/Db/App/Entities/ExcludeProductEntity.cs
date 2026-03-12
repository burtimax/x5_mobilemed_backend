using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Справочник продуктов, которые могут вызывать аллергию, непереносимость и т.д.
/// Пользователь выбирает продукты из этой таблицы для добавления в свои исключения.
/// </summary>
public class ExcludeProductEntity
{
    /// <summary>
    /// Идентификатор продукта.
    /// </summary>
    [Comment("Идентификатор продукта")]
    public Guid Id { get; set; }

    /// <summary>
    /// Наименование продукта.
    /// </summary>
    [Comment("Наименование продукта")]
    [MaxLength(200)]
    public required string ProductName { get; set; }
}
