using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App.Entities;

/// <summary>
/// Категория товаров X5.
/// </summary>
public class CategoryEntity
{
    /// <summary>
    /// Идентификатор категории.
    /// </summary>
    [Comment("Идентификатор категории")]
    public int Id { get; set; }

    /// <summary>
    /// Идентификатор родительской категории.
    /// </summary>
    [Comment("Идентификатор родительской категории")]
    public int? ParentId { get; set; }

    /// <summary>
    /// Родительская категория.
    /// </summary>
    [JsonIgnore]
    public CategoryEntity? Parent { get; set; }

    /// <summary>
    /// Дочерние категории.
    /// </summary>
    [JsonIgnore]
    public ICollection<CategoryEntity> Children { get; set; } = [];

    /// <summary>
    /// Название категории.
    /// </summary>
    [Comment("Название категории")]
    public required string Title { get; set; }

    /// <summary>
    /// URL изображения категории.
    /// </summary>
    [Comment("URL изображения категории")]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Товары в данной категории.
    /// </summary>
    [JsonIgnore]
    public ICollection<ProductEntity> Products { get; set; } = [];
}
