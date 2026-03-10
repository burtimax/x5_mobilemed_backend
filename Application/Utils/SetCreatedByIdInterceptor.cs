using Application.Services.User;
using Infrastructure.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Application.Utils;

public class SetByUserIdInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserAccessor _currentUser;
    public SetByUserIdInterceptor(ICurrentUserAccessor currentUser)
    {
        _currentUser = currentUser;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        SetUserProps(eventData);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetUserProps(eventData);
        return base.SavingChanges(eventData, result);
    }

    void SetUserProps(DbContextEventData eventData)
    {
        var userId = _currentUser.GetCurrentUserId();

        foreach (var entry in eventData.Context!.ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added && entry.Entity.CreatedById == null)
            {
                entry.Entity.Id = Guid.CreateVersion7();
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }
        }

        foreach (var entry in eventData.Context!.ChangeTracker.Entries<IBaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedById = userId;
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
            }
            if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedById = userId;
                entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
            }
            if (entry.State == EntityState.Deleted && entry.Entity.DeletedById == null)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedById = userId;
                entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
            }
        }
    }

}
