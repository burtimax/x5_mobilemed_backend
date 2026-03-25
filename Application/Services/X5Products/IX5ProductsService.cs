using Infrastructure.Db.App.Entities;

namespace Application.Services.X5Products;

/// <summary>
/// Каталог товаров X5 для текстового представления (например, для LLM).
/// </summary>
public interface IX5ProductsService
{
    /// <summary>
    /// Текстовый каталог всех товаров X5 с приоритетом сортировки &lt; 100.
    /// Формат совпадает с экспортом <c>Scripts.CategoriesAndProductsToText</c>.
    /// </summary>
    Task<string> GetProductsCatalogTextAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Загрузка карточек товаров по идентификаторам (с категорией, без лишней глубины навигации).
    /// </summary>
    Task<IReadOnlyDictionary<long, ProductEntity>> GetProductsByIdsAsync(
        IReadOnlyCollection<long> productIds,
        CancellationToken cancellationToken = default);
}
