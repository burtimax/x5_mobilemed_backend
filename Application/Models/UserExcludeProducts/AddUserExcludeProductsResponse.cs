namespace Application.Models.UserExcludeProducts;

/// <summary>
/// Ответ на добавление продуктов в исключения.
/// </summary>
public class AddUserExcludeProductsResponse
{
    /// <summary>
    /// Количество добавленных продуктов.
    /// </summary>
    public int AddedCount { get; set; }
}
