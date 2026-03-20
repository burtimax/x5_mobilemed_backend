using Infrastructure.Db.App;
using Infrastructure.Db.App.Entities;
using Infrastructure.Models;
using Microsoft.EntityFrameworkCore;
using Shared.Extensions;

namespace Application.Services.UserExcludeProducts;

/// <summary>
/// Сервис для работы с продуктами-исключениями пользователя.
/// </summary>
public class UserExcludeProductsService : IUserExcludeProductsService
{
    private readonly AppDbContext _db;

    public UserExcludeProductsService(AppDbContext db)
    {
        _db = db;
    }

    /// <inheritdoc />
    public async Task<PagedList<ExcludeProductEntity>> GetExcludeProductsAsync(
        string? search,
        int pageNumber,
        int pageSize,
        CancellationToken cancellation)
    {
        var query = _db.ExcludeProducts.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchPattern = search.ToILikePattern();
            query = query.Where(p => EF.Functions.ILike(p.ProductName, searchPattern));
        }

        query = query.OrderBy(p => p.ProductName);

        return await PagedList<ExcludeProductEntity>.ToPagedListAsync(query, pageNumber, pageSize);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetUserExcludeProductsAsync(
        Guid userId,
        CancellationToken cancellation)
    {
        return await _db.UserExcludeProducts
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.ExcludeProduct)
            .Select(e => e.ExcludeProduct)
            .ToListAsync(cancellation);
    }

    /// <inheritdoc />
    public async Task<int> SaveUserExcludeProductsAsync(
        Guid userId,
        IReadOnlyList<string> products,
        CancellationToken cancellation)
    {
        var normalizedProducts = products
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .Distinct()
            .ToList();

        await _db.UserExcludeProducts
            .Where(e => e.UserId == userId)
            .ExecuteDeleteAsync(cancellation);

        if (normalizedProducts.Count == 0)
            return 0;

        foreach (var productName in normalizedProducts)
        {
            _db.UserExcludeProducts.Add(new UserExcludeProductEntity
            {
                UserId = userId,
                ExcludeProduct = productName
            });
        }

        await _db.SaveChangesAsync(cancellation);

        return normalizedProducts.Count;
    }
}
