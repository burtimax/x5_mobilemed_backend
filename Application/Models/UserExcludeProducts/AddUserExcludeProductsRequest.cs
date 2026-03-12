using System.ComponentModel.DataAnnotations;

namespace Application.Models.UserExcludeProducts;

/// <summary>
/// Запрос на добавление продуктов в исключения пользователя.
/// </summary>
public class AddUserExcludeProductsRequest
{
    /// <summary>
    /// Список названий продуктов для добавления в исключения.
    /// </summary>
    [Required]
    public List<string> Products { get; set; } = new();
}
