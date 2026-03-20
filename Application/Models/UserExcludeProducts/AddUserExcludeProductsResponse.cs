namespace Application.Models.UserExcludeProducts;

/// <summary>
/// Ответ на сохранение продуктов-исключений.
/// </summary>
public class AddUserExcludeProductsResponse
{
    /// <summary>
    /// Количество сохранённых продуктов-исключений.
    /// </summary>
    public int AddedCount { get; set; }
}
