using Infrastructure.Db.App.Entities;
using Infrastructure.Models;

namespace Application.Services.UserExcludeProducts;

/// <summary>
/// Сервис для работы с продуктами-исключениями пользователя.
/// </summary>
public interface IUserExcludeProductsService
{
    /// <summary>
    /// Получает список продуктов из справочника с поиском по названию.
    /// </summary>
    /// <param name="search">Строка поиска по названию продукта (опционально)</param>
    /// <param name="pageNumber">Номер страницы</param>
    /// <param name="pageSize">Размер страницы</param>
    /// <param name="cancellation">Токен отмены</param>
    /// <returns>Постраничный список продуктов</returns>
    Task<PagedList<ExcludeProductEntity>> GetExcludeProductsAsync(
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellation);

    /// <summary>
    /// Получает список всех продуктов-исключений пользователя.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="cancellation">Токен отмены</param>
    /// <returns>Список названий продуктов-исключений</returns>
    Task<IReadOnlyList<string>> GetUserExcludeProductsAsync(
        Guid userId,
        CancellationToken cancellation);

    /// <summary>
    /// Сохраняет список продуктов-исключений пользователя (полная перезапись).
    /// Пустой список после нормализации удаляет все исключения.
    /// </summary>
    /// <param name="userId">Идентификатор пользователя</param>
    /// <param name="products">Полный список названий продуктов-исключений</param>
    /// <param name="cancellation">Токен отмены</param>
    /// <returns>Количество сохранённых продуктов-исключений</returns>
    Task<int> SaveUserExcludeProductsAsync(
        Guid userId,
        IReadOnlyList<string> products,
        CancellationToken cancellation);
}
