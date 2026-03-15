using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;

namespace Application.Services.Assortment;

/// <summary>
/// Сервис для работы с ассортиментом (категории и товары).
/// </summary>
public class AssortmentService : IAssortmentService
{
    private readonly AppDbContext _db;

    public AssortmentService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<List<CategoryEntity>> GetCategoriesWithProductsAsync(CancellationToken cancellation)
    {
        return await _db.Categories
            .AsNoTracking()
            .Where(c => c.Products.Any(p => p.IsActive))
            .OrderBy(c => c.Title)
            .ToListAsync(cancellation);
    }

    /// <inheritdoc />
    public async Task<PagedList<ProductEntity>> GetProductsAsync(
        IReadOnlyList<int>? categoryIds,
        int pageNumber,
        int pageSize,
        CancellationToken cancellation)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        if (categoryIds is { Count: > 0 })
        {
            query = query.Where(p => categoryIds.Contains(p.CategoryId));
        }

        query = query.OrderBy(p => p.Priority).ThenBy(p => p.Title);

        return await PagedList<ProductEntity>.ToPagedListAsync(query, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<bool> SetProductActiveAsync(long productId, bool isActive, CancellationToken cancellation)
    {
        var product = await _db.Products.FindAsync([productId], cancellation);
        if (product == null)
            return false;

        product.IsActive = isActive;
        await _db.SaveChangesAsync(cancellation);
        return true;
    }
}
