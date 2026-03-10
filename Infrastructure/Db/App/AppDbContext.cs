using Infrastructure.Db.App.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App;

public partial class AppDbContext : DbContext
{
    private const string identitySchema= "identity";
    private const string appSchema = "app";
    private const string tenantSchema = "tenant";
    private const string pipelineSchema = "pipeline";
    private const string statSchema = "stat";

    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();
    public DbSet<StatEventEntity> StatEvents => Set<StatEventEntity>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        SetSchemasToTables(builder);
        SetAllToSnakeCase(builder);
        AppDbContext.ConfigureEntities(builder);
        AppDbContext.SetFilters(builder);
    }

    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var e in
                 ChangeTracker.Entries<IBaseEntity>())
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Modified:
                    e.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    break;
                case EntityState.Deleted:
                    e.Entity.DeletedAt = DateTimeOffset.UtcNow;
                    e.State = EntityState.Modified;
                    break;
            }
        }

        foreach (var e in
                 ChangeTracker.Entries<BaseEntity>())
        {
            switch (e.State)
            {
                case EntityState.Added:
                    e.Entity.Id = Guid.CreateVersion7(DateTimeOffset.Now);
                    break;
            }
        }

        return base.SaveChangesAsync(ct);
    }

}
