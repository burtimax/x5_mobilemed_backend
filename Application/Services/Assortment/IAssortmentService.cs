using Infrastructure.Db.App.Entities;
using Infrastructure.Models;

namespace Application.Services.Assortment;

/// <summary>
/// Сервис для работы с ассортиментом (категории и товары).
/// </summary>
public interface IAssortmentService
{
    /// <summary>
    /// Получает все категории, для которых есть товары.
    /// </summary>
    /// <param name="cancellation">Токен отмены</param>
    /// <returns>Список категорий</returns>
    Task<List<CategoryEntity>> GetCategoriesWithProductsAsync(CancellationToken cancellation);

    /// <summary>
    /// Получает товары с фильтрацией по категориям.
    /// </summary>
    /// <param name="categoryIds">Идентификаторы категорий для фильтрации (null = все товары)</param>
    /// <param name="pageNumber">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellation">Токен отмены</param>
    /// <returns>Постраничный список товаров</returns>
    Task<PagedList<ProductEntity>> GetProductsAsync(
        IReadOnlyList<int>? categoryIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellation);

    /// <summary>
    /// Устанавливает активность товара.
    /// </summary>
    /// <param name="productId">Идентификатор товара</param>
    /// <param name="isActive">Признак активности</param>
    /// <param name="cancellation">Токен отмены</param>
    /// <returns>True, если товар найден и обновлён</returns>
    Task<bool> SetProductActiveAsync(long productId, bool isActive, CancellationToken cancellation);
}
