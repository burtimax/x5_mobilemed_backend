using Shared.Models;

namespace Application.Models.UserExcludeProducts;

/// <summary>
/// Запрос на получение списка продуктов-исключений из справочника.
/// </summary>
public class GetExcludeProductsRequest : Pagination
{
    /// <summary>
    /// Поиск по названию продукта.
    /// </summary>
    public string? Search { get; set; }
}
