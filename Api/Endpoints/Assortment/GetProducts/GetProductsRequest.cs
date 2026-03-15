using Shared.Models;

namespace Api.Endpoints.Assortment.GetProducts;

/// <summary>
/// Запрос на получение товаров с фильтрацией по категориям.
/// </summary>
public class GetProductsRequest : Pagination
{
    /// <summary>
    /// Идентификаторы категорий для фильтрации (через запятую). Пусто — все товары.
    /// </summary>
    public List<int>? CategoryIds { get; set; }
}
