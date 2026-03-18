using Infrastructure.Db.App.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Db.App;

public partial class AppDbContext : DbContext
{
    private const string appSchema = "app";
    private const string biomarkerSchema = "biomarker";
    private const string statSchema = "stat";
    private const string x5Schema = "x5";

    public AppDbContext() { }

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<UserProfileEntity> UserProfiles => Set<UserProfileEntity>();
    public DbSet<StatEventEntity> StatEvents => Set<StatEventEntity>();
    public DbSet<UserRppgScanEntity> UserRppgScans => Set<UserRppgScanEntity>();
    public DbSet<UserRppgScanResultItemEntity> UserRppgScanResultItems => Set<UserRppgScanResultItemEntity>();
    public DbSet<ExcludeProductEntity> ExcludeProducts => Set<ExcludeProductEntity>();
    public DbSet<UserExcludeProductEntity> UserExcludeProducts => Set<UserExcludeProductEntity>();
    public DbSet<ProductEntity> Products => Set<ProductEntity>();
    public DbSet<CategoryEntity> Categories => Set<CategoryEntity>();
    public DbSet<BiomarkerEntity> Biomarkers => Set<BiomarkerEntity>();
    public DbSet<BiomarkerScaleEntity> BiomarkerScales => Set<BiomarkerScaleEntity>();
    public DbSet<BiomarkerZoneEntity> BiomarkerZones => Set<BiomarkerZoneEntity>();


    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        SetSchemasToTables(builder);
        SetAllToSnakeCase(builder);
        AppDbContext.ConfigureEntities(builder);
        AppDbContext.SetFilters(builder);
    }
}
