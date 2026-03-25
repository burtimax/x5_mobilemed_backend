using System.Linq.Expressions;
using Infrastructure.Db.App.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shared.Extensions;

namespace Infrastructure.Db.App;

public partial class AppDbContext
{
    /// <summary>
    /// Таблицы, свойства, ключи, внеш. ключи, индексы переводит в нижний регистр в БД.
    /// </summary>
    protected void SetAllToSnakeCase(ModelBuilder builder)
    {
        foreach (var entityType in builder.Model.GetEntityTypes())
        {
            entityType.SetTableName(entityType.GetTableName().ToSnakeCase());

            foreach (var property in entityType.GetProperties())
            {
                var schema = entityType.GetSchema();
                var tableName = entityType.GetTableName();
                var storeObjectIdentifier = StoreObjectIdentifier.Table(tableName, schema);
                property.SetColumnName(property.GetColumnName(storeObjectIdentifier).ToSnakeCase());
            }

            foreach (var key in entityType.GetKeys())
                key.SetName(key.GetName().ToSnakeCase());

            foreach (var key in entityType.GetForeignKeys())
                key.SetConstraintName(key.GetConstraintName().ToSnakeCase());

            foreach (var index in entityType.GetIndexes())
                index.SetDatabaseName(index.GetDatabaseName().ToSnakeCase());
        }
    }

    /// <summary>
    /// Задать наименование таблиц и схемы для таблиц.
    /// </summary>
    private void SetSchemasToTables(ModelBuilder builder)
    {
        // Identity таблицы
        builder.Entity<UserEntity>().ToTable("users", appSchema);
        builder.Entity<UserProfileEntity>().ToTable("user_profiles", appSchema);

        // Прикладные таблицы
        builder.Entity<StatEventEntity>().ToTable("stat_events", statSchema);
        builder.Entity<UserRppgScanEntity>().ToTable("user_rppg_scans", appSchema);
        builder.Entity<UserRppgScanResultItemEntity>().ToTable("user_rppg_scan_result_items", appSchema);
        builder.Entity<ExcludeProductEntity>().ToTable("exclude_products", appSchema);
        builder.Entity<UserExcludeProductEntity>().ToTable("user_exclude_products", appSchema);
        builder.Entity<WeekRationEntity>().ToTable("week_rations", appSchema);
        builder.Entity<WeekRationItemEntity>().ToTable("week_ration_items", appSchema);
        builder.Entity<WeekRationItemReplaceEntity>().ToTable("week_ration_item_replaces", appSchema);

        // X5 схема
        builder.Entity<ProductEntity>().ToTable("products", x5Schema);
        builder.Entity<CategoryEntity>().ToTable("categories", x5Schema);

        // Биомаркеры и зоны интерпретации
        builder.Entity<BiomarkerEntity>().ToTable("biomarkers", biomarkerSchema);
        builder.Entity<BiomarkerScaleEntity>().ToTable("biomarker_scales", biomarkerSchema);
        builder.Entity<BiomarkerZoneEntity>().ToTable("biomarker_zones", biomarkerSchema);
    }

    /// <summary>
    /// Настройка фильтров запросов.
    /// </summary>
    public static void SetFilters(ModelBuilder modelBuilder)
    {
        // Фильтр для UserEntity (наследуется от IdentityUser, но реализует IBaseEntity)
        modelBuilder.Entity<UserEntity>()
            .HasQueryFilter(e => e.DeletedAt == null);

        // Фильтр для остальных сущностей, наследующих от BaseEntity
        var entities = modelBuilder.Model
            .GetEntityTypes()
            .Where(e => e.ClrType.BaseType == typeof(BaseEntity))
            .Select(e => e.ClrType);

        Expression<Func<BaseEntity, bool>>
            expression = del => del.DeletedAt == null;

        foreach (var e in entities)
        {
            ParameterExpression p = Expression.Parameter(e);
            Expression body =
                ReplacingExpressionVisitor
                    .Replace(expression.Parameters.Single(),
                        p, expression.Body);

            modelBuilder.Entity(e)
                .HasQueryFilter(
                    Expression.Lambda(body, p));
        }
    }

    public static void ConfigureEntities(ModelBuilder builder)
    {
        // Настройка связи UserEntity -> UserProfileEntity (1:1)
        builder.Entity<UserEntity>()
            .HasOne(u => u.Profile)
            .WithOne(p => p.User)
            .HasForeignKey<UserProfileEntity>(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Настройка связи StatEventEntity -> UserEntity
        builder.Entity<StatEventEntity>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Настройка связи UserRppgScanEntity -> UserEntity
        builder.Entity<UserRppgScanEntity>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Настройка связи UserRppgScanResultItemEntity -> UserRppgScanEntity
        builder.Entity<UserRppgScanResultItemEntity>()
            .HasOne(i => i.Scan)
            .WithMany(s => s.ResultItems)
            .HasForeignKey(i => i.ScanId)
            .OnDelete(DeleteBehavior.Cascade);

        // Настройка связи UserExcludeProductEntity -> UserEntity
        builder.Entity<UserExcludeProductEntity>()
            .HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Уникальность пары пользователь + продукт
        builder.Entity<UserExcludeProductEntity>()
            .HasIndex(e => new { e.UserId, e.ExcludeProduct })
            .IsUnique();

        builder.Entity<WeekRationEntity>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WeekRationEntity>()
            .HasOne(w => w.RppgScan)
            .WithMany()
            .HasForeignKey(w => w.RppgScanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WeekRationItemEntity>()
            .HasOne(i => i.WeekRation)
            .WithMany(w => w.Items)
            .HasForeignKey(i => i.WeekRationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WeekRationItemEntity>()
            .HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WeekRationItemReplaceEntity>()
            .HasOne(r => r.WeekRationItem)
            .WithMany(i => i.Replaces)
            .HasForeignKey(r => r.WeekRationItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WeekRationItemReplaceEntity>()
            .HasOne(r => r.Product)
            .WithMany()
            .HasForeignKey(r => r.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь ProductEntity -> CategoryEntity
        builder.Entity<ProductEntity>()
            .HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        // Связь CategoryEntity -> CategoryEntity (самоссылка)
        builder.Entity<CategoryEntity>()
            .HasOne(c => c.Parent)
            .WithMany(c => c.Children)
            .HasForeignKey(c => c.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Уникальность PLU
        builder.Entity<ProductEntity>()
            .HasIndex(p => p.Plu)
            .IsUnique();

        // Biomarker -> BiomarkerScale -> BiomarkerZone
        builder.Entity<BiomarkerScaleEntity>()
            .HasOne(s => s.Biomarker)
            .WithMany(b => b.Scales)
            .HasForeignKey(s => s.BiomarkerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BiomarkerZoneEntity>()
            .HasOne(z => z.BiomarkerScale)
            .WithMany(s => s.Zones)
            .HasForeignKey(z => z.BiomarkerScaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<BiomarkerEntity>()
            .HasIndex(b => b.Key)
            .IsUnique();

        // JSON-конвертация для ProductEntity
        builder.Entity<ProductEntity>()
            .Property(p => p.Images)
            .HasConversion(ProductEntityJsonConverters.ImagesConverter)
            .HasColumnType("jsonb");
        builder.Entity<ProductEntity>()
            .Property(p => p.Images)
            .Metadata.SetValueComparer(new ValueComparer<List<string>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (acc, val) => HashCode.Combine(acc, val != null ? val.GetHashCode() : 0)),
                c => c == null ? new List<string>() : new List<string>(c)));
        builder.Entity<ProductEntity>()
            .Property(p => p.Features)
            .HasConversion(ProductEntityJsonConverters.FeaturesConverter)
            .HasColumnType("jsonb");
        builder.Entity<ProductEntity>()
            .Property(p => p.Features)
            .Metadata.SetValueComparer(new ValueComparer<List<ProductFeatureDto>>(
                (c1, c2) => (c1 == null && c2 == null) || (c1 != null && c2 != null && c1.SequenceEqual(c2)),
                c => c == null ? 0 : c.Aggregate(0, (acc, val) => HashCode.Combine(acc, val != null ? val.GetHashCode() : 0)),
                c => c == null ? new List<ProductFeatureDto>() : new List<ProductFeatureDto>(c)));
    }
}
