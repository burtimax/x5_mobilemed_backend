namespace Application.Models.UserExcludeProducts;

/// <summary>
/// Запрос на получение списка продуктов-исключений из справочника.
/// </summary>
public class GetExcludeProductsRequest
{
    /// <summary>
    /// Поиск по названию продукта.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Номер страницы.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Количество элементов на странице.
    /// </summary>
    public int PageSize { get; set; } = 50;
}
