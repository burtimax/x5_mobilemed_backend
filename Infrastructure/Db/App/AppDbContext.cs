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
    public DbSet<UserRppgScanEntity> UserRppgScans => Set<UserRppgScanEntity>();
    public DbSet<UserRppgScanResultItemEntity> UserRppgScanResultItems => Set<UserRppgScanResultItemEntity>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        SetSchemasToTables(builder);
        SetAllToSnakeCase(builder);
        AppDbContext.ConfigureEntities(builder);
        AppDbContext.SetFilters(builder);
    }
}
